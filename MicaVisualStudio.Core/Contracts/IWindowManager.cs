using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.VisualStudio.Shell.Interop;

namespace MicaVisualStudio.Contracts;

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

public delegate void WindowFrameEventHandler<TEventArgs>(IVsWindowFrame sender, TEventArgs args);

public class WindowActionEventArgs(nint handle, Window window) : EventArgs
{
    public Window Window { get; } = window;

    public nint WindowHandle { get; } = handle;
}
