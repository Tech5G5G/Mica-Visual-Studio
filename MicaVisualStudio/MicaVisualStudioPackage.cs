namespace MicaVisualStudio;

/// <summary>
/// This is the class that implements the package exposed by this assembly.
/// </summary>
/// <remarks>
/// <para>
/// The minimum requirement for a class to be considered a valid package for Visual Studio
/// is to implement the IVsPackage interface and register itself with the shell.
/// This package uses the helper classes defined inside the Managed Package Framework (MPF)
/// to do it: it derives from the Package class that provides the implementation of the
/// IVsPackage interface and uses the registration attributes defined in the framework to
/// register itself and its components with the shell. These attributes tell the pkgdef creation
/// utility what data to put into .pkgdef file.
/// </para>
/// <para>
/// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
/// </para>
/// </remarks>
[PackageRegistration(AllowsBackgroundLoading = true, UseManagedResourcesOnly = true)]
[Guid(PackageGuidString)]
[ProvideOptionPage(typeof(OptionsProvider.GeneralOptions), "Mica Visual Studio", "General", 0, 0, true, SupportsProfiles = true)]
[ProvideProfile(typeof(OptionsProvider.GeneralOptions), "Mica Visual Studio", "General", 0, 0, true)]
[ProvideOptionPage(typeof(OptionsProvider.ToolOptions), "Mica Visual Studio", "\u200BTool Windows", 0, 0, true, SupportsProfiles = true)]
[ProvideProfile(typeof(OptionsProvider.ToolOptions), "Mica Visual Studio", "Tool Windows", 0, 0, true)]
[ProvideOptionPage(typeof(OptionsProvider.DialogOptions), "Mica Visual Studio", "Dialog Windows", 0, 0, true, SupportsProfiles = true)]
[ProvideProfile(typeof(OptionsProvider.DialogOptions), "Mica Visual Studio", "Dialog Windows", 0, 0, true)]
[ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideAutoLoad(UIContextGuids.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideAutoLoad(UIContextGuids.EmptySolution, PackageAutoLoadFlags.BackgroundLoad)]
public sealed class MicaVisualStudioPackage : AsyncPackage
{
    /// <summary>
    /// MicaVisualStudioPackage GUID string.
    /// </summary>
    public const string PackageGuidString = "1a10bdf6-6cb0-415e-8ddd-f16d897f1e4a";

    #region Package Members

    private ThemeHelper theme;
    private WindowManager manager;

    private ILHook hook;
    private VsColorManager colors;
    private VsWindowStyler styler;

    private (string Content, ImageMoniker Image) queuedInfo;

    /// <summary>
    /// Initialization of the package; this method is called right after the package is sited, so this is the place
    /// where you can put all the initialization code that rely on services provided by VisualStudio.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
    /// <param name="progress">A provider for progress updates.</param>
    /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        try
        {
            WindowManager.MainWindow.Loaded += Window_Loaded;

            if (Environment.OSVersion.Version.Build < 22000) //Allow Windows 11 or later
            {
                queuedInfo = ("Mica Visual Studio is not compatible with Windows 10 and earlier.", KnownMonikers.StatusWarning);
                return;
            }

            hook = new(typeof(HwndSource).GetProperty("RootVisual").SetMethod, context =>
            {
                ILCursor cursor = new(context) { Index = 0 };

                cursor.Emit(OpCodes.Ldarg_0); //this (HwndSource)
                cursor.Emit(OpCodes.Ldarg_1); //value

                cursor.EmitDelegate(RootVisualChanged);
            });
            colors = VsColorManager.Instance;

            #region Resource Keys

            colors.AddConfigs(new()
                {
                    { "Background", new(translucent: true) },

                    { "SolidBackgroundFillQuaternary", new(translucent: true) },

                    //{ "SolidBackgroundFillTertiary", ColorConfig.Default },
                    //{ "EnvironmentLayeredBackground", new(transparentOnGray: true, translucent: true, opacity: 0x7F) },

                    { "EnvironmentBackground", new(translucent: true) },
                    { "EnvironmentBackgroundGradient", ColorConfig.Default },

                    { "ActiveCaption", new(transparentOnGray: false, translucent: true, opacity: 0x7F) },
                    { "InactiveCaption", new(transparentOnGray: false, translucent: true, opacity: 0x7F) },

                    { "MainWindowActiveCaption", ColorConfig.Default },
                    { "MainWindowInactiveCaption", ColorConfig.Default },

                    { "ToolWindow", ColorConfig.Default },
                    { "ToolWindowGroup", ColorConfig.Default },
                    { "ToolWindowBackground", ColorConfig.Default },
                    { "ToolWindowFloatingFrame", ColorConfig.Default },
                    { "ToolWindowFloatingFrameInactive", ColorConfig.Default },

                { "Default", ColorConfig.Default },

                    { "WindowPanel", new(translucent: true) },

                    { "CommandBarGradient", ColorConfig.Default },

                    { "ListBox", new(transparentOnGray: false, translucent: true, opacity: 0x7F) },
                    { "ListItemBackgroundHover", new(transparentOnGray: false, translucent: true) },

                    { "TextBoxBackground", new(transparentOnGray: false, translucent: true, opacity: 0x7F) },

                    { "ScrollBarBackground", new(transparentOnGray: false, translucent: true, opacity: 0x7F) },
                    { "ScrollBarArrowBackground", ColorConfig.Default },
                    { "ScrollBarArrowDisabledBackground", ColorConfig.Default },

                    { "SelectedItemActive", new(transparentOnGray: false, translucent: true, opacity: 0x7F) },
                    { "SelectedItemInactive", new(transparentOnGray: false, translucent: true, opacity: 0x7F) }
            });
            colors.UpdateColors();

            #endregion

            styler = VsWindowStyler.Instance;

            theme = ThemeHelper.Instance;
            manager = WindowManager.Instance;

            RefreshPreferences(); //Set app theme

            if (WindowManager.MainWindow.Visibility == Visibility.Visible) //We're late, so add all windows
            {
                WindowManager.AllWindows.ForEach(i => AddWindow(i, WindowHelper.GetWindowType(i)));
                WindowManager.MainWindow.Loaded -= Window_Loaded;
            }
            else if (WindowManager.CurrentWindow is Window window) //Apply to start window
                AddWindow(window, WindowType.Tool);

            manager.WindowOpened += (s, e) => ApplyWindowPreferences(e.WindowHandle, s, e.WindowType);
            //windows.WindowClosed += (s, e) => { };

            colors.VisualStudioThemeChanged += (s, e) => RefreshPreferences();
            theme.SystemThemeChanged += (s, e) => RefreshPreferences();

            General.Saved += (s) => RefreshPreferences();
        }
        catch (Exception ex)
        {
#if DEBUG
            Debug.WriteLine($"Error initializing Mica Visual Studio: {ex.Message}");
#endif

            progress.Report(new("Mica Visual Studio", $"Error while initializing Mica Visual Studio:\n{ex.Message}"));
            queuedInfo = ($"Error while initializing Mica Visual Studio: {ex.Message} ({ex.GetType().Name})\n{ex.StackTrace}", KnownMonikers.StatusError);
        }

        void Window_Loaded(object sender, RoutedEventArgs args)
        {
            if (queuedInfo.Content is not null)
                VS.InfoBar.CreateAsync(new(queuedInfo.Content, queuedInfo.Image)).Result.TryShowInfoBarUIAsync().Forget();

            WindowManager.MainWindow.Loaded -= Window_Loaded;
        }

        void RefreshPreferences()
        {
            General general = General.Instance;
            theme.SetAppTheme(EvaluateTheme(general.AppTheme));

            foreach (var entry in manager.Windows)
                ApplyWindowPreferences(entry.Key, entry.Value.Window, entry.Value.Type, firstTime: false, general);
        }

        void AddWindow(Window window, WindowType type)
        {
            manager.AddWindow(window, type);
            ApplyWindowPreferences(window.GetHandle(), window, type);
        }

        static void RootVisualChanged(HwndSource instance, Visual value)
        {
            if (value is not null or Popup //Avoid unnecessary
                or Window) //and already handled values
                instance.CompositionTarget.BackgroundColor = Colors.Transparent;
        }
    }

    private void ApplyWindowPreferences(
        IntPtr handle,
        Window window,
        WindowType type,
        bool firstTime = true,
        General general = null)
    {
        general ??= General.Instance;

        if (firstTime && //Remove caption buttons once
            window is not null &&
            HwndSource.FromHwnd(handle) is HwndSource source)
        {
            WindowHelper.ExtendFrameIntoClientArea(handle);
            source.CompositionTarget.BackgroundColor = Colors.Transparent;

            //Don't remove caption buttons from windows that need them
            if (window.WindowStyle == WindowStyle.None || window is not DialogWindowBase)
                WindowHelper.RemoveCaptionButtons(source);
        }

        switch (type)
        {
            default:
            case WindowType.Main:
                ApplyWindowAttributes(
                    general.Theme,
                    (CornerPreference)general.CornerPreference,
                    (BackdropType)general.Backdrop);
                break;

            case WindowType.Tool when general.ToolWindows:
                ApplyWindowAttributes(
                    general.ToolTheme,
                    (CornerPreference)general.ToolCornerPreference,
                    (BackdropType)general.ToolBackdrop);
                break;

            case WindowType.Dialog when general.DialogWindows:
                ApplyWindowAttributes(
                    general.DialogTheme,
                    (CornerPreference)general.DialogCornerPreference,
                    (BackdropType)general.DialogBackdrop);
                break;
        }

        void ApplyWindowAttributes(int theme, CornerPreference corner, BackdropType backdrop)
        {
            WindowHelper.SetDarkMode(handle, EvaluateTheme(theme) == Theme.Dark);
            WindowHelper.SetCornerPreference(handle, corner);
            WindowHelper.SetBackdropType(handle, window is not null || backdrop != BackdropType.Glass ? backdrop : BackdropType.None);
        }
    }

    private Theme EvaluateTheme(int theme) => (Theme)theme switch
    {
        Theme.VisualStudio => colors.VisualStudioTheme,
        Theme.System => this.theme.SystemTheme,
        _ => (Theme)theme
    };

    protected override void Dispose(bool disposing)
    {
        theme?.Dispose();
        manager?.Dispose();

        hook?.Dispose();
        styler?.Dispose();

        if (disposing)
        {
            theme = null;
            manager = null;

            hook = null;
            colors = null;
            styler = null;
        }

        base.Dispose(disposing);
    }

    #endregion
}
