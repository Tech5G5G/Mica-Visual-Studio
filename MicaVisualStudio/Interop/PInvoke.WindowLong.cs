using System;
using System.Runtime.InteropServices;

namespace MicaVisualStudio.Interop;

internal partial class PInvoke
{
    [DllImport("user32.dll")]
    private static extern nint GetWindowLongW(nint hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern nint GetWindowLongPtrW(nint hWnd, int nIndex);

    private static nint GetWindowLong(nint hWnd, int nIndex)
    {
        return Environment.Is64BitProcess ? GetWindowLongPtrW(hWnd, nIndex) : GetWindowLongW(hWnd, nIndex);
    }

    [DllImport("user32.dll")]
    private static extern nint SetWindowLongW(nint hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll")]
    private static extern nint SetWindowLongPtrW(nint hWnd, int nIndex, uint dwNewLong);

    private static nint SetWindowLong(nint hWnd, int nIndex, uint dwNewLong)
    {
        return Environment.Is64BitProcess ? SetWindowLongPtrW(hWnd, nIndex, dwNewLong) : SetWindowLongW(hWnd, nIndex, dwNewLong);
    }

    private const int GWL_STYLE = -16, GWL_EXSTYLE = -20;

    /// <summary>
    /// Gets the <see cref="WindowStyle"/> of the specified <paramref name="hWnd"/>.
    /// </summary>
    /// <param name="hWnd">A handle to a window.</param>
    /// <returns>The <see cref="WindowStyle"/> of the specified <paramref name="hWnd"/>.</returns>
    public static WindowStyle GetWindowStyles(nint hWnd)
    {
        return (WindowStyle)GetWindowLong(hWnd, GWL_STYLE);
    }

    /// <summary>
    /// Sets the <see cref="WindowStyle"/> of the specified <paramref name="hWnd"/>.
    /// </summary>
    /// <param name="hWnd">A handle to a window.</param>
    /// <param name="style">The <see cref="WindowStyle"/> to set.</param>
    public static void SetWindowStyles(nint hWnd, WindowStyle style)
    {
        SetWindowLong(hWnd, GWL_STYLE, (uint)style);
    }

    /// <summary>
    /// Gets the <see cref="ExtendedWindowStyle"/> of the specified <paramref name="hWnd"/>.
    /// </summary>
    /// <param name="hWnd">A handle to a window.</param>
    /// <returns>The <see cref="ExtendedWindowStyle"/> of the specified <paramref name="hWnd"/>.</returns>
    public static ExtendedWindowStyle GetExtendedWindowStyles(nint hWnd)
    {
        return (ExtendedWindowStyle)GetWindowLong(hWnd, GWL_EXSTYLE);
    }

    /// <summary>
    /// Sets the <see cref="ExtendedWindowStyle"/> of the specified <paramref name="hWnd"/>.
    /// </summary>
    /// <param name="hWnd">A handle to a window.</param>
    /// <param name="style">The <see cref="ExtendedWindowStyle"/> to set.</param>
    public static void SetExtendedWindowStyles(nint hWnd, ExtendedWindowStyle style)
    {
        SetWindowLong(hWnd, GWL_EXSTYLE, (uint)style);
    }
}
