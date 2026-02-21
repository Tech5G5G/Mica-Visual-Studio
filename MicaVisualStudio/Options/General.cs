using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Community.VisualStudio.Toolkit;
using MicaVisualStudio.Enums;
using MicaVisualStudio.Contracts;
using MicaVisualStudio.Options;
using MicaVisualStudio.Options.Controls;

namespace MicaVisualStudio;

internal partial class OptionsProvider
{
    [ComVisible(true)]
    public class GeneralOptions : UIElementDialogPage
    {
        protected override UIElement Child => new GeneralPage();
    }

    [ComVisible(true)]
    public class ToolOptions : UIElementDialogPage
    {
        protected override UIElement Child => new ToolPage();
    }

    [ComVisible(true)]
    public class DialogOptions : UIElementDialogPage
    {
        protected override UIElement Child => new Options.Controls.DialogPage();
    }
}

/// <summary>
/// Represents the general options used by Mica Visual Studio.
/// </summary>
public class General : ObservableOptionModel<General>, IGeneral
{
    [OverrideDataType(SettingDataType.Int32)]
    public Theme Theme { get; set; } = Theme.VisualStudio;

    [OverrideDataType(SettingDataType.Int32)]
    public BackdropType Backdrop { get; set; } = BackdropType.Mica;

    [OverrideDataType(SettingDataType.Int32)]
    public CornerPreference CornerPreference { get; set; } = CornerPreference.Default;

    public bool ToolWindows { get; set; } = false;

    [OverrideDataType(SettingDataType.Int32)]
    public Theme ToolTheme { get; set; } = Theme.VisualStudio;

    [OverrideDataType(SettingDataType.Int32)]
    public BackdropType ToolBackdrop { get; set; } = BackdropType.Mica;

    [OverrideDataType(SettingDataType.Int32)]
    public CornerPreference ToolCornerPreference { get; set; } = CornerPreference.Default;

    public bool DialogWindows { get; set; } = false;

    [OverrideDataType(SettingDataType.Int32)]
    public Theme DialogTheme { get; set; } = Theme.VisualStudio;

    [OverrideDataType(SettingDataType.Int32)]
    public BackdropType DialogBackdrop { get; set; } = BackdropType.Mica;

    [OverrideDataType(SettingDataType.Int32)]
    public CornerPreference DialogCornerPreference { get; set; } = CornerPreference.Default;

    [OverrideDataType(SettingDataType.Int32)]
    public Theme AppTheme { get; set; } = Theme.VisualStudio;

    public bool ForceTransparency { get; set; } = true;

    public bool LayeredWindows { get; set; } = true;

    public bool AcrylicMenus { get; set; } = true;
}
