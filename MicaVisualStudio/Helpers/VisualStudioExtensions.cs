namespace MicaVisualStudio.Helpers;

public static class VisualStudioExtensions
{
    public static async System.Threading.Tasks.Task<IVsShell> GetVsShellAsync(this AsyncPackage package)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        return await package.GetServiceAsync<SVsShell, IVsShell>(throwOnFailure: false);
    }

    public static async System.Threading.Tasks.Task<IVsInfoBarUIFactory> GetVsInfoBarFactoryAsync(this AsyncPackage package)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        return await package.GetServiceAsync<SVsInfoBarUIFactory, IVsInfoBarUIFactory>(throwOnFailure: false);
    }

    public static async Task ShowInfoBarAsync(
        this AsyncPackage package,
        string content,
        ImageMoniker image,
        IVsShell shell)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        if (shell is not null &&
            await package.GetVsInfoBarFactoryAsync() is IVsInfoBarUIFactory factory &&
            ErrorHandler.Succeeded(shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out object pvar)) &&
            pvar is IVsInfoBarHost host)
            host.AddInfoBar(factory.CreateInfoBar(infoBar: new InfoBarModel(content, image)));
    }
}
