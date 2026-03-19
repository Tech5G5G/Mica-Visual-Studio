using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Community.VisualStudio.Toolkit;
using Community.VisualStudio.Toolkit.DependencyInjection.Microsoft;
using MonoMod.Utils;
using MicaVisualStudio.Options;
using MicaVisualStudio.Contracts;
using MicaVisualStudio.UI.ViewModels;
using MicaVisualStudio.Services;
using MicaVisualStudio.Services.Styling;
using MicaVisualStudio.Services.Windowing;
using MicaVisualStudio.Services.Resourcing;
using ServiceProvider = Microsoft.Extensions.DependencyInjection.ServiceProvider;

namespace MicaVisualStudio;

// Information
[PackageRegistration(AllowsBackgroundLoading = true, UseManagedResourcesOnly = true)]
[Guid(PackageGuids.guidMVSPackageString)]
[InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
[ProvideMenuResource("Menus.ctmenu", 1)]

// Options
[ProvideOptionPage(typeof(OptionsProvider.GeneralOptions), Vsix.Name, "General", 0, 0, true, SupportsProfiles = true)]
[ProvideProfile(typeof(OptionsProvider.GeneralOptions), Vsix.Name, "General", 0, 0, true)]
[ProvideOptionPage(typeof(OptionsProvider.ToolOptions), Vsix.Name, "\u200B" /* Show before Dialog Windows */ + "Tool Windows", 0, 0, true, SupportsProfiles = true)]
[ProvideProfile(typeof(OptionsProvider.ToolOptions), Vsix.Name, "Tool Windows", 0, 0, true)]
[ProvideOptionPage(typeof(OptionsProvider.DialogOptions), Vsix.Name, "Dialog Windows", 0, 0, true, SupportsProfiles = true)]
[ProvideProfile(typeof(OptionsProvider.DialogOptions), Vsix.Name, "Dialog Windows", 0, 0, true)]

// Auto load
[ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideAutoLoad(UIContextGuids.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideAutoLoad(UIContextGuids.EmptySolution, PackageAutoLoadFlags.BackgroundLoad)]
public sealed partial class MicaVisualStudioPackage : MicrosoftDIToolkitPackage<MicaVisualStudioPackage>
{
    private ILogger _logger;
    private IBackdropManager _backdrop;
    private IResourceManager _resource;
    private IMenuAcrylicizer _acrylicizer;
    private IElementTransparentizer _transparentizer;

    private ServiceProvider _provider;

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        await General.GetLiveInstanceAsync();
        await base.InitializeAsync(cancellationToken, progress);

        // If this fails, welp
        _logger = ServiceProvider.GetRequiredService<ILogger>();

        // Allow Windows 11 (or later)
        if (Environment.OSVersion.Version.Build < 22000)
        {
            _logger.InfoBar("Mica Visual Studio is not compatible with Windows 10 and earlier.", KnownMonikers.StatusWarning);
        }
        else if (TryGetService(out _resource))
        {
            // Initialize ResourceManager first...
            _resource.Configurations.AddRange(s_configs);
            _resource.ConfigureResources();

            // So backdrop applies when it's actually visible
            TryGetService(out _transparentizer);
            TryGetService(out _acrylicizer);
            TryGetService(out _backdrop);
        }
    }

    protected override void InitializeServices(IServiceCollection services)
    {
        services.AddSingleton(VS.GetRequiredService<SVsUIShell, IVsUIShell>())
                .AddSingleton(VS.GetRequiredService<SVsUIShell, IVsUIShell5>())
                .AddSingleton(VS.GetRequiredService<SVsUIShell, IVsUIShell7>())
                .AddSingleton(VS.GetRequiredService<SVsActivityLog, IVsActivityLog>());

        services.AddSingleton<IGeneral>(General.Instance);

        services.AddSingleton<ILogger, Logger>()
                .AddSingleton<IThemeService, ThemeService>()
                .AddSingleton<IWindowManager, WindowManager>()
                .AddSingleton<IInfoBarService, InfoBarService>()
                .AddSingleton<IBackdropManager, BackdropManager>()
                .AddSingleton<IResourceManager, ResourceManager>()
                .AddSingleton<IMenuAcrylicizer, MenuAcrylicizer>()
                .AddSingleton<IElementTransparentizer, ElementTransparentizer>();

        services.AddSingleton<NoneCommand>()
                .AddSingleton<MicaCommand>()
                .AddSingleton<TabbedCommand>()
                .AddSingleton<AcrylicCommand>()
                .AddSingleton<GlassCommand>()
                .AddSingleton<MoreCommand>();

        services.AddTransient<OptionPageViewModel>();
    }

    protected override IServiceProvider BuildServiceProvider(IServiceCollection serviceCollection)
    {
        return _provider = base.BuildServiceProvider(serviceCollection) as ServiceProvider;
    }

    private bool TryGetService<T>(out T service)
    {
        try
        {
            service = ServiceProvider.GetRequiredService<T>();
            return true;
        }
        catch (Exception ex)
        {
            _logger.InfoBar(ex, KnownMonikers.StatusError);

            service = default;
            return false;
        }
    }

    protected override void Dispose(bool disposing)
    {
        _provider?.Dispose();

        if (disposing)
        {
            _logger = null;
            _backdrop = null;
            _resource = null;
            _transparentizer = null;

            _provider = null;
        }

        base.Dispose(disposing);
    }
}
