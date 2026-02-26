using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Community.VisualStudio.Toolkit;
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
/// Represents the options used by Mica Visual Studio.
/// </summary>
public class General : ObservableOptionModel<General>, IGeneral
{
    [OverrideDataType(SettingDataType.Int32)]
    public Theme Theme
    {
        get => field;
        set => SetValue(ref field, value);
    } = Theme.VisualStudio;

    [OverrideDataType(SettingDataType.Int32)]
    public BackdropType Backdrop
    {
        get => field;
        set => SetValue(ref field, value);
    } = BackdropType.Mica;

    [OverrideDataType(SettingDataType.Int32)]
    public CornerPreference CornerPreference
    {
        get => field;
        set => SetValue(ref field, value);
    } = CornerPreference.Default;

    public bool ToolWindows
    {
        get => field;
        set => SetValue(ref field, value);
    } = false;

    [OverrideDataType(SettingDataType.Int32)]
    public Theme ToolTheme
    {
        get => field;
        set => SetValue(ref field, value);
    } = Theme.VisualStudio;

    [OverrideDataType(SettingDataType.Int32)]
    public BackdropType ToolBackdrop
    {
        get => field;
        set => SetValue(ref field, value);
    } = BackdropType.Mica;

    [OverrideDataType(SettingDataType.Int32)]
    public CornerPreference ToolCornerPreference
    {
        get => field;
        set => SetValue(ref field, value);
    } = CornerPreference.Default;

    public bool DialogWindows
    {
        get => field;
        set => SetValue(ref field, value);
    } = false;

    [OverrideDataType(SettingDataType.Int32)]
    public Theme DialogTheme
    {
        get => field;
        set => SetValue(ref field, value);
    } = Theme.VisualStudio;

    [OverrideDataType(SettingDataType.Int32)]
    public BackdropType DialogBackdrop
    {
        get => field;
        set => SetValue(ref field, value);
    } = BackdropType.Mica;

    [OverrideDataType(SettingDataType.Int32)]
    public CornerPreference DialogCornerPreference
    {
        get => field;
        set => SetValue(ref field, value);
    } = CornerPreference.Default;

    [OverrideDataType(SettingDataType.Int32)]
    public Theme AppTheme
    {
        get => field;
        set => SetValue(ref field, value);
    } = Theme.VisualStudio;

    public bool ForceTransparency
    {
        get => field;
        set => SetValue(ref field, value);
    } = true;

    public bool LayeredWindows
    {
        get => field;
        set => SetValue(ref field, value);
    } = true;

    public bool AcrylicMenus
    {
        get => field;
        set => SetValue(ref field, value);
    } = true;
}
