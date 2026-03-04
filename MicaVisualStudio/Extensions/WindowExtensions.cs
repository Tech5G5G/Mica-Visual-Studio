using System.Windows.Interop;
using MicaVisualStudio.Options;

namespace System.Windows;

public static class WindowExtensions
{
    extension(Window window)
    {
        public nint Handle
        {
            get
            {
                WindowInteropHelper interop = new(window);
                interop.EnsureHandle();
                return interop.Handle;
            }
        }

        public WindowType WindowType
        {
            get
            {
                if (window == Application.Current.MainWindow)
                {
                    return WindowType.Main;
                }
                else if (window is not null && // Check if window is WPF
                    (window.WindowStyle == WindowStyle.None || // and has no style
                    window.Owner is null)) // or no owner
                {
                    return WindowType.Tool;
                }
                else
                {
                    return WindowType.Dialog;
                }
            }
        }
    }
}
