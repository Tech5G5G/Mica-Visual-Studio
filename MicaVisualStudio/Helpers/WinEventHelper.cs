using System;
using System.Runtime.InteropServices;

namespace MicaVisualStudio.Helpers
{
    public delegate void WinEventDelegate(IntPtr hWinEventHook, int eventConst, IntPtr hWnd, int idObject, int idChild, int idEventThread, int dwmsEventTime);

    public class WinEventHelper
    {
        public const uint EVENT_OBJECT_SHOW = 0x8002;
        public const uint WINEVENT_OUTOFCONTEXT = 0;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate pfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        readonly IntPtr hookId;
        readonly WinEventDelegate hook;

        public WinEventHelper(WinEventDelegate proc, uint eventMin, uint eventMax, uint dwFlags) => hookId = SetWinEventHook(eventMin, eventMax, IntPtr.Zero, hook = proc, 0, 0, dwFlags);
        ~WinEventHelper() => UnhookWinEvent(hookId);
    }
}
