namespace MicaVisualStudio.VisualStudio;

public sealed class WindowObserver : IDisposable
{
    public static WindowObserver Instance { get; } = new();

    #region Static Properties

    public static Window MainWindow => Application.Current.MainWindow;

    public static Window CurrentWindow
    {
        get
        {
            var windows = Application.Current.Windows.OfType<Window>();
            return windows.FirstOrDefault(i => i.IsActive) ?? windows.FirstOrDefault(i => i.Visibility == Visibility.Visible);
        }
    }

    public static List<Window> AllWindows => [.. Application.Current.Windows.OfType<Window>()];

    #endregion

    public ReadOnlyDictionary<IntPtr, WindowInfo> Windows
    {
        get
        {
            CleanHandles();
            var sources = PresentationSource.CurrentSources.OfType<HwndSource>().ToArray();

            return new(handles.ToDictionary(i => i, i => new WindowInfo(sources.FirstOrDefault(x => x.Handle == i)?.RootVisual as Window)));
        }
    }

    public event WindowChangedEventHandler WindowOpened;
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
        if (!WindowHelper.GetWindowStyles(args.WindowHandle).HasFlag(WindowStyles.Caption) || //Check window for title bar
            handles.Contains(args.WindowHandle)) //Prefer WPF over WinEventHook and avoid duplicates
            return;

        handles.Add(args.WindowHandle);

        var window = HwndSource.FromHwnd(args.WindowHandle) is HwndSource source ? source.RootVisual as Window : null;
        WindowOpened?.Invoke(window, new(args.WindowHandle, window));
    }

    public void AppendWindow(Window window)
    {
        if (window.IsLoaded)
            AppendWindow(window.GetHandle());
    }

    public void AppendWindow(IntPtr handle) =>
        handles.Add(handle);

    private void CleanHandles() =>
        handles.RemoveWhere(i =>
            !WindowHelper.IsAlive(i) || //Check if alive
            WindowHelper.GetProcessId(i) != procId); //and belongs to current process

    #region Dispose

    private bool disposed;

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

public delegate void WindowChangedEventHandler(Window sender, WindowActionEventArgs args);

public class WindowActionEventArgs(IntPtr handle, Window window) : EventArgs
{
    public IntPtr WindowHandle { get; } = handle;

    public WindowType WindowType { get; } = WindowHelper.GetWindowType(window);
}

public class WindowInfo(Window window)
{
    public Window Window { get; } = window;

    public WindowType Type { get; } = WindowHelper.GetWindowType(window);
}
