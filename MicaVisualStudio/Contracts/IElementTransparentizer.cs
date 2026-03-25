using System.Windows;
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

    void Layer(FrameworkElement element);

    void TransparentizeStyle(Style style);
    Style SubclassStyle(Style style);
}
