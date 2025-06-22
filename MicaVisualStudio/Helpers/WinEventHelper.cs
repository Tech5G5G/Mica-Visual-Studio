namespace MicaVisualStudio.Helpers;

public class WinEventHelper
{
    #region PInvoke

    [DllImport("User32.dll", SetLastError = true)]
    private static extern IntPtr SetWinEventHook(uint eventMin,
                                                 uint eventMax,
                                                 IntPtr hmodWinEventProc,
                                                 WinEventDelegate pfnWinEventProc,
                                                 uint idProcess,
                                                 uint idThread,
                                                 uint dwFlags);

    [DllImport("User32.dll", SetLastError = true)]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    public const uint WINEVENT_OUTOFCONTEXT = 0;

    public const uint EVENT_OBJECT_SHOW = 0x8002,
        EVENT_OBJECT_CREATE = 0x8000,
        EVENT_OBJECT_DESTROY = 0x8001;

    #endregion

    readonly IntPtr hookId;
    readonly WinEventDelegate hook;

    public WinEventHelper(WinEventDelegate proc, uint winEvent, uint procId, uint dwFlags) =>
        hookId = SetWinEventHook(winEvent, winEvent, IntPtr.Zero, hook = proc, procId, 0, dwFlags);

    ~WinEventHelper() => UnhookWinEvent(hookId);
}

public delegate void WinEventDelegate(IntPtr hWinEventHook, int eventConst, IntPtr hWnd, int idObject, int idChild, int idEventThread, int dwmsEventTime);
