using System;
using System.Collections.Generic;
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
using MicaVisualStudio.Services;
using MicaVisualStudio.Contracts;
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
[ProvideOptionPage(typeof(OptionsProvider.ToolOptions), Vsix.Name, /* Show before Dialog Windows */ "\u200BTool Windows", 0, 0, true, SupportsProfiles = true)]
[ProvideProfile(typeof(OptionsProvider.ToolOptions), Vsix.Name, "Tool Windows", 0, 0, true)]
[ProvideOptionPage(typeof(OptionsProvider.DialogOptions), Vsix.Name, "Dialog Windows", 0, 0, true, SupportsProfiles = true)]
[ProvideProfile(typeof(OptionsProvider.DialogOptions), Vsix.Name, "Dialog Windows", 0, 0, true)]

// Auto load
[ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideAutoLoad(UIContextGuids.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideAutoLoad(UIContextGuids.EmptySolution, PackageAutoLoadFlags.BackgroundLoad)]
public sealed class MicaVisualStudioPackage : MicrosoftDIToolkitPackage<MicaVisualStudioPackage>
{
    private ThemeHelper theme;
    private WindowObserver observer;

    private VsColorManager colors;
    private VsWindowStyler styler;

    private ServiceProvider _provider;

    private (string Content, ImageMoniker Image) queuedInfo;

    protected override void InitializeServices(IServiceCollection services)
    {
        services.AddSingleton<IGeneral>(General.Instance);

        services.AddSingleton<IVsWindowManager, VsWindowManager>();

        services.AddSingleton<NoneCommand>()
                .AddSingleton<MicaCommand>()
                .AddSingleton<TabbedCommand>()
                .AddSingleton<AcrylicCommand>()
                .AddSingleton<GlassCommand>()
                .AddSingleton<MoreCommand>();
    }

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await base.InitializeAsync(cancellationToken, progress);
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        
        try
        {
            WindowObserver.MainWindow.Loaded += Window_Loaded;

            if (Environment.OSVersion.Version.Build < 22000) // Allow Windows 11 or later
            {
                queuedInfo = ("Mica Visual Studio is not compatible with Windows 10 and earlier.", KnownMonikers.StatusWarning);
                return;
            }

            colors = VsColorManager.Instance;

            #region Resource Keys

            colors.AddConfigs(new()
            {
                { "Background", new(translucent: true) },

                { "SolidBackgroundFillQuaternary", new(translucent: true) },

                // { "SolidBackgroundFillTertiary", ColorConfig.Default },
                // { "EnvironmentLayeredBackground", new(transparentOnGray: true, translucent: true, opacity: 0x7F) },

                { "EnvironmentBackground", new(translucent: true) },
                { "EnvironmentBackgroundGradient", ColorConfig.Default },

                { "ActiveCaption", ColorConfig.Layered },
                { "InactiveCaption", ColorConfig.Layered },

                { "MainWindowActiveCaption", ColorConfig.Default },
                { "MainWindowInactiveCaption", ColorConfig.Default },

                { "ToolWindow", ColorConfig.Default },
                { "ToolWindowGroup", ColorConfig.Default },
                { "ToolWindowBackground", ColorConfig.Default },
                { "ToolWindowFloatingFrame", ColorConfig.Default },
                { "ToolWindowFloatingFrameInactive", ColorConfig.Default },
                { "ToolWindowTabMouseOverBackgroundGradient", ColorConfig.Layered },

                { "ToolWindowContentGrid", ColorConfig.Layered },

                { "PopupBackground", ColorConfig.Default },

                { "Default", ColorConfig.Default },

                { "Window", ColorConfig.Default },
                { "WindowPanel", new(translucent: true) },

                { "CommandBarGradient", ColorConfig.Default },
                { "CommandBarGradientBegin", ColorConfig.Default },

                { "ListBox", ColorConfig.Layered },
                { "ListItemBackgroundHover", new(transparentOnGray: false, translucent: true) },

                { "SelectedItemActive", ColorConfig.Layered },
                { "SelectedItemInactive", ColorConfig.Layered },

                { "Unfocused", ColorConfig.Layered },

                { "Caption", ColorConfig.Layered },

                { "TextBoxBackground", ColorConfig.Layered },
                { "SearchBoxBackground", ColorConfig.Layered },

                { "Button", ColorConfig.Layered },
                { "ButtonFocused", ColorConfig.Default },

                { "ComboBoxBackground", ColorConfig.Layered },

                { "InfoBarBorder", ColorConfig.Default },

                { "Page", ColorConfig.Default },
                { "PageBackground", ColorConfig.Default },

                { "BrandedUIBackground", ColorConfig.Default },

                { "ScrollBarBackground", ColorConfig.Layered },
                { "ScrollBarArrowBackground", ColorConfig.Default },
                { "ScrollBarArrowDisabledBackground", ColorConfig.Default },

                { "AutoHideResizeGrip", ColorConfig.Default },
                { "AutoHideResizeGripDisabled", ColorConfig.Default },

                { "Content", ColorConfig.Default },
                { "ContentSelected", ColorConfig.Layered },
                { "ContentMouseOver", ColorConfig.Layered },
                { "ContentInactiveSelected", ColorConfig.Layered },

                { "Wonderbar", ColorConfig.Default },
                { "WonderbarMouseOver", ColorConfig.Layered },
                { "WonderbarTreeInactiveSelected", ColorConfig.Default },

                { "Details", ColorConfig.Layered },

                { "BackgroundLowerRegion", ColorConfig.Default },
                { "WizardBackgroundLowerRegion", ColorConfig.Default }
            });
            colors.UpdateColors();

            #endregion

            if (General.Instance.ForceTransparency)
                (styler = VsWindowStyler.Instance).Listen();

            theme = ThemeHelper.Instance;
            observer = WindowObserver.Instance;

            RefreshPreferences(); // Set app theme

            if (WindowObserver.MainWindow.Visibility == Visibility.Visible) // We're late, so add all windows
            {
                WindowObserver.AllWindows.ForEach(AddWindow);
                WindowObserver.MainWindow.Loaded -= Window_Loaded;
            }
            else if (WindowObserver.CurrentWindow is Window window) // Apply to start window
                AddWindow(window);

            observer.WindowOpened += WindowOpened;
            // windows.WindowClosed += WindowClosed;

            colors.VisualStudioThemeChanged += ThemeChanged;
            theme.SystemThemeChanged += ThemeChanged;

            General.Saved += GeneralSaved;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing Mica Visual Studio: {ex.Message}");

            progress.Report(new("Mica Visual Studio", $"Error while initializing Mica Visual Studio:\n{ex.Message}"));
            queuedInfo = ($"Error while initializing Mica Visual Studio: {ex.Message} ({ex.GetType().Name})\n{ex.StackTrace}", KnownMonikers.StatusError);
        }

        void Window_Loaded(object sender, RoutedEventArgs args)
        {
            if (queuedInfo.Content is not null)
                VS.InfoBar.CreateAsync(new(queuedInfo.Content, queuedInfo.Image)).Result.TryShowInfoBarUIAsync().Forget();

            WindowObserver.MainWindow.Loaded -= Window_Loaded;
        }

        void AddWindow(Window window)
        {
            observer.AppendWindow(window);
            ApplyWindowPreferences(
                window.GetHandle(),
                window,
                WindowHelper.GetWindowType(window));
        }
    }

    #region Event Handlers

    private void WindowOpened(Window sender, WindowActionEventArgs args) => ApplyWindowPreferences(args.WindowHandle, sender, args.WindowType);

    // private void WindowClosed(Window sender, WindowActionEventArgs args) { }

    private void ThemeChanged(object sender, Theme args) => RefreshPreferences();

    private void GeneralSaved(General sender) => RefreshPreferences();

    private void RefreshPreferences()
    {
        General general = General.Instance;
        theme.SetAppTheme(EvaluateTheme(general.AppTheme));

        foreach (var entry in observer.Windows)
            ApplyWindowPreferences(entry.Key, entry.Value.Window, entry.Value.Type, firstTime: false, general);
    }

    #endregion

    private void ApplyWindowPreferences(
        IntPtr handle,
        Window window,
        WindowType type,
        bool firstTime = true,
        General general = null)
    {
        general ??= General.Instance;

        if (window?.AllowsTransparency == true) // Don't apply to transparent windows
            return;

        if (firstTime && // Remove caption buttons once
            window is not null && // Check that window is WPF
            HwndSource.FromHwnd(handle) is HwndSource source)
        {
            WindowHelper.ExtendFrameIntoClientArea(handle);
            source.CompositionTarget.BackgroundColor = Colors.Transparent;

            // Don't remove caption buttons from windows that need them
            if (window.WindowStyle == WindowStyle.None || window is not DialogWindowBase)
                WindowHelper.RemoveCaptionButtons(source);
        }

        switch (type)
        {
            default:
            case WindowType.Main:
                ApplyWindowAttributes(
                    general.Theme,
                    general.CornerPreference,
                    general.Backdrop);
                break;

            case WindowType.Tool when general.ToolWindows:
                ApplyWindowAttributes(
                    general.ToolTheme,
                    general.ToolCornerPreference,
                    general.ToolBackdrop);
                break;

            case WindowType.Dialog when general.DialogWindows:
                ApplyWindowAttributes(
                    general.DialogTheme,
                    general.DialogCornerPreference,
                    general.DialogBackdrop);
                break;
        }

        void ApplyWindowAttributes(Theme theme, CornerPreference corner, BackdropType backdrop)
        {
            WindowHelper.EnableDarkMode(handle, EvaluateTheme(theme) == Theme.Dark);
            WindowHelper.SetCornerPreference(handle, corner);
            WindowHelper.SetBackdropType(handle, window is not null || backdrop != BackdropType.Glass ? backdrop : BackdropType.None);
        }
    }

    private Theme EvaluateTheme(Theme theme) => theme switch
    {
        Theme.VisualStudio => colors.VisualStudioTheme,
        Theme.System => this.theme.SystemTheme,
        _ => (Theme)theme
    };

    protected override IServiceProvider BuildServiceProvider(IServiceCollection serviceCollection) =>
        _provider = base.BuildServiceProvider(serviceCollection) as ServiceProvider;

    protected override void Dispose(bool disposing)
    {
        theme?.Dispose();
        observer?.Dispose();

        styler?.Dispose();

        _provider?.Dispose();

        if (disposing)
        {
            observer?.WindowOpened -= WindowOpened;
            // observer?.WindowClosed -= WindowClosed;

            colors?.VisualStudioThemeChanged -= ThemeChanged;
            theme?.SystemThemeChanged -= ThemeChanged;

            General.Saved -= GeneralSaved;

            _provider = null;
        }

        base.Dispose(disposing);
    }
}
