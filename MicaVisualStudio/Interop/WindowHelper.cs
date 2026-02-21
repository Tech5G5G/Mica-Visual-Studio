using System;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using MicaVisualStudio.Enums;

namespace MicaVisualStudio.Interop;

/// <summary>
/// Represents a static wrapper for various P/Invoke functions involving windows.
/// </summary>
public static class WindowHelper
{
    #region DWM

    [DllImport("dwmapi.dll", EntryPoint = "DwmExtendFrameIntoClientArea")]
    private static extern int ExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

    [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute")]
    private static extern int SetWindowAttribute(IntPtr hwnd, int dwAttribute, ref uint pvAttribute, int cbAttribute);

    [DllImport("dwmapi.dll", EntryPoint = "DwmEnableBlurBehindWindow")]
    private static extern int EnableBlurBehindWindow(IntPtr hWnd, ref DWM_BLURBEHIND pBlurBehind);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

    [DllImport("user32.dll")]
    private static extern bool SetWindowCompositionAttribute(IntPtr hwnd, ref WINDOWCOMPOSITIONATTRIBDATA pwcad);

    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38,
        DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
        DWMWA_WINDOW_CORNER_PREFERENCE = 33,
        DWMWA_BORDER_COLOR = 34;

    private const uint DWMWA_COLOR_NONE = 0xFFFFFFFE,
        DWMWA_COLOR_DEFAULT = 0xFFFFFFFF;

    private const uint DWM_BB_ENABLE = 0x1,
        DWM_BB_BLURREGION = 0x2,
        DWM_BB_TRANSITIONONMAXIMIZED = 0x4;

    private const int WCA_ACCENT_POLICY = 19;

    private const int ACCENT_DISABLED = 0,
        ACCENT_ENABLE_ACRYLICBLURBEHIND = 4;

    private struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    private struct DWM_BLURBEHIND
    {
        public uint dwFlags;
        public bool fEnable;
        public IntPtr hRgnBlur;
        public bool fTransitionOnMaximized;
    }

    private struct WINDOWCOMPOSITIONATTRIBDATA
    {
        public int Attrib;
        public IntPtr pvData;
        public uint cbData;
    }

    private struct AccentPolicy
    {
#pragma warning disable 0649
        public int AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
#pragma warning restore 0649
    }

    /// <summary>
    /// Extends the frame of the specified <paramref name="hWnd"/> into its client area.
    /// </summary>
    /// <param name="hWnd">A handle to a window.</param>
    public static void ExtendFrameIntoClientArea(IntPtr hWnd)
    {
        MARGINS margins = new() { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
        ExtendFrameIntoClientArea(hWnd, ref margins);
    }

    /// <summary>
    /// Enables or disables dark mode for the specified <paramref name="hWnd"/>.
    /// </summary>
    /// <param name="hWnd">A handle to a window.</param>
    /// <param name="enable">Whether or not to enable dark mode.</param>
    public static void EnableDarkMode(IntPtr hWnd, bool enable)
    {
        uint mode = enable ? 1u : 0;
        SetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref mode, sizeof(uint));
    }

    /// <summary>
    /// Sets the <see cref="CornerPreference"/> of the specified <paramref name="hWnd"/>.
    /// </summary>
    /// <param name="hWnd">A handle to a window.</param>
    /// <param name="preference">The <see cref="CornerPreference"/> to set.</param>
    public static void SetCornerPreference(IntPtr hWnd, CornerPreference preference)
    {
        uint corner = (uint)preference;
        SetWindowAttribute(hWnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(uint));
    }

    /// <summary>
    /// Sets the <see cref="BackdropType"/> of the specified <paramref name="hWnd"/>.
    /// </summary>
    /// <param name="hWnd">A handle to a window.</param>
    /// <param name="backdrop">The <see cref="BackdropType"/> to set.</param>
    public static void SetBackdropType(IntPtr hWnd, BackdropType backdrop)
    {
        uint type = (uint)(backdrop == BackdropType.Glass ? BackdropType.None : backdrop);
        SetWindowAttribute(hWnd, DWMWA_SYSTEMBACKDROP_TYPE, ref type, sizeof(uint));

        EnableWindowTransparency(hWnd, enable: backdrop == BackdropType.Glass);
    }

    /// <summary>
    /// Shows or hides the border of the specified <paramref name="hWnd"/>.
    /// </summary>
    /// <param name="hWnd">A handle to a window.</param>
    /// <param name="enable">Whether or not to show the border.</param>
    public static void EnableWindowBorder(IntPtr hWnd, bool enable)
    {
        uint color = enable ? DWMWA_COLOR_DEFAULT : DWMWA_COLOR_NONE;
        SetWindowAttribute(hWnd, DWMWA_BORDER_COLOR, ref color, sizeof(uint));
    }

    /// <summary>
    /// Enables or disables transparency for the specified <paramref name="hWnd"/>.
    /// </summary>
    /// <param name="hWnd">A handle to a window.</param>
    /// <param name="enable">Whether or not to enable transparency.</param>
    public static void EnableWindowTransparency(IntPtr hWnd, bool enable)
    {
        DWM_BLURBEHIND bb = new()
        {
            fEnable = enable,
            hRgnBlur = enable ? CreateRectRgn(-2, -2, -1, -1) : IntPtr.Zero,
            fTransitionOnMaximized = true,
            dwFlags = DWM_BB_ENABLE | DWM_BB_BLURREGION | DWM_BB_TRANSITIONONMAXIMIZED
        };
        _ = EnableBlurBehindWindow(hWnd, ref bb);
    }

    /// <summary>
    /// Enables or disables a blur effect used as the specified <paramref name="hWnd"/>'s background.
    /// </summary>
    /// <param name="hWnd">A handle to a window.</param>
    /// <param name="fallback">A fallback <see cref="System.Drawing.Color"/> to use if blurring is not available.</param>
    /// <param name="enable">Whether or not to enable blurring.</param>
    public static void EnableWindowBlur(IntPtr hWnd, System.Drawing.Color fallback, bool enable)
    {
        AccentPolicy policy = new()
        {
            AccentState = enable ? ACCENT_ENABLE_ACRYLICBLURBEHIND : ACCENT_DISABLED,
            GradientColor = ColorTranslator.ToWin32(fallback)
        };

        var size = Marshal.SizeOf<AccentPolicy>();
        var ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(policy, ptr, fDeleteOld: false);
        
        WINDOWCOMPOSITIONATTRIBDATA data = new()
        {
            Attrib = WCA_ACCENT_POLICY,
            pvData = ptr,
            cbData = (uint)size
        };

        SetWindowCompositionAttribute(hWnd, ref data);
        Marshal.FreeHGlobal(ptr);
    }

    #endregion

    #region Caption Buttons

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);

    [DllImport("user32.dll")]
    private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

    [DllImport("user32.dll")]
    private static extern IntPtr EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

    [DllImport("user32.dll")]
    private static extern int TrackPopupMenuEx(IntPtr hMenu, uint uFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

    [DllImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")]
    private static extern bool GetWindowAttribute(IntPtr hwnd, uint dwAttribute, out RECT pvAttribute, uint cbAttribute);

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

    private const uint SW_NORMAL = 1,
        SW_MAXIMIZE = 3;

    private const uint MF_ENABLED = 0x0,
        MF_GRAYED = 0x1;

    private const int HTCAPTION = 2;

    private const int VK_SPACE = 0x20;

    private const int DWMWA_CAPTION_BUTTON_BOUNDS = 5;

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
    public static int GetTitleBarHeight(IntPtr hWnd)
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
        const int MenuSpacing = 2;
        WindowType type = GetWindowType(source.RootVisual as Window);

        GetSystemMenu(source.Handle, bRevert: false); // Make sure window menu is created

        source.AddHook(Hook);
        SetWindowStyles(source.Handle, GetWindowStyles(source.Handle)); // Refresh styles

        IntPtr Hook(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_DESTROY:
                    HwndSource.FromHwnd(hWnd)?.RemoveHook(Hook); // Avoid memory leaks
                    break;

                case WM_STYLECHANGING when (int)wParam == GWL_STYLE:
                    STYLESTRUCT structure = Marshal.PtrToStructure<STYLESTRUCT>(lParam);

                    if (type == WindowType.Main || // Apply WS_OVERLAPPEDWINDOW style to main window
                        ((WindowStyles)structure.styleNew).HasFlag(WindowStyles.ThickFrame)) // or any sizable window
                        structure.styleNew |= (uint)WindowStyles.OverlappedWindow;

                    structure.styleNew &= (uint)~WindowStyles.SystemMenu; // Remove the WS_SYSMENU style

                    Marshal.StructureToPtr(structure, lParam, fDeleteOld: true);
                    handled = true;
                    break;

                case WM_NCRBUTTONUP when (int)wParam == HTCAPTION:
                    ShowMenu(
                        hWnd,
                        (short)lParam,
                        (short)((int)lParam >> 16 /* Y position shift */),
                        keyboard: false);
                    handled = true;
                    break;

                case WM_SYSKEYDOWN when (int)wParam == VK_SPACE && IsAltPressed(lParam):
                    int height = GetTitleBarHeight(hWnd);

                    POINT point = new() { x = MenuSpacing, y = (height > 0 ? height : System.Windows.Forms.SystemInformation.CaptionHeight) + MenuSpacing };
                    ClientToScreen(hWnd, ref point);

                    ShowMenu(hWnd, point.x, point.y, keyboard: true);
                    break;
            }

            return IntPtr.Zero;
        }

        void ShowMenu(IntPtr hWnd, int x, int y, bool keyboard)
        {
            IntPtr menu = GetSystemMenu(hWnd, bRevert: false);

            uint minimize = type == WindowType.Dialog ? MF_GRAYED : MF_ENABLED;
            uint maximize = GetWindowStyles(source.Handle).HasFlag(WindowStyles.MaximizeBox) ? MF_ENABLED : MF_GRAYED;
            uint size = GetWindowStyles(source.Handle).HasFlag(WindowStyles.ThickFrame) ? MF_ENABLED : MF_GRAYED;

            if (GetWindowPlacement(hWnd, out WINDOWPLACEMENT placement))
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

            int cmd = TrackPopupMenuEx(
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
                SendMessage(hWnd, WM_SYSCOMMAND, (IntPtr)cmd, IntPtr.Zero);
        }

        bool IsAltPressed(IntPtr lParam) =>
            (((int)lParam >> 29) // Context code shift
            & 1) // First bit mask
            == 1; // TRUE
    }

    #endregion

    #region Window Styles

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern uint SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    private const int GWL_STYLE = -16,
        GWL_EXSTYLE = -20;

    /// <summary>
    /// Determines the <see cref="WindowType"/> of the specified <paramref name="window"/>.
    /// </summary>
    /// <param name="window">A <see cref="Window"/> to critique.</param>
    /// <returns>The <see cref="WindowType"/> of the specified <paramref name="window"/>.</returns>
    public static WindowType GetWindowType(Window window)
    {
        if (window == Application.Current.MainWindow)
        {
            return WindowType.Main;
        }
        else if (window is not null && // Check if window is WPF
            (window.WindowStyle == WindowStyle.None || // and has no style
            window.Owner is null)) // or no owner
        {
            return WindowType.Tool;
        }
        else
        {
            return WindowType.Dialog;
        }
    }

    /// <summary>
    /// Gets the <see cref="WindowStyles"/> of the specified <paramref name="hWnd"/>.
    /// </summary>
    /// <param name="hWnd">A handle to a window.</param>
    /// <returns>The <see cref="WindowStyles"/> of the specified <paramref name="hWnd"/>.</returns>
    public static WindowStyles GetWindowStyles(IntPtr hWnd) => (WindowStyles)GetWindowLong(hWnd, GWL_STYLE);
    /// <summary>
    /// Sets the <see cref="WindowStyles"/> of the specified <paramref name="hWnd"/>.
    /// </summary>
    /// <param name="hWnd">A handle to a window.</param>
    /// <param name="styles">The <see cref="WindowStyles"/> to set.</param>
    public static void SetWindowStyles(IntPtr hWnd, WindowStyles styles) => SetWindowLong(hWnd, GWL_STYLE, (uint)styles);

    /// <summary>
    /// Gets the <see cref="WindowStylesEx"/> of the specified <paramref name="hWnd"/>.
    /// </summary>
    /// <param name="hWnd">A handle to a window.</param>
    /// <returns>The <see cref="WindowStylesEx"/> of the specified <paramref name="hWnd"/>.</returns>
    public static WindowStylesEx GetExtendedWindowStyles(IntPtr hWnd) => (WindowStylesEx)GetWindowLong(hWnd, GWL_EXSTYLE);
    /// <summary>
    /// Sets the <see cref="WindowStylesEx"/> of the specified <paramref name="hWnd"/>.
    /// </summary>
    /// <param name="hWnd">A handle to a window.</param>
    /// <param name="styles">The <see cref="WindowStylesEx"/> to set.</param>
    public static void SetExtendedWindowStyles(IntPtr hWnd, WindowStylesEx styles) => SetWindowLong(hWnd, GWL_EXSTYLE, (uint)styles);

    #endregion

    #region Utilities

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildProc lpEnumFunc, IntPtr lParam);

    private const uint LWA_ALPHA = 0x00000002;

    private delegate bool EnumChildProc(IntPtr hwnd, IntPtr lParam);

    /// <summary>
    /// Determines whether the specified <paramref name="hWnd"/> is alive; that is, whether it is still valid.
    /// </summary>
    /// <param name="hWnd">A handle to a window.</param>
    /// <returns><see langword="true"/> if the specified <paramref name="hWnd"/> is still alive. Otherwise, <see langword="false"/>.</returns>
    public static bool IsAlive(IntPtr hWnd) => IsWindow(hWnd);

    /// <summary>
    /// Gets the process ID associated with the specified <paramref name="hWnd"/>.
    /// </summary>
    /// <param name="hWnd">A handle to a window.</param>
    /// <returns>The ID of the process that owns the specified <paramref name="hWnd"/>.</returns>
    public static int GetProcessId(IntPtr hWnd)
    {
        GetWindowThreadProcessId(hWnd, out uint procId);
        return (int)procId;
    }

    /// <summary>
    /// Makes the specified <paramref name="hWnd"/> layered by adding the <see cref="WindowStylesEx.Layered"/> style.
    /// </summary>
    /// <param name="hWnd">A handle to a window.</param>
    public static void MakeLayered(IntPtr hWnd)
    {
        SetExtendedWindowStyles(hWnd, GetExtendedWindowStyles(hWnd) | WindowStylesEx.Layered);
        SetLayeredWindowAttributes(
            hWnd,
            (uint)ColorTranslator.ToWin32(Color.Black),
            0xFF, // Set opactiy to 100%
            LWA_ALPHA);
    }

    /// <summary>
    /// Enumerates the children of the specified <paramref name="hWnd"/>.
    /// </summary>
    /// <param name="hWnd">A handle to a window.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of window handles representing the children of the specified <paramref name="hWnd"/>.</returns>
    public static IEnumerable<IntPtr> GetChildren(IntPtr hWnd)
    {
        List<IntPtr> handles = [];
        EnumChildWindows(hWnd, Proc, IntPtr.Zero);
        return handles;

        bool Proc(IntPtr hwnd, IntPtr lParam)
        {
            handles.Add(hwnd);
            return true;
        }
    }

    /// <summary>
    /// Gets the handle of the specified <paramref name="window"/>.
    /// </summary>
    /// <param name="window">A <see cref="Window"/> to retrieve the handle of.</param>
    /// <returns>The handle of the specified <paramref name="window"/>.</returns>
    public static IntPtr GetHandle(this Window window)
    {
        WindowInteropHelper interop = new(window);
        interop.EnsureHandle();
        return interop.Handle;
    }

    #endregion
}

/// <summary>
/// Specifies the style(s) of a window.
/// </summary>
/// <remarks>
/// Used in:
/// <list type="bullet">
/// <item>
/// <see cref="WindowHelper.GetWindowStyles(IntPtr)"/>
/// </item>
/// <item>
/// <see cref="WindowHelper.SetWindowStyles(IntPtr, WindowStyles)"/>
/// </item>
/// </list>
/// </remarks>
[Flags]
public enum WindowStyles : uint
{
    Border = 0x00800000,
    Caption = 0x00C00000,
    Child = 0x40000000,
    ClipChildren = 0x02000000,
    ClipSiblings = 0x04000000,
    Disabled = 0x08000000,
    DLGFrame = 0x00400000,
    Group = 0x00020000,
    HScroll = 0x00100000,
    Iconic = 0x20000000,
    Maximize = 0x01000000,
    MaximizeBox = 0x00010000,
    Minimize = 0x20000000,
    MinimizeBox = 0x00020000,
    Overlapped = 0x00000000,
    OverlappedWindow = Overlapped | Caption | SystemMenu | ThickFrame | MinimizeBox | MaximizeBox,
    Popup = 0x80000000,
    PopupWindow = Popup | Border | SystemMenu,
    SystemMenu = 0x00080000,
    TabStop = 0x00010000,
    ThickFrame = 0x00040000,
    Visible = 0x10000000,
    VScroll = 0x00200000
}

/// <summary>
/// Specifies the extended style(s) of a window.
/// </summary>
/// <remarks>
/// Used in:
/// <list type="bullet">
/// <item>
/// <see cref="WindowHelper.GetExtendedWindowStyles(IntPtr)"/>
/// </item>
/// <item>
/// <see cref="WindowHelper.SetExtendedWindowStyles(IntPtr, WindowStylesEx)"/>
/// </item>
/// </list>
/// </remarks>
public enum WindowStylesEx : uint
{
    AcceptFiles = 0x00000010,
    AppWindow = 0x00040000,
    ClientEdge = 0x00000200,
    Composited = 0x02000000,
    ContextHelp = 0x00000400,
    ControlParent = 0x00010000,
    DLGModalFrame = 0x00000001,
    Layered = 0x00080000,
    LayoutRTL = 0x00400000,
    Left = 0x00000000,
    LeftScrollBar = 0x00004000,
    LTRReading = 0x00000000,
    MDIChild = 0x00000040,
    NoActivate = 0x08000000,
    NoInheritLayout = 0x00100000,
    NoParentNotify = 0x00000004,
    NoRedirectionBitmap = 0x00200000,
    OverlappedWindow = WindowEdge | ClientEdge,
    PaletteWindow = WindowEdge | ToolWindow | TopMost,
    Right = 0x00001000,
    RightScrollBar = 0x00000000,
    RTLReading = 0x00002000,
    StaticEdge = 0x00020000,
    ToolWindow = 0x00000080,
    TopMost = 0x00000008,
    Transparent = 0x00000020,
    WindowEdge = 0x00000100
}
