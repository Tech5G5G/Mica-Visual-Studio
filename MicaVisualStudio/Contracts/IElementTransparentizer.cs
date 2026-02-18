using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell.Interop;

namespace MicaVisualStudio.Contracts;

public interface IElementTransparentizer
{
    void StyleAllWindows();
    void StyleWindow(Window window);

    void StyleAllWindowFrames();
    void StyleWindowFrame(IVsWindowFrame frame);

    void StyleElementTree(FrameworkElement element);
    void StyleTree(IEnumerable<FrameworkElement> tree);

    void StyleDockTarget(Border dock);
    void StyleToolBar(ToolBar bar);
    void StyleTabItem(TabItem tab);
    void StyleHwndHost(HwndHost host);
    void StyleControl(Control control);
    void StylePanel(Panel panel);
    void StyleBorder(Border border);

    void Layer(Control control);
    void Layer(Panel panel);
    void Layer(Border border);

    void TransparentizeStyle(Style style);
    Style SubclassStyle(Style style);
}
