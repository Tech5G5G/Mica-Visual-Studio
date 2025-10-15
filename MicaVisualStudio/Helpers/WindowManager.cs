namespace MicaVisualStudio.Helpers;

public static class WindowManager
{
    public static Dictionary<IntPtr, (WindowType Type, Window Window)> Windows => windows;
    private static readonly Dictionary<IntPtr, (WindowType Type, Window Window)> windows = [];

    public static Window MainWindow => Application.Current.MainWindow;

    public static Window CurrentWindow
    {
        get
        {
            var windows = Application.Current.Windows.OfType<Window>();
            return windows.FirstOrDefault(i => i.IsActive) ?? windows.FirstOrDefault(i => i.Visibility == Visibility.Visible);
        }
    }

    public static event WindowChangedEventHandler WindowOpened;
    public static event WindowChangedEventHandler WindowClosed;

    private static readonly WinEventHook hook;

    static WindowManager()
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

    private static void WindowLoaded(object sender, RoutedEventArgs args)
    {
        if (sender is Window window)
        {
            var handle = window.GetHandle();
            var type = GetWindowType(handle, window);

            windows.Add(handle, (type, window));
            WindowOpened?.Invoke(window, new(handle, type));
        }
    }

    private static void WindowUnloaded(object sender, RoutedEventArgs args)
    {
        if (sender is Window window &&
            windows.FirstOrDefault(i => i.Value.Window == window) is KeyValuePair<IntPtr, (WindowType Type, Window Window)> pair)
        {
            windows.Remove(pair.Key);
            WindowClosed?.Invoke(window, new(pair.Key, pair.Value.Type));
        }
    }

    private static void EventOccurred(WinEventHook sender, EventOccuredEventArgs args)
    {
        if (!windows.ContainsKey(args.WindowHandle) && //Prefer WPF over WinEventHook and avoid duplicates
            WindowHelper.GetWindowStyles(args.WindowHandle).HasFlag(WindowStyles.Caption)) //Check window for title bar 
        {
            var window = HwndSource.FromHwnd(args.WindowHandle) is HwndSource source ? source.RootVisual as Window : null;
            var type = GetWindowType(args.WindowHandle, window);

            windows.Add(args.WindowHandle, (type, window));
            WindowOpened?.Invoke(window, new(args.WindowHandle, type));
        }
    }

    private static WindowType GetWindowType(IntPtr hWnd, Window window)
    {
        if (window == MainWindow)
            return WindowType.Main;

        var styles = WindowHelper.GetWindowStyles(hWnd);

        if (window is not null && //Check if window is WPF
            styles.HasFlag(WindowStyles.ThickFrame) && //Check window for resizable border
            (window.WindowStyle == WindowStyle.None || //Check window for custom title bar
            styles.HasFlag(WindowStyles.MaximizeBox))) //Or for maximize button 
            return WindowType.Tool;
        else
            return WindowType.Dialog;
    }
}

public enum WindowType
{
    Main,
    Tool,
    Dialog
}

public delegate void WindowChangedEventHandler(Window sender, WindowChangedEventArgs args);

public class WindowChangedEventArgs(IntPtr hWnd, WindowType type) : EventArgs
{
    public IntPtr WindowHandle { get; } = hWnd;

    public WindowType WindowType { get; } = type;
}
