using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Community.VisualStudio.Toolkit;
using Community.VisualStudio.Toolkit.DependencyInjection;
using Community.VisualStudio.Toolkit.DependencyInjection.Core;
using MicaVisualStudio.Interop;
using MicaVisualStudio.Contracts;

namespace MicaVisualStudio;

public abstract class BackdropCommand(DIToolkitPackage package, IBackdropManager manager) : BaseDICommand(package)
{
    protected abstract BackdropType Backdrop { get; }

    private readonly IBackdropManager _manager = manager;

    protected async override Task ExecuteAsync(OleMenuCmdEventArgs args) =>
        _manager.Backdrop = Backdrop;

    protected override void BeforeQueryStatus(EventArgs args) =>
        Command.Checked = _manager.Backdrop == Backdrop;
}

[Command(PackageIds.NoneCommandId)]
public sealed class NoneCommand(DIToolkitPackage package, IBackdropManager manager) : BackdropCommand(package, manager)
{
    protected override BackdropType Backdrop => BackdropType.None;
}

[Command(PackageIds.MicaCommandId)]
public sealed class MicaCommand(DIToolkitPackage package, IBackdropManager manager) : BackdropCommand(package, manager)
{
    protected override BackdropType Backdrop => BackdropType.Mica;
}

[Command(PackageIds.TabbedCommandId)]
public sealed class TabbedCommand(DIToolkitPackage package, IBackdropManager manager) : BackdropCommand(package, manager)
{
    protected override BackdropType Backdrop => BackdropType.Tabbed;
}

[Command(PackageIds.AcrylicCommandId)]
public sealed class AcrylicCommand(DIToolkitPackage package, IBackdropManager manager) : BackdropCommand(package, manager)
{
    protected override BackdropType Backdrop => BackdropType.Acrylic;
}

[Command(PackageIds.GlassCommandId)]
public sealed class GlassCommand(DIToolkitPackage package, IBackdropManager manager) : BackdropCommand(package, manager)
{
    protected override BackdropType Backdrop => BackdropType.Glass;
}
