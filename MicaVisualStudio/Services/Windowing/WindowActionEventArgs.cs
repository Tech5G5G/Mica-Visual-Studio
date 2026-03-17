using System;
using System.Windows;

namespace MicaVisualStudio.Services.Windowing;

public sealed class WindowActionEventArgs(nint handle, Window window) : EventArgs
{
    public nint WindowHandle { get; } = handle;

    public Window Window { get; } = window;
}
