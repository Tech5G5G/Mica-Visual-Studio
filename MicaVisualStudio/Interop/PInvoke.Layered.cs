using System.Drawing;
using System.Runtime.InteropServices;

namespace MicaVisualStudio.Interop;

internal partial class PInvoke
{
    [DllImport("user32.dll")]
    private static extern bool SetLayeredWindowAttributes(nint hwnd, uint crKey, byte bAlpha, uint dwFlags);

    private const uint LWA_ALPHA = 0x00000002;

    /// <summary>
    /// Makes the specified <paramref name="hWnd"/> layered by adding the <see cref="ExtendedWindowStyle.Layered"/> style.
    /// </summary>
    /// <param name="hWnd">A handle to a window.</param>
    public static void MakeLayered(nint hWnd)
    {
        SetExtendedWindowStyles(hWnd, GetExtendedWindowStyles(hWnd) | ExtendedWindowStyle.Layered);
        SetLayeredWindowAttributes(
            hWnd,
            (uint)ColorTranslator.ToWin32(Color.Black),
            0xFF, // Set opactiy to 100%
            LWA_ALPHA);
    }
}
