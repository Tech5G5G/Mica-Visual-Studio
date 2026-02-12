using Microsoft.VisualStudio.Shell.Interop;

namespace MicaVisualStudio.VisualStudio;

public partial class VsWindowStyler
{
    private uint cookie;

    public void OnFrameCreated(IVsWindowFrame frame) { }

    public void OnFrameDestroyed(IVsWindowFrame frame) { }

    public void OnFrameIsVisibleChanged(IVsWindowFrame frame, bool newIsVisible) { }

    public void OnFrameIsOnScreenChanged(IVsWindowFrame frame, bool newIsOnScreen)
    {
        if (newIsOnScreen)
            ApplyToWindowFrame(frame);
    }

    public void OnActiveFrameChanged(IVsWindowFrame oldFrame, IVsWindowFrame newFrame)
    {
        if (newFrame is not null)
            ApplyToWindowFrame(newFrame);
    }
}