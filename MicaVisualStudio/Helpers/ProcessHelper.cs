namespace MicaVisualStudio.Helpers;

public static class ProcessHelper
{
    #region PInvoke

    [DllImport("user32")]
    private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    #endregion

    public static int GetWindowProcessID(IntPtr hWnd)
    {
        GetWindowThreadProcessId(hWnd, out int pid);
        return pid;
    }

    public static Process GetWindowProcess(IntPtr hWnd) => Process.GetProcessById(GetWindowProcessID(hWnd));
}
