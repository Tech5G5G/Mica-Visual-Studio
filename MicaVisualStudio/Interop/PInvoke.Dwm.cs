using System;
using System.Drawing;
using System.Runtime.InteropServices;
using MicaVisualStudio.Options;

namespace MicaVisualStudio.Interop;

internal partial class PInvoke
{
    [DllImport("dwmapi.dll", EntryPoint = "DwmExtendFrameIntoClientArea")]
    private static extern int ExtendFrameIntoClientArea(nint hWnd, ref MARGINS pMarInset);

    [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute")]
    private static extern int SetWindowAttribute(nint hwnd, int dwAttribute, ref uint pvAttribute, int cbAttribute);

    [DllImport("dwmapi.dll", EntryPoint = "DwmEnableBlurBehindWindow")]
    private static extern int EnableBlurBehindWindow(nint hWnd, ref DWM_BLURBEHIND pBlurBehind);

    [DllImport("gdi32.dll")]
    private static extern nint CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

    [DllImport("user32.dll")]
    private static extern bool SetWindowCompositionAttribute(nint hwnd, ref WINDOWCOMPOSITIONATTRIBDATA pwcad);

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
        public nint hRgnBlur;
        public bool fTransitionOnMaximized;
    }

    private struct WINDOWCOMPOSITIONATTRIBDATA
    {
        public int Attrib;
        public nint pvData;
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

    public static void ExtendFrameIntoClientArea(nint hWnd)
    {
        MARGINS margins = new() { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
        ExtendFrameIntoClientArea(hWnd, ref margins);
    }

    public static void EnableDarkMode(nint hWnd, bool enable)
    {
        var mode = enable ? 1u : 0;
        SetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref mode, sizeof(uint));
    }

    public static void SetCornerPreference(nint hWnd, CornerPreference preference)
    {
        var corner = (uint)preference;
        SetWindowAttribute(hWnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(uint));
    }

    public static void SetBackdropType(nint hWnd, BackdropType backdrop)
    {
        var type = (uint)(backdrop == BackdropType.Glass ? BackdropType.None : backdrop);
        SetWindowAttribute(hWnd, DWMWA_SYSTEMBACKDROP_TYPE, ref type, sizeof(uint));

        EnableWindowTransparency(hWnd, enable: backdrop == BackdropType.Glass);
    }

    public static void EnableWindowBorder(nint hWnd, bool enable)
    {
        var color = enable ? DWMWA_COLOR_DEFAULT : DWMWA_COLOR_NONE;
        SetWindowAttribute(hWnd, DWMWA_BORDER_COLOR, ref color, sizeof(uint));
    }

    public static void EnableWindowTransparency(nint hWnd, bool enable)
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

    public static void EnableWindowBlur(nint hWnd, Color fallback, bool enable)
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
}
