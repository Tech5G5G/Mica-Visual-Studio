using System;
using System.Runtime.InteropServices;

namespace MicaVisualStudio.Interop;

public sealed class WinEventHook : IDisposable
{
    #region PInvoke

    [DllImport("user32.dll")]
    private static extern nint SetWinEventHook(
        uint eventMin,
        uint eventMax,
        nint hmodWinEventProc,
        WinEventDelegate pfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(nint hWinEventHook);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    private delegate void WinEventDelegate(
        nint hWinEventHook,
        int eventConst,
        nint hWnd,
        int idObject,
        int idChild,
        int idEventThread,
        int dwmsEventTime);

    #endregion

    public event EventOccuredEventHandler EventOccurred;

    private readonly nint _hookId;
    private readonly WinEventDelegate _hook;

    public WinEventHook(Event winEvent, EventFlags flags, int pid)
    {
        _hookId = SetWinEventHook((uint)winEvent, (uint)winEvent, IntPtr.Zero, _hook = Procedure, (uint)pid, idThread: 0, (uint)flags);
    }

    private void Procedure(nint hWinEventHook, int eventConst, nint hWnd, int idObject, int idChild, int idEventThread, int dwmsEventTime)
    {
        EventOccurred?.Invoke(this, new(eventConst, hWnd, idObject, idChild, dwmsEventTime));
    }

    #region Dispose

    private bool _disposed;

    ~WinEventHook()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            EventOccurred = null;
        }

        UnhookWinEvent(_hookId);
        _disposed = true;
    }

    #endregion
}

public enum Event
{
    Foreground = 0x0003,
    Create = 0x8000,
    Destroy = 0x8001,
    Show = 0x8002,
    ParentChange = 0x800F
}

[Flags]
public enum EventFlags
{
    OutOfContext = 0x0000,
    SkipOwnThread = 0x0001,
    SkipOwnProcess = 0x0002,
    InContext = 0x0004,
}

public sealed class EventOccuredEventArgs(int eventConst, nint hWnd, int idObject, int idChild, int dwmsEventTime) : EventArgs
{
    private const int CHILDID_SELF = 0;

    public Event Event { get; } = (Event)eventConst;

    public nint WindowHandle { get; } = hWnd;

    public int ObjectId { get; } = idObject;

    public bool TriggeredByChild { get; } = idChild != CHILDID_SELF;

    public int EventTime { get; } = dwmsEventTime;
}

public delegate void EventOccuredEventHandler(WinEventHook sender, EventOccuredEventArgs e);
