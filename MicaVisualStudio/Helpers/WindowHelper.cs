using Microsoft.VisualStudio.OLE.Interop;

namespace MicaVisualStudio.Helpers;

public static class WindowHelper
{
    #region DWM

    [DllImport("dwmapi.dll", EntryPoint = "DwmExtendFrameIntoClientArea")]
    private static extern int ExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

    [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute")]
    private static extern int SetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    [DllImport("dwmapi.dll", EntryPoint = "DwmEnableBlurBehindWindow")]
    private static extern int EnableBlurBehindWindow(IntPtr hWnd, ref DWM_BLURBEHIND pBlurBehind);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38,
        DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
        DWMWA_WINDOW_CORNER_PREFERENCE = 33;

    [Flags]
    private enum DWM_BB
    {
        DWM_BB_ENABLE = 1,
        DWM_BB_BLURREGION = 2,
        DWM_BB_TRANSITIONONMAXIMIZED = 4
    }

    private struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    private struct DWM_BLURBEHIND
    {
        public DWM_BB dwFlags;
        public bool fEnable;
        public IntPtr hRgnBlur;
        public bool fTransitionOnMaximized;
    }

    public static void ExtendFrameIntoClientArea(IntPtr hWnd)
    {
        MARGINS margins = new() { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
        ExtendFrameIntoClientArea(hWnd, ref margins);
    }

    public static void EnableDarkMode(IntPtr hWnd)
    {
        int mode = 1; //TRUE
        SetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref mode, sizeof(int));
    }

    public static void SetCornerPreference(IntPtr hWnd, CornerPreference preference)
    {
        int corner = (int)preference;
        SetWindowAttribute(hWnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
    }

    public static void SetBackdropType(IntPtr hWnd, BackdropType backdrop)
    {
        int type = (int)(backdrop == BackdropType.Glass ? BackdropType.None : backdrop);
        SetWindowAttribute(hWnd, DWMWA_SYSTEMBACKDROP_TYPE, ref type, sizeof(int));

        bool enable = backdrop == BackdropType.Glass;
        EnableWindowTransparency(hWnd, enable);
    }

    public static void EnableWindowTransparency(IntPtr hWnd, bool enable)
    {
        DWM_BLURBEHIND bb = new()
        {
            dwFlags = DWM_BB.DWM_BB_ENABLE | DWM_BB.DWM_BB_BLURREGION | DWM_BB.DWM_BB_TRANSITIONONMAXIMIZED,
            fEnable = enable,
            hRgnBlur = enable ? CreateRectRgn(-2, -2, -1, -1) : IntPtr.Zero,
            fTransitionOnMaximized = true
        };
        _ = EnableBlurBehindWindow(hWnd, ref bb);
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

    [DllImport("dwmapi.dll")]
    private static extern bool DwmGetWindowAttribute(IntPtr hwnd, uint dwAttribute, out RECT pvAttribute, uint cbAttribute);

    struct WINDOWPLACEMENT
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

    private const int WM_DESTROY = 0x02,
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

    public static int GetTitleBarHeight(IntPtr hWnd)
    {
        DwmGetWindowAttribute(hWnd, DWMWA_CAPTION_BUTTON_BOUNDS, out RECT bounds, (uint)Marshal.SizeOf<RECT>());
        return bounds.bottom - bounds.top;
    }

    public static void RemoveCaptionButtons(HwndSource source)
    {
        const int MenuSpacing = 2;

        GetSystemMenu(source.Handle, bRevert: false); //Make sure window menu is created

        source.AddHook(Hook);
        SetWindowStyles(source.Handle, GetWindowStyles(source.Handle)); //Refresh styles

        IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_DESTROY:
                    source.RemoveHook(Hook); //Avoid memory leaks
                    break;
                case WM_STYLECHANGING when (int)wParam == GWL_STYLE:
                    STYLESTRUCT structure = Marshal.PtrToStructure<STYLESTRUCT>(lParam);

                    //Remove WS_SYSMENU style
                    structure.styleNew |= (uint)WindowStyles.OverlappedWindow;
                    structure.styleNew &= (uint)~WindowStyles.SystemMenu;

                    Marshal.StructureToPtr(structure, lParam, fDeleteOld: true);
                    handled = true;
                    break;
                case WM_NCRBUTTONUP when (int)wParam == HTCAPTION:
                    ShowMenu(
                        hwnd,
                        (short)lParam,
                        (short)((int)lParam >> 16 /*Y position shift*/),
                        keyboard: false);
                    handled = true;
                    break;
                case WM_SYSKEYDOWN when (int)wParam == VK_SPACE && IsAltPressed(lParam):
                    int height = GetTitleBarHeight(hwnd);

                    POINT point = new() { x = MenuSpacing, y = (height < 1 ? System.Windows.Forms.SystemInformation.CaptionHeight : height) + MenuSpacing };
                    ClientToScreen(hwnd, ref point);

                    ShowMenu(hwnd, point.x, point.y, keyboard: true);
                    break;
            }

            return IntPtr.Zero;
        }

        void ShowMenu(IntPtr hWnd, int x, int y, bool keyboard)
        {
            IntPtr menu = GetSystemMenu(hWnd, bRevert: false);

            uint minimize = (source.RootVisual as Window).Owner is null ? MF_ENABLED : MF_GRAYED; //If the window is a child, it is probably a dialog
            if (GetWindowPlacement(hWnd, out WINDOWPLACEMENT placement))  
                if (placement.showCmd == SW_NORMAL)
                {
                    EnableMenuItem(menu, SC_RESTORE, MF_GRAYED);
                    EnableMenuItem(menu, SC_MOVE, MF_ENABLED);
                    EnableMenuItem(menu, SC_SIZE, MF_ENABLED);
                    EnableMenuItem(menu, SC_MINIMIZE, minimize);
                    EnableMenuItem(menu, SC_MAXIMIZE, MF_ENABLED);
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
                TPM_RETURNCMD | TPM_NONOTIFY | //Don't notify as we'll send a message later
                ((uint)System.Windows.Forms.SystemInformation.PopupMenuAlignment * TPM_RIGHTALIGN) |
                (keyboard ? TPM_LEFTBUTTON : TPM_RIGHTBUTTON) |
                (keyboard ? TPM_NOANIMATION : 0 /*Default fade animation*/),
                keyboard && placement.showCmd == SW_MAXIMIZE ? x - MenuSpacing : x,
                y,
                hWnd,
                IntPtr.Zero);

            if (cmd != 0)
                SendMessage(hWnd, WM_SYSCOMMAND, (IntPtr)cmd, IntPtr.Zero);
        }

        bool IsAltPressed(IntPtr lParam) =>
            (((int)lParam >> 29) //Context code shift
            & 0b1) //First bit mask
            == 1; //TRUE
    }

    #endregion

    #region Window Styles

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern uint SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    private const int GWL_STYLE = -16;

    public static WindowStyles GetWindowStyles(IntPtr hWnd) => (WindowStyles)GetWindowLong(hWnd, GWL_STYLE);

    public static void SetWindowStyles(IntPtr hWnd, WindowStyles styles) => SetWindowLong(hWnd, GWL_STYLE, (uint)styles);

    #endregion

    #region App Theme

    [DllImport("uxtheme.dll", EntryPoint = "#135")]
    private static extern int SetPreferredAppMode(PreferredAppMode preferredAppMode);

    public static void SetAppTheme(PreferredAppMode theme) => SetPreferredAppMode(theme);

    #endregion

    #region Interop

    public static IntPtr GetHandle(this Window window)
    {
        WindowInteropHelper interop = new(window);
        interop.EnsureHandle();
        return interop.Handle;
    }

    #endregion
}

#region Enums

public enum BackdropType
{
    Auto,
    None,
    Mica,
    Acrylic,
    Tabbed,
    Glass
}

public enum CornerPreference
{
    Default,
    Square,
    Round,
    RoundSmall
}

public enum PreferredAppMode
{
    Default,
    Light,
    Dark
}

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

#endregion
