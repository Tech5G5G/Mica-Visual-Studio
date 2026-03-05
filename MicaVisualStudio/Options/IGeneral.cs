using System.ComponentModel;

namespace MicaVisualStudio.Options;

public interface IGeneral : INotifyPropertyChanged
{
    Theme Theme { get; set; }
    BackdropType Backdrop { get; set; }
    CornerPreference CornerPreference { get; set; }

    bool ToolWindows { get; set; }
    Theme ToolTheme { get; set; }
    BackdropType ToolBackdrop { get; set; }
    CornerPreference ToolCornerPreference { get; set; }

    bool DialogWindows { get; set; }
    Theme DialogTheme { get; set; }
    BackdropType DialogBackdrop { get; set; }
    CornerPreference DialogCornerPreference { get; set; }

    Theme AppTheme { get; set; }

    bool ForceTransparency { get; set; }
    bool LayeredWindows { get; set; }
    bool AcrylicMenus { get; set; }
}
