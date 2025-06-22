namespace MicaVisualStudio.Composition;

public static class CompositionExtensions
{
    public static void CreateDesktopWindowTarget(this Compositor compositor, nint hWnd, bool isTopmost, out DesktopWindowTarget target)
    {
        var interop = (ICompositorDesktopInterop)(object)compositor;
        interop.CreateDesktopWindowTarget(hWnd, isTopmost, out target);
    }

    [ComImport]
    [Guid("29E691FA-4567-4DCA-B319-D0F207EB6807")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ICompositorDesktopInterop
    {
        void CreateDesktopWindowTarget(IntPtr hwndTarget, bool isTopmost, out DesktopWindowTarget target);
    }
}
