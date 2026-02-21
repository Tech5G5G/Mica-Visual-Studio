using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.VisualStudio.Shell.Interop;

namespace MicaVisualStudio.Windowing;

public interface IWindowManager
{
    IReadOnlyDictionary<nint, Window> Windows { get; }

    event EventHandler<WindowActionEventArgs> WindowOpened;
    event EventHandler<WindowActionEventArgs> WindowClosed;

    IReadOnlyList<IVsWindowFrame> WindowFrames { get; }

    event WindowFrameEventHandler<object> FrameCreated;
    event WindowFrameEventHandler<object> FrameDestroyed;
    event WindowFrameEventHandler<bool> FrameIsVisibleChanged;
    event WindowFrameEventHandler<bool> FrameIsOnScreenChanged;
    event WindowFrameEventHandler<IVsWindowFrame> ActiveFrameChanged;
}
