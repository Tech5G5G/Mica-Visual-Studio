namespace MicaVisualStudio.Helpers;

/// <summary>
/// Handles <see cref="IVsShell"/> property changes.
/// </summary>
public class VsEventsHelper : IVsShellPropertyEvents
{
    /// <summary>
    /// Occurs when the visibility of the main window changes.
    /// </summary>
    public event EventHandler<MainWindowVisChangedEventArgs> MainWindowVisChanged;

    public int OnShellPropertyChange(int propid, object var)
    {
        switch (propid)
        {
            case (int)__VSSPROPID2.VSSPROPID_MainWindowVisibility:
                MainWindowVisChanged?.Invoke(this, new MainWindowVisChangedEventArgs(
                    Process.GetCurrentProcess().MainWindowHandle,
                    (bool)var));
                break;
        }

        return VSConstants.S_OK;
    }

    /// <summary>
    /// Creates an instance of <see cref="VsEventsHelper"/> for an <see cref="AsyncPackage"/>. Must be called from the UI thread.
    /// </summary>
    /// <param name="package">The instance of <see cref="AsyncPackage"/> to use.</param>
    /// <returns>If no errors occured, an instance of <see cref="VsEventsHelper"/>, asynchronously. Otherwise, <see langword="null"/>.</returns>
    public static async System.Threading.Tasks.Task<VsEventsHelper> CreateAsync(AsyncPackage package, CancellationToken cancellationToken)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        if (package is null ||
            await package.GetServiceAsync(typeof(SVsShell)) is not IVsShell shell)
            return null;

        var listener = new VsEventsHelper();
        shell.AdviseShellPropertyChanges(listener, out _);
        return listener;
    }
}

/// <summary>
/// Event arguments for <see cref="VsEventsHelper.MainWindowVisChanged"/>.
/// </summary>
/// <param name="handle">The handle of the main window.</param>
/// <param name="visible">Whether the main window is visible.</param>
public class MainWindowVisChangedEventArgs(nint handle, bool visible) : EventArgs
{
    /// <summary>
    /// Gets the handle of the main window.
    /// </summary>
    public nint MainWindowHandle { get; } = handle;

    /// <summary>
    /// Gets whether the main window is visible.
    /// </summary>
    public bool MainWindowVisible { get; } = visible;
}
