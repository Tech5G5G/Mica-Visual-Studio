using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using MicaVisualStudio.Options;

namespace MicaVisualStudio.Interop
{
    internal partial class PInvoke
    {
        [DllImport("user32.dll")]
        private static extern nint SendMessage(nint hWnd, int Msg, nint wParam, nint lParam);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(nint hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool GetWindowPlacement(nint hWnd, out WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        private static extern nint GetSystemMenu(nint hWnd, bool bRevert);

        [DllImport("user32.dll")]
        private static extern nint EnableMenuItem(nint hMenu, uint uIDEnableItem, uint uEnable);

        [DllImport("user32.dll")]
        private static extern int TrackPopupMenuEx(nint hMenu, uint uFlags, int x, int y, nint hwnd, nint lptpm);

        [DllImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")]
        private static extern bool GetWindowAttribute(nint hwnd, uint dwAttribute, out RECT pvAttribute, uint cbAttribute);

        private const int WM_NULL = 0x0,
            WM_DESTROY = 0x2,
            WM_STYLECHANGING = 0x7C,
            WM_NCRBUTTONUP = 0xA5,
            WM_SYSKEYDOWN = 0x104,
            WM_SYSCOMMAND = 0x112;

        private const uint SC_RESTORE = 0xF120,
            SC_MOVE = 0xF010,
            SC_SIZE = 0xF000,
            SC_MAXIMIZE = 0xF030,
            SC_MINIMIZE = 0xF020,
            SC_CLOSE = 0xF060;

        private const uint TPM_LEFTBUTTON = 0x0,
            TPM_RIGHTBUTTON = 0x2,
            TPM_RIGHTALIGN = 0x8,
            TPM_NONOTIFY = 0x80,
            TPM_RETURNCMD = 0x100,
            TPM_NOANIMATION = 0x4000;

        private const uint SW_NORMAL = 1, SW_MAXIMIZE = 3;

        private const uint MF_ENABLED = 0x0, MF_GRAYED = 0x1;

        private const int HTCAPTION = 2;

        private const int VK_SPACE = 0x20;

        private const int DWMWA_CAPTION_BUTTON_BOUNDS = 5;
        
        private const int MenuSpacing = 2;

        private struct WINDOWPLACEMENT
        {
            public uint length;
            public uint flags;
            public uint showCmd;
            public POINT ptMinPosition;
            public POINT ptMaxPosition;
            public RECT rcNormalPosition;
            public RECT rcDevice;
        }

        private struct STYLESTRUCT
        {
#pragma warning disable 0649
            public uint styleOld;
            public uint styleNew;
#pragma warning restore 0649
        }

        /// <summary>
        /// Gets the height, in pixels, of the specified <paramref name="hWnd"/>'s title bar.
        /// </summary>
        /// <param name="hWnd">A handle to a window.</param>
        /// <returns>The height of the specified <paramref name="hWnd"/>'s title bar.</returns>
        public static int GetTitleBarHeight(nint hWnd)
        {
            GetWindowAttribute(hWnd, DWMWA_CAPTION_BUTTON_BOUNDS, out RECT bounds, (uint)Marshal.SizeOf<RECT>());
            return bounds.bottom - bounds.top;
        }

        /// <summary>
        /// Patches the specified <paramref name="source"/> to remove its caption buttons but retain system menu functionality.
        /// </summary>
        /// <param name="source">An <see cref="HwndSource"/> to patch.</param>
        public static void RemoveCaptionButtons(HwndSource source)
        {
            var type = (source.RootVisual as Window).WindowType;

            // Make sure window menu is created
            GetSystemMenu(source.Handle, bRevert: false);

            source.AddHook(Hook);
            SetWindowStyles(source.Handle, GetWindowStyles(source.Handle)); // Refresh styles

            nint Hook(nint hWnd, int msg, nint wParam, nint lParam, ref bool handled)
            {
                switch (msg)
                {
                    case WM_DESTROY:
                        // Clean up hook
                        HwndSource.FromHwnd(hWnd)?.RemoveHook(Hook);
                        break;

                    case WM_STYLECHANGING when (int)wParam == GWL_STYLE:
                        var structure = Marshal.PtrToStructure<STYLESTRUCT>(lParam);

                        if (type == WindowType.Main || // Apply WS_OVERLAPPEDWINDOW style to main window
                            ((WindowStyle)structure.styleNew).HasFlag(WindowStyle.ThickFrame)) // or any sizable window
                        {
                            structure.styleNew |= (uint)WindowStyle.OverlappedWindow;
                        }

                        // Remove the WS_SYSMENU style
                        structure.styleNew &= (uint)~WindowStyle.SystemMenu;

                        Marshal.StructureToPtr(structure, lParam, fDeleteOld: true);
                        handled = true;
                        break;

                    case WM_NCRBUTTONUP when (int)wParam == HTCAPTION:
                        ShowMenu(
                            hWnd,
                            (short)lParam,
                            (short)((int)lParam >> 16 /* Y position shift */),
                            keyboard: false,
                            type);
                        handled = true;
                        break;

                    case WM_SYSKEYDOWN when (int)wParam == VK_SPACE && IsAltPressed(lParam):
                        var height = GetTitleBarHeight(hWnd);

                        POINT point = new() { x = MenuSpacing, y = (height > 0 ? height : System.Windows.Forms.SystemInformation.CaptionHeight) + MenuSpacing };
                        ClientToScreen(hWnd, ref point);

                        ShowMenu(hWnd, point.x, point.y, keyboard: true, type);
                        break;
                }

                return IntPtr.Zero;
            }
        }

        private static void ShowMenu(nint hWnd, int x, int y, bool keyboard, WindowType type)
        {
            var menu = GetSystemMenu(hWnd, bRevert: false);

            uint minimize = type == WindowType.Dialog ? MF_GRAYED : MF_ENABLED;
            uint maximize = GetWindowStyles(hWnd).HasFlag(WindowStyle.MaximizeBox) ? MF_ENABLED : MF_GRAYED;
            uint size = GetWindowStyles(hWnd).HasFlag(WindowStyle.ThickFrame) ? MF_ENABLED : MF_GRAYED;

            if (GetWindowPlacement(hWnd, out WINDOWPLACEMENT placement))
            {
                if (placement.showCmd == SW_NORMAL)
                {
                    EnableMenuItem(menu, SC_RESTORE, MF_GRAYED);
                    EnableMenuItem(menu, SC_MOVE, MF_ENABLED);
                    EnableMenuItem(menu, SC_SIZE, size);
                    EnableMenuItem(menu, SC_MINIMIZE, minimize);
                    EnableMenuItem(menu, SC_MAXIMIZE, maximize);
                    EnableMenuItem(menu, SC_CLOSE, MF_ENABLED);
                }
                else if (placement.showCmd == SW_MAXIMIZE)
                {
                    EnableMenuItem(menu, SC_RESTORE, MF_ENABLED);
                    EnableMenuItem(menu, SC_MOVE, MF_GRAYED);
                    EnableMenuItem(menu, SC_SIZE, MF_GRAYED);
                    EnableMenuItem(menu, SC_MINIMIZE, minimize);
                    EnableMenuItem(menu, SC_MAXIMIZE, MF_GRAYED);
                    EnableMenuItem(menu, SC_CLOSE, MF_ENABLED);
                }
            }

            var cmd = TrackPopupMenuEx(
                menu,
                TPM_RETURNCMD | TPM_NONOTIFY | // Don't notify as we'll send a message later
                ((uint)System.Windows.Forms.SystemInformation.PopupMenuAlignment * TPM_RIGHTALIGN) |
                (keyboard ? TPM_LEFTBUTTON : TPM_RIGHTBUTTON) |
                (keyboard ? TPM_NOANIMATION : 0 /* Default fade animation */),
                keyboard && placement.showCmd == SW_MAXIMIZE ? x - MenuSpacing : x,
                y,
                hWnd,
                IntPtr.Zero);

            if (cmd != WM_NULL)
            {
                SendMessage(hWnd, WM_SYSCOMMAND, cmd, IntPtr.Zero);
            }
        }

        private static bool IsAltPressed(nint lParam)
        {
            return (((int)lParam
                >> 29) // Context code shift
                & 1)   // First bit mask
                == 1;  // TRUE
        }
    }
}
