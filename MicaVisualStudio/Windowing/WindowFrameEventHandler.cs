using Microsoft.VisualStudio.Shell.Interop;

namespace MicaVisualStudio.Windowing;

public delegate void WindowFrameEventHandler<TEventArgs>(IVsWindowFrame sender, TEventArgs e);
