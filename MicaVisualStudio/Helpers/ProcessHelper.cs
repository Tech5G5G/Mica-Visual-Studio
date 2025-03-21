using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MicaVisualStudio.Helpers
{
    public static class ProcessHelper
    {
        [DllImport("user32")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        public static int GetWindowProcessID(IntPtr hWnd)
        {
            GetWindowThreadProcessId(hWnd, out int pid);
            return pid;
        }

        public static Process GetWindowProcess(IntPtr hWnd) => Process.GetProcessById(GetWindowProcessID(hWnd));
    }
}
