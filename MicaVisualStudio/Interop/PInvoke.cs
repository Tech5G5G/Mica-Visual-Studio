using System.Text;
using System.Runtime.InteropServices;

namespace MicaVisualStudio.Interop;

internal static partial class PInvoke
{
    [DllImport("user32.dll")]
    private static extern bool IsWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern int GetClassName(nint hWnd, StringBuilder lpClassName, int nMaxCount);

    public static bool IsAlive(nint hWnd)
    {
        return IsWindow(hWnd);
    }

    public static int GetProcessId(nint hWnd)
    {
        GetWindowThreadProcessId(hWnd, out uint procId);
        return (int)procId;
    }

    public static string GetClassName(nint hWnd)
    {
        StringBuilder builder = new(256);
        if (GetClassName(hWnd, builder, builder.Capacity) != 0)
        {
            return builder.ToString();
        }
        else
        {
            return string.Empty;
        }
    }
}
