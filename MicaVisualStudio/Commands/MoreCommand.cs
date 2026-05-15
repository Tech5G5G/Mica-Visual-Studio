using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Community.VisualStudio.Toolkit;
using Community.VisualStudio.Toolkit.DependencyInjection;
using Community.VisualStudio.Toolkit.DependencyInjection.Core;

namespace MicaVisualStudio;

[Command(CommandTable.MVSPackage.MoreCommandId)]
public sealed class MoreCommand(DIToolkitPackage package) : BaseDICommand(package)
{
    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        await VS.Settings.OpenAsync<OptionsProvider.GeneralOptions>();
    }
}
