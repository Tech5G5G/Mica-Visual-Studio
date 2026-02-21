using System.ComponentModel;

namespace MicaVisualStudio.Options;

public interface IGeneral : INotifyPropertyChanged
{
    /// <summary>
    /// Gets or sets the <see cref="Options.Theme"/> used for all windows.
    /// </summary>
    Theme Theme { get; set; }
    /// <summary>
    /// Gets or sets the <see cref="BackdropType"/> used for all windows.
    /// </summary>
    BackdropType Backdrop { get; set; }
    /// <summary>
    /// Gets or sets the <see cref="Options.CornerPreference"/> used for all windows.
    /// </summary>
    CornerPreference CornerPreference { get; set; }

    /// <summary>
    /// Gets or sets whether to use separate options for <see cref="WindowType.Tool"/> windows.
    /// </summary>
    bool ToolWindows { get; set; }
    /// <summary>
    /// Gets or sets the <see cref="Options.Theme"/> used for <see cref="WindowType.Tool"/> windows.
    /// </summary>
    Theme ToolTheme { get; set; }
    /// <summary>
    /// Gets or sets the <see cref="BackdropType"/> used for <see cref="WindowType.Tool"/> windows.
    /// </summary>
    BackdropType ToolBackdrop { get; set; }
    /// <summary>
    /// Gets or sets the <see cref="Options.CornerPreference"/> used for <see cref="WindowType.Tool"/> windows.
    /// </summary>
    CornerPreference ToolCornerPreference { get; set; }

    /// <summary>
    /// Gets or sets whether to use separate options for <see cref="WindowType.Dialog"/> windows.
    /// </summary>
    bool DialogWindows { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Options.Theme"/> used for <see cref="WindowType.Dialog"/> windows.
    /// </summary>
    Theme DialogTheme { get; set; }
    /// <summary>
    /// Gets or sets the <see cref="BackdropType"/> used for <see cref="WindowType.Dialog"/> windows.
    /// </summary>
    BackdropType DialogBackdrop { get; set; }
    /// <summary>
    /// Gets or sets the <see cref="Options.CornerPreference"/> used for <see cref="WindowType.Dialog"/> windows.
    /// </summary>
    CornerPreference DialogCornerPreference { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Options.Theme"/> used by the app.
    /// </summary>
    Theme AppTheme { get; set; }

    /// <summary>
    /// Gets or sets whether to transparentize elements.
    /// </summary>
    bool ForceTransparency { get; set; }
    /// <summary>
    /// Gets or sets whether child windows recieve the <c>WS_EX_LAYERED</c> style.
    /// </summary>
    bool LayeredWindows { get; set; }
    /// <summary>
    /// Gets or sets whether popups are acrylicized.
    /// </summary>
    bool AcrylicMenus { get; set; }

    public void Save();
    public void Load();
}
