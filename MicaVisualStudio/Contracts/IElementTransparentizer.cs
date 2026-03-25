using System.Windows;
using System.Windows.Interop;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell.Interop;
using MicaVisualStudio.Services.Styling;

namespace MicaVisualStudio.Contracts;

public interface IElementTransparentizer
{
    void StyleAllWindows();
    void StyleWindow(Window window);

    void StyleAllWindowFrames();
    void StyleWindowFrame(IVsWindowFrame frame);

    void StyleHwnd(nint handle);
    void StyleElementTree(FrameworkElement element, TreeType type);

    void StyleControl(Control control);
    void StylePanel(Panel panel);
    void StyleBorder(Border border);

    void StyleHwndHost(HwndHost host);

    void Layer(FrameworkElement element);

    void TransparentizeStyle(Style style);
    Style SubclassStyle(Style style);
}
