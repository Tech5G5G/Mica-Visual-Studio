using System;
using System.Windows;

namespace MicaVisualStudio.Windowing;

public class WindowActionEventArgs(nint handle, Window window) : EventArgs
{
    public nint WindowHandle { get; } = handle;

    public Window Window { get; } = window;
}
