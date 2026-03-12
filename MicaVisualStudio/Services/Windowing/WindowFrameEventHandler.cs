using Microsoft.VisualStudio.Shell.Interop;

namespace MicaVisualStudio.Services.Windowing;

public delegate void WindowFrameEventHandler<TEventArgs>(IVsWindowFrame sender, TEventArgs e);
