namespace MicaVisualStudio.VisualStudio;

public sealed class WindowManager : IDisposable
{
    public static WindowManager Instance { get; } = new();

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

    public ReadOnlyDictionary<IntPtr, (WindowType Type, Window Window)> Windows => new(
        windows.Select(i => (i.Key, (i.Value.Type, i.Value.Window.TryGetTarget(out Window window) ? window : null))).ToDictionary(i => i.Key, i => i.Item2));

    public event WindowChangedEventHandler WindowOpened;
    public event WindowChangedEventHandler WindowClosed;

    private readonly WinEventHook hook;
    private readonly Dictionary<IntPtr, (WindowType Type, WeakReference<Window> Window)> windows = [];

    private WindowManager()
    {
        hook = new(Event.Foreground, EventFlags.OutOfContext, Process.GetCurrentProcess().Id);
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
        if (sender is Window window)
            AddWindow(window, WindowHelper.GetWindowType(window));
    }

    private void WindowUnloaded(object sender, RoutedEventArgs args)
    {
        if (sender is Window window)
            RemoveWindow(window);
    }

    private void EventOccurred(WinEventHook sender, EventOccuredEventArgs args)
    {
        if (WindowHelper.GetWindowStyles(args.WindowHandle).HasFlag(WindowStyles.Caption) && //Check window for title bar
            !windows.ContainsKey(args.WindowHandle)) //Prefer WPF over WinEventHook and avoid duplicates
        {
            var window = HwndSource.FromHwnd(args.WindowHandle) is HwndSource source ? source.RootVisual as Window : null;
            var type = WindowHelper.GetWindowType(window);

            windows.Add(args.WindowHandle, (type, new(window)));
            WindowOpened?.Invoke(window, new(args.WindowHandle, type));
        }
    }

    public void AddWindow(Window window, WindowType type)
    {
        var handle = window.GetHandle();

        windows.Add(handle, (type, new(window)));
        WindowOpened?.Invoke(window, new(handle, type));
    }

    public void RemoveWindow(Window window)
    {
        if (windows.FirstOrDefault(i => i.Value.Window.TryGetTarget(out Window w) && w == window) is
            KeyValuePair<IntPtr, (WindowType Type, WeakReference<Window> Window)> pair)
        {
            windows.Remove(pair.Key);
            WindowClosed?.Invoke(window, new(pair.Key, pair.Value.Type));
        }
    }

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

public delegate void WindowChangedEventHandler(Window sender, WindowChangedEventArgs args);

public class WindowChangedEventArgs(IntPtr hWnd, WindowType type) : EventArgs
{
    public IntPtr WindowHandle { get; } = hWnd;

    public WindowType WindowType { get; } = type;
}
