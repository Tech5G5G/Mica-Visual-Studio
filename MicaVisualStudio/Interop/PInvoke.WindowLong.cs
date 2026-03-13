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

    public static WindowStyle GetWindowStyles(nint hWnd)
    {
        return (WindowStyle)GetWindowLong(hWnd, GWL_STYLE);
    }

    public static void SetWindowStyles(nint hWnd, WindowStyle style)
    {
        SetWindowLong(hWnd, GWL_STYLE, (uint)style);
    }

    public static ExtendedWindowStyle GetExtendedWindowStyles(nint hWnd)
    {
        return (ExtendedWindowStyle)GetWindowLong(hWnd, GWL_EXSTYLE);
    }

    public static void SetExtendedWindowStyles(nint hWnd, ExtendedWindowStyle style)
    {
        SetWindowLong(hWnd, GWL_EXSTYLE, (uint)style);
    }
}
