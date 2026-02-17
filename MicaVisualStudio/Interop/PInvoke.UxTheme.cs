using System.Runtime.InteropServices;

namespace MicaVisualStudio.Interop;

internal static partial class PInvoke
{
    [DllImport("uxtheme.dll", EntryPoint = "#135")]
    private static extern int SetPreferredAppMode(PreferredAppMode preferredAppMode);

    [DllImport("uxtheme.dll", EntryPoint = "#136")]
    private static extern void FlushMenuThemes();

    public static void SetAppMode(PreferredAppMode mode)
    {
        SetPreferredAppMode(mode);
        FlushMenuThemes();
    }

    public enum PreferredAppMode
    {
        ForceDark = 2,
        ForceLight = 3
    }
}
