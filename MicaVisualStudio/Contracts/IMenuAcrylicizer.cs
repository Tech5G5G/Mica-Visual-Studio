using System.Windows;
using System.Windows.Interop;
using System.Windows.Controls.Primitives;

namespace MicaVisualStudio.Contracts;

public interface IMenuAcrylicizer
{
    void AcrylicizePopup(Popup popup, HwndSource source, FrameworkElement root = null);
    void RemovePopupOffset(Popup popup);
}
