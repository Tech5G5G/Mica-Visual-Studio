using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;
using MicaVisualStudio.Interop;

namespace MicaVisualStudio.VisualStudio;

/// <summary>
/// Represents an observer that listens for opened and closed <see cref="Window"/>s.
/// </summary>
public sealed class WindowObserver : IDisposable
{
    /// <summary>
    /// Gets the singleton instance of <see cref="WindowObserver"/>.
    /// </summary>
    public static WindowObserver Instance => field ??= new();

    #region Static Properties

    /// <summary>
    /// Gets the main window of the current <see cref="Application"/>.
    /// </summary>
    public static Window MainWindow => Application.Current.MainWindow;

    /// <summary>
    /// Gets the current, focused window of the current <see cref="Application"/>.
    /// </summary>
    public static Window CurrentWindow
    {
        get
        {
            var windows = Application.Current.Windows.OfType<Window>();
            return windows.FirstOrDefault(i => i.IsActive) ?? windows.FirstOrDefault(i => i.Visibility == Visibility.Visible);
        }
    }

    /// <summary>
    /// Gets all of the windows of the current <see cref="Application"/>.
    /// </summary>
    public static List<Window> AllWindows => [.. Application.Current.Windows.OfType<Window>()];

    #endregion
    
    /// <summary>
    /// Gets a <see cref="ReadOnlyDictionary{TKey, TValue}"/> containing the <see cref="Window"/>s found that are still alive.
    /// </summary>
    public IReadOnlyDictionary<IntPtr, WindowInfo> Windows
    {
        get
        {
            CleanHandles();
            var sources = PresentationSource.CurrentSources.OfType<HwndSource>().ToArray();

            return handles.ToDictionary(i => i, i => new WindowInfo(sources.FirstOrDefault(x => x.Handle == i)?.RootVisual as Window));
        }
    }

    /// <summary>
    /// Occurs when a new <see cref="Window"/> is opened.
    /// </summary>
    public event WindowChangedEventHandler WindowOpened;
    /// <summary>
    /// Occurs when a <see cref="Window"/> is closed.
    /// </summary>
    public event WindowChangedEventHandler WindowClosed;

    private readonly int procId;
    private readonly WinEventHook hook;

    private readonly HashSet<IntPtr> handles = [];

    private WindowObserver()
    {
        hook = new(Event.Foreground, EventFlags.OutOfContext, procId = Process.GetCurrentProcess().Id);
        hook.EventOccurred += EventOccurred;

        EventManager.RegisterClassHandler(
            typeof(Window),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(WindowLoaded));

        EventManager.RegisterClassHandler(
            typeof(Window),
            FrameworkElement.UnloadedEvent,
            new RoutedEventHandler(WindowUnloaded));
    }

    private void WindowLoaded(object sender, RoutedEventArgs args)
    {
        if (sender is not Window window)
            return;

        var handle = window.GetHandle();

        handles.Add(handle);
        WindowOpened?.Invoke(window, new(handle, window));
    }

    private void WindowUnloaded(object sender, RoutedEventArgs args)
    {
        if (sender is not Window window)
            return;

        CleanHandles();
        WindowClosed?.Invoke(window, new(IntPtr.Zero, window));
    }

    private void EventOccurred(WinEventHook sender, EventOccuredEventArgs args)
    {
        if (!WindowHelper.GetWindowStyles(args.WindowHandle).HasFlag(WindowStyles.Caption) || // Check window for title bar
            handles.Contains(args.WindowHandle)) // Prefer WPF over WinEventHook and avoid duplicates
            return;

        handles.Add(args.WindowHandle);

        var window = HwndSource.FromHwnd(args.WindowHandle) is HwndSource source ? source.RootVisual as Window : null;
        WindowOpened?.Invoke(window, new(args.WindowHandle, window));
    }

    /// <summary>
    /// Appends <paramref name="window"/> to the end of <see cref="Windows"/>.
    /// </summary>
    /// <param name="window">A <see cref="Window"/> to append.</param>
    public void AppendWindow(Window window)
    {
        if (window.IsLoaded)
            AppendWindow(window.GetHandle());
    }

    /// <summary>
    /// Appends a <paramref name="handle"/> to a window to the end of <see cref="Windows"/>.
    /// </summary>
    /// <param name="handle">A handle to a window.</param>
    public void AppendWindow(IntPtr handle) =>
        handles.Add(handle);

    private void CleanHandles() =>
        handles.RemoveWhere(i =>
            !WindowHelper.IsAlive(i) || // Check if alive
            WindowHelper.GetProcessId(i) != procId); // and belongs to current process

    #region Dispose

    private bool disposed;

    /// <summary>
    /// Disposes the singleton instance of <see cref="WindowObserver"/>.
    /// </summary>
    /// <remarks>Calling this method will stop the <see cref="WindowOpened"/> and <see cref="WindowClosed"/> events from occuring.</remarks>
    public void Dispose()
    {
        if (!disposed)
        {
            hook.Dispose();
            disposed = true;
        }
    }

    #endregion
}

/// <summary>
/// Represents the handler for the <see cref="WindowObserver.WindowOpened"/> and <see cref="WindowObserver.WindowClosed"/> events.
/// </summary>
/// <param name="sender">The <see cref="Window"/> that generated the event.</param>
/// <param name="args">The <see cref="WindowActionEventArgs"/> to go along with the event.</param>
public delegate void WindowChangedEventHandler(Window sender, WindowActionEventArgs args);

/// <summary>
/// Represents the <see cref="EventArgs"/> for the <see cref="WindowObserver.WindowOpened"/> and <see cref="WindowObserver.WindowClosed"/> events.
/// </summary>
/// <param name="handle">A handle to a window.</param>
/// <param name="window">The <see cref="Window"/> that generated the event.</param>
public class WindowActionEventArgs(IntPtr handle, Window window) : EventArgs
{
    public IntPtr WindowHandle { get; } = handle;

    public WindowType WindowType { get; } = WindowHelper.GetWindowType(window);
}

/// <summary>
/// Represents information about a specified <see cref="System.Windows.Window"/>.
/// </summary>
/// <param name="window">A <see cref="System.Windows.Window"/> to get information from.</param>
public class WindowInfo(Window window)
{
    public Window Window { get; } = window;

    public WindowType Type { get; } = WindowHelper.GetWindowType(window);
}
