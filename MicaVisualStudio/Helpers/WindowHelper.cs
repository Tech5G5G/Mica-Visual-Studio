using System;
using System.Runtime.InteropServices;

namespace MicaVisualStudio.Helpers
{
    public static class WindowHelper
    {
        #region PInvoke

        [DllImport("Dwmapi.dll", EntryPoint = "DwmSetWindowAttribute", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int SetWindowAttribute(IntPtr hWnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

        [DllImport("Dwmapi.dll", EntryPoint = "DwmExtendFrameIntoClientArea", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int ExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        const int GWL_STYLE = -16;

        const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

        private struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        #endregion

        public static void SetSystemBackdropType(IntPtr hWnd, BackdropType backdrop)
        {
            int type = (int)backdrop;
            SetWindowAttribute(hWnd, DWMWA_SYSTEMBACKDROP_TYPE, ref type, sizeof(int));
        }

        public static void SetImmersiveDarkMode(IntPtr hWnd, Theme theme)
        {
            int actualTheme = (int)theme;
            SetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref actualTheme, sizeof(int));
        }

        public static void SetCornerPreference(IntPtr hWnd, CornerPreference preference)
        {
            int cornerPrefence = (int)preference;
            SetWindowAttribute(hWnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPrefence, sizeof(int));
        }

        public static void ExtendFrameIntoClientArea(IntPtr hWnd)
        {
            var margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
            ExtendFrameIntoClientArea(hWnd, ref margins);
        }

        public static WindowStyle GetWindowStyles(IntPtr hWnd) => (WindowStyle)GetWindowLong(hWnd, GWL_STYLE);
    }

    #region Enums

    public enum BackdropType
    {
        Auto,
        None,
        Mica,
        Acrylic,
        Tabbed,
    }

    public enum CornerPreference
    {
        Default,
        Square,
        Round,
        RoundSmall
    }

    public enum Theme
    {
        Light,
        Dark,
        System
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
}
