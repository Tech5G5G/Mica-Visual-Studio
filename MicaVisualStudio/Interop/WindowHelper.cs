using Microsoft.VisualStudio.OLE.Interop;

namespace MicaVisualStudio.Interop;

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

    private const uint DWM_BB_ENABLE = 0x1,
        DWM_BB_BLURREGION = 0x2,
        DWM_BB_TRANSITIONONMAXIMIZED = 0x4;

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

    public static void ExtendFrameIntoClientArea(IntPtr hWnd)
    {
        MARGINS margins = new() { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
        ExtendFrameIntoClientArea(hWnd, ref margins);
    }

    public static void SetDarkMode(IntPtr hWnd, bool enable)
    {
        int mode = enable ? 1 : 0;
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

        SetWindowTransparency(hWnd, enable: backdrop == BackdropType.Glass);
    }

    public static void SetWindowTransparency(IntPtr hWnd, bool enable)
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

    public static int GetTitleBarHeight(IntPtr hWnd)
    {
        GetWindowAttribute(hWnd, DWMWA_CAPTION_BUTTON_BOUNDS, out RECT bounds, (uint)Marshal.SizeOf<RECT>());
        return bounds.bottom - bounds.top;
    }

    public static void RemoveCaptionButtons(HwndSource source)
    {
        const int MenuSpacing = 2;
        WindowType type = GetWindowType(source.RootVisual as Window);

        GetSystemMenu(source.Handle, bRevert: false); //Make sure window menu is created

        source.AddHook(Hook);
        SetWindowStyles(source.Handle, GetWindowStyles(source.Handle)); //Refresh styles

        IntPtr Hook(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_DESTROY:
                    source.RemoveHook(Hook); //Avoid memory leaks
                    break;
                case WM_STYLECHANGING when (int)wParam == GWL_STYLE:
                    STYLESTRUCT structure = Marshal.PtrToStructure<STYLESTRUCT>(lParam);

                    if (type == WindowType.Main || //Apply WS_OVERLAPPEDWINDOW style to main window
                        ((WindowStyles)structure.styleNew).HasFlag(WindowStyles.ThickFrame)) //or any sizable window
                        structure.styleNew |= (uint)WindowStyles.OverlappedWindow;

                    structure.styleNew &= (uint)~WindowStyles.SystemMenu; //Remove the WS_SYSMENU style

                    Marshal.StructureToPtr(structure, lParam, fDeleteOld: true);
                    handled = true;
                    break;
                case WM_NCRBUTTONUP when (int)wParam == HTCAPTION:
                    ShowMenu(
                        hWnd,
                        (short)lParam,
                        (short)((int)lParam >> 16 /*Y position shift*/),
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
                TPM_RETURNCMD | TPM_NONOTIFY | //Don't notify as we'll send a message later
                ((uint)System.Windows.Forms.SystemInformation.PopupMenuAlignment * TPM_RIGHTALIGN) |
                (keyboard ? TPM_LEFTBUTTON : TPM_RIGHTBUTTON) |
                (keyboard ? TPM_NOANIMATION : 0 /*Default fade animation*/),
                keyboard && placement.showCmd == SW_MAXIMIZE ? x - MenuSpacing : x,
                y,
                hWnd,
                IntPtr.Zero);

            if (cmd != WM_NULL)
                SendMessage(hWnd, WM_SYSCOMMAND, (IntPtr)cmd, IntPtr.Zero);
        }

        bool IsAltPressed(IntPtr lParam) =>
            (((int)lParam >> 29) //Context code shift
            & 1) //First bit mask
            == 1; //TRUE
    }

    #endregion

    #region Window Styles

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern uint SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    private const int GWL_STYLE = -16,
        GWL_EXSTYLE = -20;

    public static WindowType GetWindowType(Window window)
    {
        if (window == WindowManager.MainWindow)
            return WindowType.Main;
        else if (window is not null && //Check if window is WPF
            (window.WindowStyle == WindowStyle.None || //and has no style
            window.Owner is null)) //or no owner
            return WindowType.Tool;
        else
            return WindowType.Dialog;
    }

    public static WindowStyles GetWindowStyles(IntPtr hWnd) => (WindowStyles)GetWindowLong(hWnd, GWL_STYLE);
    public static void SetWindowStyles(IntPtr hWnd, WindowStyles styles) => SetWindowLong(hWnd, GWL_STYLE, (uint)styles);

    public static WindowStylesEx GetExtendedWindowStyles(IntPtr hWnd) => (WindowStylesEx)GetWindowLong(hWnd, GWL_EXSTYLE);
    public static void SetExtendedWindowStyles(IntPtr hWnd, WindowStylesEx styles) => SetWindowLong(hWnd, GWL_EXSTYLE, (uint)styles);

    #endregion

    #region Utilities

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    public static bool IsAlive(IntPtr hWnd) => IsWindow(hWnd);

    public static int GetProcessId(IntPtr hWnd)
    {
        GetWindowThreadProcessId(hWnd, out uint procId);
        return (int)procId;
    }

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

public enum WindowType
{
    Main,
    Tool,
    Dialog
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

#endregion
