using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using Microsoft.VisualStudio.PlatformUI;
using MicaVisualStudio.Options;
using MicaVisualStudio.Interop;
using MicaVisualStudio.Contracts;
using MicaVisualStudio.Windowing;
using MicaVisualStudio.Resourcing;

namespace MicaVisualStudio.Services;

public class BackdropManager : IBackdropManager
{
    private readonly IGeneral _general;
    private readonly IThemeService _theme;
    private readonly IWindowManager _window;
    private readonly IResourceManager _resource;

    public BackdropManager(
        IGeneral general,
        IThemeService theme,
        IWindowManager window,
        IResourceManager resource)
    {
        _general = general;
        _theme = theme;
        _window = window;
        _resource = resource;

        general.PropertyChanged += OnOptionChanged;
        theme.SystemThemeChanged += OnThemeChanged;
        window.WindowOpened += OnWindowOpened;
        resource.VisualStudioThemeChanged += OnThemeChanged;

        RefreshPreferences(firstTime: true);
    }

    private void OnOptionChanged(object sender, PropertyChangedEventArgs args)
    {
        RefreshPreferences(firstTime: false);
    }

    private void OnThemeChanged(object sender, Theme args)
    {
        RefreshPreferences(firstTime: false);
    }

    private void OnWindowOpened(object sender, WindowActionEventArgs args)
    {
        ApplyWindowPreferences(args.WindowHandle, args.Window, firstTime: true);
    }

    private void ApplyWindowPreferences(nint handle, Window window, bool firstTime)
    {
        if (window?.AllowsTransparency == true)
        {
            // Don't apply to transparent windows
            return;
        }

        if (firstTime && // Remove caption buttons once
            window is not null && // Check that window is WPF
            HwndSource.FromHwnd(handle) is HwndSource source)
        {
            WindowHelper.ExtendFrameIntoClientArea(handle);
            source.CompositionTarget?.BackgroundColor = Colors.Transparent;

            // Don't remove caption buttons from windows that need them
            if (window.WindowStyle == WindowStyle.None || window is not DialogWindowBase)
            {
                WindowHelper.RemoveCaptionButtons(source);
            }
        }

        switch (WindowHelper.GetWindowType(window))
        {
            default:
            case WindowType.Main:
                ApplyWindowAttributes(
                    _general.Theme,
                    _general.Backdrop,
                    _general.CornerPreference);
                break;

            case WindowType.Tool when _general.ToolWindows:
                ApplyWindowAttributes(
                    _general.ToolTheme,
                    _general.ToolBackdrop,
                    _general.ToolCornerPreference);
                break;

            case WindowType.Dialog when _general.DialogWindows:
                ApplyWindowAttributes(
                    _general.DialogTheme,
                    _general.DialogBackdrop,
                    _general.DialogCornerPreference);
                break;
        }

        void ApplyWindowAttributes(Theme theme, BackdropType backdrop, CornerPreference corner)
        {
            WindowHelper.EnableDarkMode(handle, EvaluateTheme(theme) == Theme.Dark);
            WindowHelper.SetBackdropType(handle, window is null && backdrop == BackdropType.Glass ? BackdropType.None : backdrop);
            WindowHelper.SetCornerPreference(handle, corner);
        }
    }

    private Theme EvaluateTheme(Theme theme)
    {
        return theme switch
        {
            Theme.VisualStudio => _resource.VisualStudioTheme,
            Theme.System => _theme.SystemTheme,
            _ => theme
        };
    }

    public void RefreshPreferences(bool firstTime)
    {
        PInvoke.SetAppMode(
            EvaluateTheme(_general.AppTheme) == Theme.Light ? PInvoke.PreferredAppMode.ForceLight : PInvoke.PreferredAppMode.ForceDark);

        foreach (var entry in _window.Windows)
        {
            ApplyWindowPreferences(entry.Key, entry.Value, firstTime);
        }
    }
}
