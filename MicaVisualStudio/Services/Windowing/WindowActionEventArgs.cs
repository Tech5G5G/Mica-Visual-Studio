using System;
using System.Windows;

namespace MicaVisualStudio.Services.Windowing;

public class WindowActionEventArgs(nint handle, Window window) : EventArgs
{
    public nint WindowHandle { get; } = handle;

    public Window Window { get; } = window;
}
