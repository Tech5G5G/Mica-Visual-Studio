using System.Windows.Forms;

namespace MicaVisualStudio.Helpers;

/// <summary>
/// Handles Windows shell events for window creation and destruction.
/// </summary>
public class ShellHelper : NativeWindow
{
    #region PInvoke

    [DllImport("user32.dll")]
    private static extern void SetTaskmanWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool RegisterShellHookWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern uint RegisterWindowMessage(string lpString);

    private const int HSHELL_WINDOWCREATED = 1,
        HSHELL_WINDOWDESTROYED = 2;

    #endregion

    private readonly uint WM_SHELLHOOK;

    /// <summary>
    /// Creates a new instance of <see cref="ShellHelper"/>.
    /// </summary>
    public ShellHelper()
    {
        CreateHandle(new() { ClassName = "static" });

        SetTaskmanWindow(Handle);
        if (RegisterShellHookWindow(Handle))
            WM_SHELLHOOK = RegisterWindowMessage("SHELLHOOK");
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_SHELLHOOK)
            switch ((int)m.WParam)
            {
                case HSHELL_WINDOWCREATED:
                    WindowCreated?.Invoke(this, new(m.LParam));
                    break;
                case HSHELL_WINDOWDESTROYED:
                    WindowDestroyed?.Invoke(this, new(m.LParam));
                    break;
            }

        base.WndProc(ref m);
    }

    /// <summary>
    /// Occurs when a window is created.
    /// </summary>
    public event EventHandler<WindowChangedEventArgs> WindowCreated;

    /// <summary>
    /// Occurs when a window is destroyed.
    /// </summary>
    public event EventHandler<WindowChangedEventArgs> WindowDestroyed;
}

/// <summary>
/// Event arguments for window change events.
/// </summary>
/// <param name="handle">The handle of the window that was changed.</param>
public class WindowChangedEventArgs(nint handle) : EventArgs
{
    /// <summary>
    /// Gets the handle of the window that was changed.
    /// </summary>
    public nint WindowHandle { get; } = handle;
}
