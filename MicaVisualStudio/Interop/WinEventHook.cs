namespace MicaVisualStudio.Interop;

public class WinEventHook : IDisposable
{
    #region PInvoke

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(
        uint eventMin,
        uint eventMax,
        IntPtr hmodWinEventProc,
        WinEventDelegate pfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    public const uint WINEVENT_OUTOFCONTEXT = 0;

    public const uint EVENT_OBJECT_SHOW = 0x8002,
        EVENT_OBJECT_CREATE = 0x8000,
        EVENT_OBJECT_DESTROY = 0x8001;

    private delegate void WinEventDelegate(
        IntPtr hWinEventHook,
        int eventConst,
        IntPtr hWnd,
        int idObject,
        int idChild,
        int idEventThread,
        int dwmsEventTime);

    #endregion

    public event EventOccuredEventHandler EventOccurred;

    private readonly IntPtr hookId;
    private readonly WinEventDelegate hook;

    public WinEventHook(Event winEvent, EventFlags flags, int pid) =>
        hookId = SetWinEventHook((uint)winEvent, (uint)winEvent, IntPtr.Zero, hook = Procedure, (uint)pid, idThread: 0, (uint)flags);

    private void Procedure(IntPtr hWinEventHook, int eventConst, IntPtr hWnd, int idObject, int idChild, int idEventThread, int dwmsEventTime) =>
        EventOccurred?.Invoke(this, new(eventConst, hWnd, idObject, idChild, dwmsEventTime));

    #region Dispose

    private bool disposed;

    ~WinEventHook() => Dispose(disposing: false);

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            UnhookWinEvent(hookId);
            disposed = true;
        }
    }

    #endregion
}

public enum Event
{
    Foreground = 0x0003,
    Create = 0x8000,
    Destroy = 0x8001,
    Show = 0x8002
}

[Flags]
public enum EventFlags
{
    OutOfContext = 0x0000,
    SkipOwnThread = 0x0001,
    SkipOwnProcess = 0x0002,
    InContext = 0x0004,
}

public class EventOccuredEventArgs(int eventConst, IntPtr hWnd, int idObject, int idChild, int dwmsEventTime) : EventArgs
{
    private const int CHILDID_SELF = 0;

    public Event Event { get; } = (Event)eventConst;

    public IntPtr WindowHandle { get; } = hWnd;

    public int ObjectId { get; } = idObject;

    public bool TriggeredByChild { get; } = idChild != CHILDID_SELF;

    public int EventTime { get; } = dwmsEventTime;
}

public delegate void EventOccuredEventHandler(WinEventHook sender, EventOccuredEventArgs args);
