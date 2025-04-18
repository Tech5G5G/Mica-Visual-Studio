using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace MicaVisualStudio.Helpers
{
    public class ThemeHelper
    {
        public Theme Theme => (int)Registry.GetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "AppsUseLightTheme", 0) == 1 ? Theme.Light : Theme.Dark;

        public event Action<Theme> ThemeChanged;

        #region PInvoke

        [DllImport("Comctl32.dll", SetLastError = true)]
        private static extern bool SetWindowSubclass(IntPtr hWnd, SubclassDelegate pfnSubclass, uint uIdSubclass, uint dwRefData);

        [DllImport("Comctl32.dll", SetLastError = true)]
        private static extern int DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        const int WM_WININICHANGE = 0x001A;
        const int WM_SETTINGCHANGE = WM_WININICHANGE;

        private delegate int SubclassDelegate(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, uint dwRefData);

        #endregion

        readonly SubclassDelegate subclass;

        public ThemeHelper(IntPtr hWnd) => SetWindowSubclass(hWnd, subclass = new SubclassDelegate(WindowSubclass), 0, 0);

        private int WindowSubclass(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, uint dwRefData)
        {
            if (uMsg == WM_SETTINGCHANGE)
                ThemeChanged?.Invoke(Theme);

            return DefSubclassProc(hWnd, uMsg, wParam, lParam);
        } 
    }
}
