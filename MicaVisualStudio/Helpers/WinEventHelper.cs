namespace MicaVisualStudio.Helpers;

public class WinEventHelper
{
    public const uint EVENT_OBJECT_SHOW = 0x8002;
    public const uint WINEVENT_OUTOFCONTEXT = 0;

    public const uint EVENT_OBJECT_SHOW = 0x8002,
        EVENT_OBJECT_CREATE = 0x8000,
        EVENT_OBJECT_DESTROY = 0x8001;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    readonly IntPtr hookId;
    readonly WinEventDelegate hook;

    public WinEventHelper(WinEventDelegate proc, uint eventMin, uint eventMax, uint dwFlags) => hookId = SetWinEventHook(eventMin, eventMax, IntPtr.Zero, hook = proc, 0, 0, dwFlags);
    ~WinEventHelper() => UnhookWinEvent(hookId);
}

public delegate void WinEventDelegate(IntPtr hWinEventHook, int eventConst, IntPtr hWnd, int idObject, int idChild, int idEventThread, int dwmsEventTime);
