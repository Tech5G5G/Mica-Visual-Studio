using System.Drawing;
using System.Runtime.InteropServices;

namespace MicaVisualStudio.Interop;

internal partial class PInvoke
{
    [DllImport("user32.dll")]
    private static extern bool SetLayeredWindowAttributes(nint hwnd, uint crKey, byte bAlpha, uint dwFlags);

    private const uint LWA_ALPHA = 0x00000002;

    public static void AddLayeredAttributes(nint hWnd)
    {
        SetExtendedWindowStyles(hWnd, GetExtendedWindowStyles(hWnd) | ExtendedWindowStyle.Layered);
        SetLayeredWindowAttributes(
            hWnd,
            (uint)ColorTranslator.ToWin32(Color.Black),
            0xFF, // Set opacity to 100%
            LWA_ALPHA);
    }
}
