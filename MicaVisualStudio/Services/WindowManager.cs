using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using MicaVisualStudio.Interop;
using MicaVisualStudio.Windowing;
using MicaVisualStudio.Extensions;

namespace MicaVisualStudio.Services;

public class WindowManager : IWindowManager, IVsWindowFrameEvents, IDisposable
{
    public IReadOnlyDictionary<nint, Window> Windows
    {
        get
        {
            CleanHandles();
            var sources = PresentationSource.CurrentSources.OfType<HwndSource>()
                                                           .ToDictionary(s => s.Handle);

            return _handles.ToDictionary(h => h, h => sources.TryGetValue(h, out var source) ? source.RootVisual as Window : null);
        }
    }
    private readonly List<nint> _handles = [];

    public IReadOnlyList<IVsWindowFrame> WindowFrames => new ReadOnlyCollection<IVsWindowFrame>([.. _frames]);
    private readonly WeakCollection<IVsWindowFrame> _frames = [];

    public event EventHandler<WindowActionEventArgs> WindowOpened;
    public event EventHandler<WindowActionEventArgs> WindowClosed;

    public event WindowFrameEventHandler<object> FrameCreated;
    public event WindowFrameEventHandler<object> FrameDestroyed;
    public event WindowFrameEventHandler<bool> FrameIsVisibleChanged;
    public event WindowFrameEventHandler<bool> FrameIsOnScreenChanged;
    public event WindowFrameEventHandler<IVsWindowFrame> ActiveFrameChanged;

    private readonly IVsUIShell _shell;
    private readonly IVsUIShell7 _shell7;

    private readonly WinEventHook _hook;
    private readonly int _pid = Process.GetCurrentProcess().Id;

    private readonly uint _cookie;

    public WindowManager(IVsUIShell shell, IVsUIShell7 shell7)
    {
        _shell = shell;
        _shell7 = shell7;

        ThreadHelper.ThrowIfNotOnUIThread();
        _cookie = shell7.AdviseWindowFrameEvents(this);

        _hook = new(Event.Foreground, EventFlags.OutOfContext, _pid);
        _hook.EventOccurred += OnEventOccurred;

        EventManager.RegisterClassHandler(
            typeof(Window),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnWindowLoaded));

        EventManager.RegisterClassHandler(
            typeof(Window),
            FrameworkElement.UnloadedEvent,
            new RoutedEventHandler(OnWindowUnloaded));

        GetAllWindows();
        GetAllWindowFrames();
    }

    private void GetAllWindows() =>
        _handles.AddRange(
            Application.Current.Windows.OfType<Window>()
                                       .Select(w => w.GetHandle()));

    private void GetAllWindowFrames()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        _shell.GetToolWindowEnum(out var toolEnum);
        _shell.GetDocumentWindowEnum(out var docEnum);

        foreach (var frame in toolEnum.ToEnumerable().Concat(docEnum.ToEnumerable()))
        {
            _frames.Add(frame);
        }
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs args)
    {
        if (sender is Window window)
        {
            var handle = window.GetHandle();

            if (!_handles.Contains(handle))
            {
                _handles.Add(handle);
            }

            WindowOpened?.Invoke(this, new(handle, window));
        }
    }

    private void OnWindowUnloaded(object sender, RoutedEventArgs args)
    {
        if (sender is Window window)
        {
            CleanHandles();
            WindowClosed?.Invoke(this, new(IntPtr.Zero, window));
        }
    }

    private void OnEventOccurred(WinEventHook sender, EventOccuredEventArgs args)
    {
        if (!_handles.Contains(args.WindowHandle) && // Prefer WPF over WinEventHook and avoid duplicates
            WindowHelper.GetWindowStyles(args.WindowHandle).HasFlag(WindowStyles.Caption)) // Check window for title bar
        {
            _handles.Add(args.WindowHandle);

            var window = HwndSource.FromHwnd(args.WindowHandle) is HwndSource source ? source.RootVisual as Window : null;
            WindowOpened?.Invoke(this, new(args.WindowHandle, window));
        }
    }

    private void CleanHandles() =>
        _handles.RemoveAll(h =>
            !WindowHelper.IsAlive(h) || // Check if alive...
            WindowHelper.GetProcessId(h) != _pid); // and belongs to current process

    #region IVsWindowFrameEvents

    void IVsWindowFrameEvents.OnFrameCreated(IVsWindowFrame frame)
    {
        _frames.Add(frame);
        FrameCreated?.Invoke(frame, args: null);
    }

    void IVsWindowFrameEvents.OnFrameDestroyed(IVsWindowFrame frame)
    {
        _frames.Remove(frame);
        FrameDestroyed?.Invoke(frame, args: null);
    }

    void IVsWindowFrameEvents.OnFrameIsVisibleChanged(IVsWindowFrame frame, bool newIsVisible) =>
        FrameIsVisibleChanged?.Invoke(frame, newIsVisible);

    void IVsWindowFrameEvents.OnFrameIsOnScreenChanged(IVsWindowFrame frame, bool newIsOnScreen) =>
        FrameIsOnScreenChanged?.Invoke(frame, newIsOnScreen);

    void IVsWindowFrameEvents.OnActiveFrameChanged(IVsWindowFrame oldFrame, IVsWindowFrame newFrame) =>
        ActiveFrameChanged?.Invoke(oldFrame, newFrame);

    #endregion

    #region Dispose

    private bool _disposed;

    void IDisposable.Dispose()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (!_disposed)
        {
            _shell7.UnadviseWindowFrameEvents(_cookie);
            _hook.Dispose();

            _frames.Clear();
            _handles.Clear();

            _disposed = true;
        }
    }

    #endregion
}
