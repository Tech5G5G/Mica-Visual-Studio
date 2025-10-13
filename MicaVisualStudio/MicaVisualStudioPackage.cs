using System.Linq;

namespace MicaVisualStudio
{
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
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideOptionPage(typeof(OptionsProvider.GeneralOptions), "Mica Visual Studio", "General", 0, 0, true, SupportsProfiles = true)]
    [ProvideProfile(typeof(OptionsProvider.GeneralOptions), "Mica Visual Studio", "General", 0, 0, true)]
    [ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class MicaVisualStudioPackage : AsyncPackage
    {
        /// <summary>
        /// MicaVisualStudioPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "1a10bdf6-6cb0-415e-8ddd-f16d897f1e4a";

        #region Package Members

        private readonly int pid = Process.GetCurrentProcess().Id;
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
                shell = await this.GetVsShellAsync();

                if (Environment.OSVersion.Version.Build < 22000) //Allow Windows 11 or later
                {
                    queuedInfo = ("Mica Visual Studio is not compatible with Windows 10 and earlier.", KnownMonikers.StatusWarning);
                    return;
                }

                shellHelper = new();
                shellHelper.WindowCreated += WindowCreated;
                shellHelper.WindowDestroyed += WindowDestroyed;

                vsHandle = Application.Current.MainWindow.GetHandle();

                    listener = helper;
                    listener.MainWindowVisChanged += SetVsHandle;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing Mica Visual Studio: {ex.Message}");

                progress.Report(new("Mica Visual Studio", $"Error while initializing Mica Visual Studio:\n{ex.Message}"));
                queuedInfo = ($"Error while initializing Mica Visual Studio: {ex.Message} ({ex.GetType().Name})\n{ex.StackTrace}", KnownMonikers.StatusError);
            }
        }
        }

            void Window_Loaded(object sender, RoutedEventArgs args)
        {
                if (queuedInfo.Content is not null)
                    _ = this.ShowInfoBarAsync(queuedInfo.Content, queuedInfo.Image, shell);

                WindowManager.MainWindow.Loaded -= Window_Loaded;
            }
        }

        private void ApplyWindowAttributes(IntPtr hWnd, WindowType type, bool firstTime = true)
        {
            var general = General.Instance;

            if (firstTime && //Remove caption buttons once
                HwndSource.FromHwnd(hWnd) is HwndSource source &&
                source.RootVisual is Window window)
        {
                WindowHelper.ExtendFrameIntoClientArea(hWnd);
                window.Background = new System.Windows.Media.SolidColorBrush(source.CompositionTarget.BackgroundColor = System.Windows.Media.Colors.Transparent);

                //Don't remove caption buttons from windows that need them
                if (window.WindowStyle == WindowStyle.None || window is not Microsoft.VisualStudio.PlatformUI.DialogWindowBase)
                    WindowHelper.RemoveCaptionButtons(source);
            }

                WindowHelper.EnableDarkMode(hWnd); //Just looks better

            switch (type)
                {
                default:
                case WindowType.Main:
                    WindowHelper.SetBackdropType(hWnd, (BackdropType)general.Backdrop);
                    WindowHelper.SetCornerPreference(hWnd, (CornerPreference)general.CornerPreference);
                    break;
                case WindowType.Tool when general.ToolWindows:
                    WindowHelper.SetBackdropType(hWnd, (BackdropType)general.ToolBackdrop);
                    WindowHelper.SetCornerPreference(hWnd, (CornerPreference)general.ToolCornerPreference);
                    break;
                case WindowType.Dialog when general.DialogWindows:
                    WindowHelper.SetBackdropType(hWnd, (BackdropType)general.DialogBackdrop);
                    WindowHelper.SetCornerPreference(hWnd, (CornerPreference)general.DialogCornerPreference);
                    break;
            }
        }

        #endregion
    }
}
