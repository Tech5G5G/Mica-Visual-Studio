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
using Community.VisualStudio.Toolkit;
using MicaVisualStudio.Interop;
using MicaVisualStudio.Contracts;
using MicaVisualStudio.Extensions;

namespace MicaVisualStudio.Services;

public class VsWindowManager : IVsWindowManager, IVsWindowFrameEvents, IDisposable
{
    public IReadOnlyList<Window> Windows
    {
        get
        {
            CleanHandles();

            return new ReadOnlyCollection<Window>(
                [.. PresentationSource.CurrentSources.OfType<HwndSource>()
                                                     .Where(s => _handles.Contains(s.Handle))
                                                     .Select(i => i.RootVisual)
                                                     .OfType<Window>()]);

        }
    }
    private readonly List<nint> _handles = [];

    public IReadOnlyList<IVsWindowFrame> WindowFrames => new ReadOnlyCollection<IVsWindowFrame>([.. _frames]);
    private readonly WeakCollection<IVsWindowFrame> _frames = [];

    public event EventHandler<Window> WindowOpened;
    public event EventHandler<Window> WindowClosed;

    public event WindowFrameEventHandler<object> FrameCreated;
    public event WindowFrameEventHandler<object> FrameDestroyed;
    public event WindowFrameEventHandler<bool> FrameIsVisibleChanged;
    public event WindowFrameEventHandler<bool> FrameIsOnScreenChanged;
    public event WindowFrameEventHandler<IVsWindowFrame> ActiveFrameChanged;

    private readonly IVsUIShell _shell = VS.GetRequiredService<SVsUIShell, IVsUIShell>();
    private readonly IVsUIShell7 _shell7 = VS.GetRequiredService<SVsUIShell, IVsUIShell7>();

    private readonly WinEventHook _hook;
    private readonly int _pid = Process.GetCurrentProcess().Id;

    private readonly uint _cookie;

    public VsWindowManager()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        _cookie = _shell7.AdviseWindowFrameEvents(this);

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

            _handles.Add(handle);
            WindowOpened?.Invoke(this, window);
        }
    }

    private void OnWindowUnloaded(object sender, RoutedEventArgs args)
    {
        if (sender is Window window)
        {
            CleanHandles();
            WindowClosed?.Invoke(this, window);
        }
    }

    private void OnEventOccurred(WinEventHook sender, EventOccuredEventArgs args)
    {
        if (!_handles.Contains(args.WindowHandle) && // Prefer WPF over WinEventHook and avoid duplicates
            WindowHelper.GetWindowStyles(args.WindowHandle).HasFlag(WindowStyles.Caption)) // Check window for title bar
        {
            _handles.Add(args.WindowHandle);

            var window = HwndSource.FromHwnd(args.WindowHandle) is HwndSource source ? source.RootVisual as Window : null;
            WindowOpened?.Invoke(this, window);
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
