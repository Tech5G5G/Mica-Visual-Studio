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

    /// <summary>
    /// Determines whether the specified <paramref name="hWnd"/> is alive; that is, whether it is still valid.
    /// </summary>
    /// <param name="hWnd">A handle to a window.</param>
    /// <returns><see langword="true"/> if the specified <paramref name="hWnd"/> is still alive. Otherwise, <see langword="false"/>.</returns>
    public static bool IsAlive(nint hWnd)
    {
        return IsWindow(hWnd);
    }

    /// <summary>
    /// Gets the process ID associated with the specified <paramref name="hWnd"/>.
    /// </summary>
    /// <param name="hWnd">A handle to a window.</param>
    /// <returns>The ID of the process that owns the specified <paramref name="hWnd"/>.</returns>
    public static int GetProcessId(nint hWnd)
    {
        GetWindowThreadProcessId(hWnd, out uint procId);
        return (int)procId;
    }

    /// <summary>
    /// Enumerates the children of the specified <paramref name="hWnd"/>.
    /// </summary>
    /// <param name="hWnd">A handle to a window.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of window handles representing the children of the specified <paramref name="hWnd"/>.</returns>
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
