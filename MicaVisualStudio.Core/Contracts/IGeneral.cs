using System.ComponentModel;
using MicaVisualStudio.Enums;

namespace MicaVisualStudio.Contracts;

public interface IGeneral : INotifyPropertyChanged
{
    /// <summary>
    /// Gets or sets the <see cref="Options.Theme"/> used for all windows.
    /// </summary>
    /// <remarks>Used in <see cref="WindowHelper.EnableDarkMode(System.IntPtr, bool)"/>.</remarks>
    Theme Theme { get; set; }
    /// <summary>
    /// Gets or sets the <see cref="BackdropType"/> used for all windows.
    /// </summary>
    /// <remarks>Used in <see cref="WindowHelper.SetBackdropType(System.IntPtr, BackdropType)"/>.</remarks>
    BackdropType Backdrop { get; set; }
    /// <summary>
    /// Gets or sets the <see cref="Interop.CornerPreference"/> used for all windows.
    /// </summary>
    /// <remarks>Used in <see cref="WindowHelper.SetCornerPreference(System.IntPtr, Interop.CornerPreference)"/>.</remarks>
    CornerPreference CornerPreference { get; set; }

    /// <summary>
    /// Gets or sets whether to use separate options for <see cref="WindowType.Tool"/> windows.
    /// </summary>
    bool ToolWindows { get; set; }
    /// <summary>
    /// Gets or sets the <see cref="Options.Theme"/> used for <see cref="WindowType.Tool"/> windows.
    /// </summary>
    /// <remarks>Used in <see cref="WindowHelper.EnableDarkMode(System.IntPtr, bool)"/>.</remarks>
    Theme ToolTheme { get; set; }
    /// <summary>
    /// Gets or sets the <see cref="BackdropType"/> used for <see cref="WindowType.Tool"/> windows.
    /// </summary>
    /// <remarks>Used in <see cref="WindowHelper.SetBackdropType(System.IntPtr, BackdropType)"/>.</remarks>
    BackdropType ToolBackdrop { get; set; }
    /// <summary>
    /// Gets or sets the <see cref="Interop.CornerPreference"/> used for <see cref="WindowType.Tool"/> windows.
    /// </summary>
    /// <remarks>Used in <see cref="WindowHelper.SetCornerPreference(System.IntPtr, Interop.CornerPreference)"/>.</remarks>
    CornerPreference ToolCornerPreference { get; set; }

    /// <summary>
    /// Gets or sets whether to use separate options for <see cref="WindowType.Dialog"/> windows.
    /// </summary>
    bool DialogWindows { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Options.Theme"/> used for <see cref="WindowType.Dialog"/> windows.
    /// </summary>
    /// <remarks>Used in <see cref="WindowHelper.EnableDarkMode(System.IntPtr, bool)"/>.</remarks>
    Theme DialogTheme { get; set; }
    /// <summary>
    /// Gets or sets the <see cref="BackdropType"/> used for <see cref="WindowType.Dialog"/> windows.
    /// </summary>
    /// <remarks>Used in <see cref="WindowHelper.SetBackdropType(System.IntPtr, BackdropType)"/>.</remarks>
    BackdropType DialogBackdrop { get; set; }
    /// <summary>
    /// Gets or sets the <see cref="Interop.CornerPreference"/> used for <see cref="WindowType.Dialog"/> windows.
    /// </summary>
    /// <remarks>Used in <see cref="WindowHelper.SetCornerPreference(System.IntPtr, Interop.CornerPreference)"/>.</remarks>
    CornerPreference DialogCornerPreference { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Options.Theme"/> used by the app.
    /// </summary>
    /// <remarks>Used in <see cref="ThemeHelper.SetAppTheme(Options.Theme)"/>.</remarks>
    Theme AppTheme { get; set; }

    /// <summary>
    /// Gets or sets whether to initialize <see cref="Services.ElementTransparentizer"/>.
    /// </summary>
    bool ForceTransparency { get; set; }
    /// <summary>
    /// Gets or sets whether child windows recieve the <see cref="WindowStylesEx.Layered"/> style via
    /// <see cref="WindowHelper.MakeLayered(System.IntPtr)"/>.
    /// </summary>
    /// <remarks>Used by <see cref="Services.ElementTransparentizer"/>.</remarks>
    bool LayeredWindows { get; set; }
    /// <summary>
    /// Gets or sets whether popups are acrylicized.
    /// </summary>
    /// <remarks>Used by <see cref="Services.MenuAcrylicizer"/>.</remarks>
    bool AcrylicMenus { get; set; }

    public void Save();
    public void Load();
}
