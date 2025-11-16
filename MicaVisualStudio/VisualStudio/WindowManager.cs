namespace MicaVisualStudio.VisualStudio;

public sealed class WindowManager : IDisposable
{
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

    public static WindowManager Instance { get; } = new();

    public Dictionary<IntPtr, (WindowType Type, Window Window)> Windows => windows;
    private readonly Dictionary<IntPtr, (WindowType Type, Window Window)> windows = [];

    public event WindowChangedEventHandler WindowOpened;
    public event WindowChangedEventHandler WindowClosed;

    private readonly WinEventHook hook;

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
        {
            var handle = window.GetHandle();
            var type = WindowHelper.GetWindowType(window);

            windows.Add(handle, (type, window));
            WindowOpened?.Invoke(window, new(handle, type));
        }
    }

    private void WindowUnloaded(object sender, RoutedEventArgs args)
    {
        if (sender is Window window &&
            windows.FirstOrDefault(i => i.Value.Window == window) is KeyValuePair<IntPtr, (WindowType Type, Window Window)> pair)
        {
            windows.Remove(pair.Key);
            WindowClosed?.Invoke(window, new(pair.Key, pair.Value.Type));
        }
    }

    private void EventOccurred(WinEventHook sender, EventOccuredEventArgs args)
    {
        if (!windows.ContainsKey(args.WindowHandle) && //Prefer WPF over WinEventHook and avoid duplicates
            WindowHelper.GetWindowStyles(args.WindowHandle).HasFlag(WindowStyles.Caption)) //Check window for title bar 
        {
            var window = HwndSource.FromHwnd(args.WindowHandle) is HwndSource source ? source.RootVisual as Window : null;
            var type = WindowHelper.GetWindowType(window);

            windows.Add(args.WindowHandle, (type, window));
            WindowOpened?.Invoke(window, new(args.WindowHandle, type));
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
