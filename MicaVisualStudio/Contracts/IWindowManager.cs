using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.VisualStudio.Shell.Interop;
using MicaVisualStudio.Services.Windowing;

namespace MicaVisualStudio.Contracts;

public interface IWindowManager
{
    IReadOnlyDictionary<nint, Window> Windows { get; }

    IReadOnlyList<IVsWindowFrame> WindowFrames { get; }

    event EventHandler<WindowActionEventArgs> WindowOpened;
    event EventHandler<WindowActionEventArgs> WindowClosed;

    event WindowFrameEventHandler<object> FrameCreated;
    event WindowFrameEventHandler<object> FrameDestroyed;
    event WindowFrameEventHandler<bool> FrameIsVisibleChanged;
    event WindowFrameEventHandler<bool> FrameIsOnScreenChanged;
    event WindowFrameEventHandler<IVsWindowFrame> ActiveFrameChanged;
}
