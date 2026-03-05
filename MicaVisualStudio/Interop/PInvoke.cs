using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MicaVisualStudio.Interop;

internal static partial class PInvoke
{
    [DllImport("user32.dll")]
    private static extern bool IsWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(nint hWndParent, EnumChildProc lpEnumFunc, nint lParam);

    private delegate bool EnumChildProc(nint hwnd, nint lParam);

    public static bool IsAlive(nint hWnd)
    {
        return IsWindow(hWnd);
    }

    public static int GetProcessId(nint hWnd)
    {
        GetWindowThreadProcessId(hWnd, out uint procId);
        return (int)procId;
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
