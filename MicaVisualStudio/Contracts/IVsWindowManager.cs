using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.VisualStudio.Shell.Interop;

namespace MicaVisualStudio.Contracts;

public interface IVsWindowManager
{
    IReadOnlyList<Window> Windows { get; }

    event EventHandler<Window> WindowOpened;
    event EventHandler<Window> WindowClosed;

    IReadOnlyList<IVsWindowFrame> WindowFrames { get; }

    event WindowFrameEventHandler<object> FrameCreated;
    event WindowFrameEventHandler<object> FrameDestroyed;
    event WindowFrameEventHandler<bool> FrameIsVisibleChanged;
    event WindowFrameEventHandler<bool> FrameIsOnScreenChanged;
    event WindowFrameEventHandler<IVsWindowFrame> ActiveFrameChanged;
}

public delegate void WindowFrameEventHandler<TEventArgs>(IVsWindowFrame sender, TEventArgs args);
