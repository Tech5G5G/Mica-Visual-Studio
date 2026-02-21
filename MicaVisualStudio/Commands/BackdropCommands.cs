using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Community.VisualStudio.Toolkit;
using Community.VisualStudio.Toolkit.DependencyInjection;
using Community.VisualStudio.Toolkit.DependencyInjection.Core;
using MicaVisualStudio.Options;
using MicaVisualStudio.Contracts;

namespace MicaVisualStudio;

public abstract class BackdropCommand(DIToolkitPackage package, IGeneral general) : BaseDICommand(package)
{
    protected abstract BackdropType Backdrop { get; }

    private readonly IGeneral _general = general;

    protected async override Task ExecuteAsync(OleMenuCmdEventArgs args)
    {
        _general.Backdrop = Backdrop;
        _general.Save();
    }

    protected override void BeforeQueryStatus(EventArgs args) =>
        Command.Checked = _general.Backdrop == Backdrop;
}

[Command(PackageIds.NoneCommandId)]
public sealed class NoneCommand(DIToolkitPackage package, IGeneral general) : BackdropCommand(package, general)
{
    protected override BackdropType Backdrop => BackdropType.None;
}

[Command(PackageIds.MicaCommandId)]
public sealed class MicaCommand(DIToolkitPackage package, IGeneral general) : BackdropCommand(package, general)
{
    protected override BackdropType Backdrop => BackdropType.Mica;
}

[Command(PackageIds.TabbedCommandId)]
public sealed class TabbedCommand(DIToolkitPackage package, IGeneral general) : BackdropCommand(package, general)
{
    protected override BackdropType Backdrop => BackdropType.Tabbed;
}

[Command(PackageIds.AcrylicCommandId)]
public sealed class AcrylicCommand(DIToolkitPackage package, IGeneral general) : BackdropCommand(package, general)
{
    protected override BackdropType Backdrop => BackdropType.Acrylic;
}

[Command(PackageIds.GlassCommandId)]
public sealed class GlassCommand(DIToolkitPackage package, IGeneral general) : BackdropCommand(package, general)
{
    protected override BackdropType Backdrop => BackdropType.Glass;
}
