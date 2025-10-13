namespace MicaVisualStudio.Helpers;

public static class WindowHelper
{
    #region PInvoke

    [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute", CharSet = CharSet.Unicode)]
    private static extern int SetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    [DllImport("dwmapi.dll", EntryPoint = "DwmExtendFrameIntoClientArea", CharSet = CharSet.Unicode)]
    private static extern int ExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

    [DllImport("dwmapi.dll", EntryPoint = "DwmEnableBlurBehindWindow")]
    private static extern int EnableBlurBehindWindow(IntPtr hWnd, ref DWM_BLURBEHIND pBlurBehind);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern uint SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

    private const int GWL_STYLE = -16;

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

    #endregion

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


    {
        _ = GetWindowThreadProcessId(hWnd, out uint pid);
        return (int)pid;
    }

    public static WindowStyle GetWindowStyles(IntPtr hWnd) => (WindowStyle)GetWindowLong(hWnd, GWL_STYLE);

    public static IntPtr GetHandle(this Window window)
    {
        WindowInteropHelper interop = new(window);
        interop.EnsureHandle();
        return interop.Handle;
    }
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

[Flags]
public enum WindowStyle : uint
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
