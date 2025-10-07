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
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class MicaVisualStudioPackage : AsyncPackage
    {
        /// <summary>
        /// MicaVisualStudioPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "1a10bdf6-6cb0-415e-8ddd-f16d897f1e4a";

        #region Package Members

        private int pid;
        private IntPtr vsHandle;

        private ShellHelper shellHelper;
        private VsEventsHelper listener;

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
                var proc = Process.GetCurrentProcess();
                pid = proc.Id;

                ApplyWindowAttributes(proc.MainWindowHandle, false);
                General.Saved += (s) =>
                {
                    if (vsHandle != IntPtr.Zero)
                        ApplyWindowAttributes(vsHandle, false);
                };

                shellHelper = new();
                shellHelper.WindowCreated += WindowCreated;
                shellHelper.WindowDestroyed += WindowDestroyed;

                if (await VsEventsHelper.CreateAsync(this, cancellationToken) is VsEventsHelper helper)
                {
                    listener = helper;
                    listener.MainWindowVisChanged += SetVsHandle;
            }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing Mica Visual Studio: {ex.Message}");
            }
        }

        private void SetVsHandle(object sender, MainWindowVisChangedEventArgs args)
        {
            if (args.MainWindowHandle != vsHandle && args.MainWindowVisible)
                vsHandle = args.MainWindowHandle;
            }

        private void WindowCreated(object sender, WindowChangedEventArgs args)
        {
            if (args.WindowHandle != IntPtr.Zero && //Check for null reference
                WindowHelper.GetWindowProcessId(args.WindowHandle) == pid && //Check if window belongs to current process
                !controllers.ContainsKey(args.WindowHandle) && //Don't composite the same window twice
                WindowHelper.GetWindowStyles(args.WindowHandle).HasFlag(WindowStyle.Caption)) //Check window for title bar
                ApplyWindowAttributes(args.WindowHandle, args.WindowHandle != vsHandle);
        }

        private void WindowDestroyed(object sender, WindowChangedEventArgs args)
        {
            if (args.WindowHandle != IntPtr.Zero && //Check for null reference
                WindowHelper.GetWindowProcessId(args.WindowHandle) == pid && //Check if window belongs to current process
                controllers.TryGetValue(args.WindowHandle, out MicaController controller)) //Get controller (if any) for window handle
            {
                controller.Dispose();
                controllers.Remove(args.WindowHandle);
            }
        }

        Compositor compositor;
        DispatcherQueueController dispatcher;

        readonly System.Collections.Generic.Dictionary<nint, MicaController> controllers = [];
        private void ApplyWindowAttributes(IntPtr hWnd, bool toolWindow)
        {
            //Make sure DispatcherQueue and Compositor are created for current thread
            if (dispatcher is null && DispatcherQueueController.TryCreate(out dispatcher))
                compositor = new();

            //Null check compositor
            if (compositor is null)
                return;
            //Create target and controller and appy to window
            var controller = new MicaController(compositor);
            controller.SetTarget(hWnd);

            //Cache controller
            controllers.Add(hWnd, controller);

            //var general = General.Instance;

            //WindowHelper.ExtendFrameIntoClientArea(hWnd);
            //WindowHelper.EnableDarkMode(hWnd);

            //if (toolWindow && general.ToolWindows)
            //{
            //    WindowHelper.SetSystemBackdropType(hWnd, (BackdropType)general.ToolBackdrop);
            //    WindowHelper.SetCornerPreference(hWnd, (CornerPreference)general.ToolCornerPreference);
            //}
            //else
            //{
            //    WindowHelper.SetSystemBackdropType(hWnd, (BackdropType)general.Backdrop);
            //    WindowHelper.SetCornerPreference(hWnd, (CornerPreference)general.CornerPreference);
            //}
        }

        #endregion
    }
}
