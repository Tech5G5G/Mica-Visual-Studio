using System;
using System.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace MicaVisualStudio.Helpers
{
    public class VsEventsHelper : IVsShellPropertyEvents
    {
        public int OnShellPropertyChange(int propid, object var)
        {
            switch (propid)
            {
                case (int)__VSSPROPID2.VSSPROPID_MainWindowVisibility:
                    MainWindowVisChanged?.Invoke(this, new MainWindowVisChangedEventArgs(
                        Process.GetCurrentProcess().MainWindowHandle,
                        (bool)var));
                    break;
            }

            return VSConstants.S_OK;
        }

        public event EventHandler<MainWindowVisChangedEventArgs> MainWindowVisChanged;
    }

public class MainWindowVisChangedEventArgs(IntPtr handle, bool visible) : EventArgs
    {
    public IntPtr MainWindowHandle { get; } = handle;

    public bool MainWindowVisible { get; } = visible;
}
