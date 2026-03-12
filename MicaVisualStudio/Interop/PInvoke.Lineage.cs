using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MicaVisualStudio.Interop;

internal partial class PInvoke
{
    [DllImport("user32.dll")]
    private static extern nint GetParent(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(nint hWndParent, EnumChildProc lpEnumFunc, nint lParam);

    private delegate bool EnumChildProc(nint hwnd, nint lParam);

    public static nint GetOwner(nint hWnd)
    {
        return GetParent(hWnd);
    }

    public static IEnumerable<nint> GetChildren(nint hWnd)
    {
        List<nint> handles = [];
        EnumChildWindows(hWnd, Proc, IntPtr.Zero);
        return handles;

        bool Proc(nint hwnd, nint lParam)
        {
            handles.Add(hwnd);
            return true;
        }
    }
}
