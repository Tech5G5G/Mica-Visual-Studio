﻿using System;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using MicaVisualStudio.Helpers;

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

        IntPtr vshWnd;
        int processId;

        WinEventHelper eventHelper;
        ThemeHelper themeHelper;
        VsEventsHelper listener;

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

            var proc = Process.GetCurrentProcess();
            processId = proc.Id;

            eventHelper = new WinEventHelper(WinEventProc, WinEventHelper.EVENT_OBJECT_SHOW, WinEventHelper.EVENT_OBJECT_SHOW, WinEventHelper.WINEVENT_OUTOFCONTEXT);
            ApplyWindowAttributes(proc.MainWindowHandle, false);

            #region GetVsHandle

            listener = new VsEventsHelper();
            listener.MainWindowVisChanged += SetVsHandle;

            IVsShell vsShell = (await GetServiceAsync(typeof(SVsShell))) as IVsShell;
            vsShell.AdviseShellPropertyChanges(listener, out _);
        }

        private void SetVsHandle(object sender, MainWindowVisChangedEventArgs args)
        {
            if (args.MainWindowHandle == vshWnd || !args.MainWindowVisible)
                return;
            vshWnd = args.MainWindowHandle;

            #endregion

            General.Saved += (s) => ApplyWindowAttributes(vshWnd, false);
            themeHelper = new ThemeHelper(vshWnd);
            themeHelper.ThemeChanged += (e) =>
            {
                if ((Theme)General.Instance.Theme == Theme.System)
                    WindowHelper.SetImmersiveDarkMode(vshWnd, e);
            };
        }

        private void WinEventProc(IntPtr hWinEventHook, int eventConst, IntPtr hWnd, int idObject, int idChild, int idEventThread, int dwmsEventTime)
        {
            if (hWnd != IntPtr.Zero && //Checks for null reference
                ProcessHelper.GetWindowProcessID(hWnd) == processId && //Only applies to windows under current VS process
                WindowHelper.GetWindowStyles(hWnd).HasFlag(WindowStyle.Caption)) //Checks window for a title bar
                ApplyWindowAttributes(hWnd, hWnd != vshWnd);
        }

        private void ApplyWindowAttributes(IntPtr hWnd, bool toolWindow)
        {
            var general = General.Instance;
            WindowHelper.ExtendFrameIntoClientArea(hWnd);

            if (toolWindow && general.ToolWindows)
            {
                WindowHelper.SetImmersiveDarkMode(hWnd, (Theme)general.ToolTheme);
                WindowHelper.SetSystemBackdropType(hWnd, (BackdropType)general.ToolBackdrop);
                WindowHelper.SetCornerPreference(hWnd, (CornerPreference)general.ToolCornerPreference);
            }
            else
            {
                WindowHelper.SetImmersiveDarkMode(hWnd, general.Theme == 2 ? themeHelper.Theme : (Theme)general.Theme);
                WindowHelper.SetSystemBackdropType(hWnd, (BackdropType)general.Backdrop);
                WindowHelper.SetCornerPreference(hWnd, (CornerPreference)general.CornerPreference);
            }
        }

        #endregion
    }
}
