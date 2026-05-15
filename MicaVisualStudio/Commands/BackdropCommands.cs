using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Community.VisualStudio.Toolkit;
using Community.VisualStudio.Toolkit.DependencyInjection;
using Community.VisualStudio.Toolkit.DependencyInjection.Core;
using MicaVisualStudio.Options;

namespace MicaVisualStudio;

public abstract class BackdropCommand(DIToolkitPackage package, IGeneral general) : BaseDICommand(package)
{
    protected abstract BackdropType Backdrop { get; }

    private readonly IGeneral _general = general;

    protected async override Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        _general.Backdrop = Backdrop;
    }

    protected override void BeforeQueryStatus(EventArgs e)
    {
        Command.Checked = _general.Backdrop == Backdrop;
    }
}

[Command(CommandTable.MVSPackage.NoneCommandId)]
public sealed class NoneCommand(DIToolkitPackage package, IGeneral general) : BackdropCommand(package, general)
{
    protected override BackdropType Backdrop => BackdropType.None;
}

[Command(CommandTable.MVSPackage.MicaCommandId)]
public sealed class MicaCommand(DIToolkitPackage package, IGeneral general) : BackdropCommand(package, general)
{
    protected override BackdropType Backdrop => BackdropType.Mica;
}

[Command(CommandTable.MVSPackage.TabbedCommandId)]
public sealed class TabbedCommand(DIToolkitPackage package, IGeneral general) : BackdropCommand(package, general)
{
    protected override BackdropType Backdrop => BackdropType.Tabbed;
}

[Command(CommandTable.MVSPackage.AcrylicCommandId)]
public sealed class AcrylicCommand(DIToolkitPackage package, IGeneral general) : BackdropCommand(package, general)
{
    protected override BackdropType Backdrop => BackdropType.Acrylic;
}

[Command(CommandTable.MVSPackage.GlassCommandId)]
public sealed class GlassCommand(DIToolkitPackage package, IGeneral general) : BackdropCommand(package, general)
{
    protected override BackdropType Backdrop => BackdropType.Glass;
}
