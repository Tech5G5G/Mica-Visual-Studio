using System;
using System.Runtime.InteropServices;

namespace MicaVisualStudio.Interop;

/// <summary>
/// Represents an event hook for Windows events.
/// </summary>
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

    public const uint WINEVENT_OUTOFCONTEXT = 0;

    public const uint EVENT_OBJECT_SHOW = 0x8002,
        EVENT_OBJECT_CREATE = 0x8000,
        EVENT_OBJECT_DESTROY = 0x8001;

    private delegate void WinEventDelegate(
        nint hWinEventHook,
        int eventConst,
        nint hWnd,
        int idObject,
        int idChild,
        int idEventThread,
        int dwmsEventTime);

    #endregion

    /// <summary>
    /// Occurs when the specified <see cref="Event"/> occurs.
    /// </summary>
    public event EventOccuredEventHandler EventOccurred;

    private readonly nint _hookId;
    private readonly WinEventDelegate _hook;

    /// <summary>
    /// Initializes a new instance of <see cref="WinEventHook"/>.
    /// </summary>
    /// <param name="winEvent">The <see cref="Event"/> to look out for.</param>
    /// <param name="flags">The location of this <see cref="WinEventHook"/> and the sources to be skipped.</param>
    /// <param name="pid">The ID of the process to watch. Set to <c>0</c> to watch all processes.</param>
    public WinEventHook(Event winEvent, EventFlags flags, int pid)
    {
        _hookId = SetWinEventHook((uint)winEvent, (uint)winEvent, IntPtr.Zero, _hook = Procedure, (uint)pid, idThread: 0, (uint)flags);
    }

    private void Procedure(IntPtr hWinEventHook, int eventConst, IntPtr hWnd, int idObject, int idChild, int idEventThread, int dwmsEventTime)
    {
        EventOccurred?.Invoke(this, new(eventConst, hWnd, idObject, idChild, dwmsEventTime));
    }

    #region Dispose

    private bool _disposed;

    ~WinEventHook()
    {
        DisposeInternal(/* disposing: false */);
    }

    /// <summary>
    /// Disposes this instance of <see cref="WinEventHook"/>, unregistering the associated hook.
    /// </summary>
    public void Dispose()
    {
        DisposeInternal(/* disposing: true */);
        GC.SuppressFinalize(this);
    }

    private void DisposeInternal(/* bool disposing */)
    {
        if (!_disposed)
        {
            UnhookWinEvent(_hookId);
            _disposed = true;
        }
    }

    #endregion
}

/// <summary>
/// Specifies the event used by <see cref="WinEventHook"/>.
/// </summary>
public enum Event
{
    /// <summary>
    /// Specifies to listen to foreground window changes.
    /// </summary>
    /// <remarks>Equivalent to EVENT_SYSTEM_FOREGROUND.</remarks>
    Foreground = 0x0003,

    /// <summary>
    /// Specifies to listen to the creation of objects.
    /// </summary>
    /// <remarks>Equivalent to EVENT_OBJECT_CREATE.</remarks>
    Create = 0x8000,

    /// <summary>
    /// Specifies to listen to the destruction of objects.
    /// </summary>
    /// <remarks>Equivalent to EVENT_OBJECT_DESTROY.</remarks>
    Destroy = 0x8001,

    /// <summary>
    /// Specifies to listen to hidden objects being shown.
    /// </summary>
    /// <remarks>Equivalent to EVENT_OBJECT_SHOW.</remarks>
    Show = 0x8002
}

/// <summary>
/// Specifies the location of a hook and the sources to be skipped.
/// </summary>
[Flags]
public enum EventFlags
{
    /// <summary>
    /// Specifies to place the hook outside of the process of the hookee. This is the default.
    /// </summary>
    /// <remarks>Equivalent to WINEVENT_INCONTEXT.</remarks>
    OutOfContext = 0x0000,

    /// <summary>
    /// Specifies to skip events that occur in the same thread as the thread registering the hook.
    /// </summary>
    /// <remarks>Equivalent to WINEVENT_SKIPOWNTHREAD.</remarks>
    SkipOwnThread = 0x0001,

    /// <summary>
    /// Specifies to skip events that occur in the same process as the process registering the hook.
    /// </summary>
    /// <remarks>Equivalent to WINEVENT_SKIPOWNPROCESS.</remarks>
    SkipOwnProcess = 0x0002,

    /// <summary>
    /// Specifies to place the hook inside the process of the hookee.
    /// </summary>
    /// <remarks>Equivalent to WINEVENT_OUTOFCONTEXT.</remarks>
    InContext = 0x0004,
}

/// <summary>
/// Represents the <see cref="EventArgs"/> for the <see cref="WinEventHook.EventOccurred"/> event.
/// </summary>
/// <param name="eventConst">An <see cref="int"/> representation of the <see cref="Interop.Event"/> that occured.</param>
/// <param name="hWnd">The handle to the window that generated the <see cref="Interop.Event"/>.</param>
/// <param name="idObject">The ID of the object that generated the <see cref="Interop.Event"/>.</param>
/// <param name="idChild">The ID of the child that generated the <see cref="Interop.Event"/>.</param>
/// <param name="dwmsEventTime">The amount of time, in milliseconds, that the <see cref="Interop.Event"/> was generated in.</param>
public class EventOccuredEventArgs(int eventConst, IntPtr hWnd, int idObject, int idChild, int dwmsEventTime) : EventArgs
{
    private const int CHILDID_SELF = 0;

    /// <summary>
    /// The <see cref="Interop.Event"/> that occured.
    /// </summary>
    public Event Event { get; } = (Event)eventConst;

    /// <summary>
    /// The handle to the window associated with the event.
    /// </summary>
    public IntPtr WindowHandle { get; } = hWnd;

    /// <summary>
    /// The ID of the object associated with the event.
    /// </summary>
    public int ObjectId { get; } = idObject;

    /// <summary>
    /// Whether the event was triggered by a child.
    /// </summary>
    public bool TriggeredByChild { get; } = idChild != CHILDID_SELF;

    /// <summary>
    /// The amount of time, in milliseconds, which it took for the <see cref="Interop.Event"/> to be generated.
    /// </summary>
    public int EventTime { get; } = dwmsEventTime;
}

/// <summary>
/// Represents the handler for the <see cref="WinEventHook.EventOccurred"/> event.
/// </summary>
/// <param name="sender">The <see cref="WinEventHook"/> that generated the event.</param>
/// <param name="args">The <see cref="EventOccuredEventArgs"/> to go along with the event.</param>
public delegate void EventOccuredEventHandler(WinEventHook sender, EventOccuredEventArgs args);
