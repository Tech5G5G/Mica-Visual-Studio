using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using Microsoft.VisualStudio.PlatformUI;
using MicaVisualStudio.Options;
using MicaVisualStudio.Interop;
using MicaVisualStudio.Contracts;
using MicaVisualStudio.Services.Windowing;

namespace MicaVisualStudio.Services;

public sealed class BackdropManager : IBackdropManager, IDisposable
{
    private readonly ILogger _logger;
    private readonly IGeneral _general;
    private readonly IThemeService _theme;
    private readonly IWindowManager _window;
    private readonly IResourceManager _resource;

    public BackdropManager(
        ILogger logger,
        IGeneral general,
        IThemeService theme,
        IWindowManager window,
        IResourceManager resource)
    {
        _logger = logger;
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

    private void OnOptionChanged(object sender, PropertyChangedEventArgs e)
    {
        RefreshPreferences(firstTime: false);
    }

    private void OnThemeChanged(object sender, Theme e)
    {
        RefreshPreferences(firstTime: false);
    }

    private void OnWindowOpened(object sender, WindowActionEventArgs e)
    {
        ApplyWindowPreferences(e.WindowHandle, e.Window, firstTime: true);
    }

    private void ApplyWindowPreferences(nint handle, Window window, bool firstTime)
    {
        try
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
                PInvoke.ExtendFrameIntoClientArea(handle);
                source.CompositionTarget?.BackgroundColor = Colors.Transparent;

                // Don't remove caption buttons from windows that need them
                if (window.WindowStyle == WindowStyle.None || window is not DialogWindowBase)
                {
                    PInvoke.RemoveCaptionButtons(source);
                }
            }

            switch (window.WindowType)
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
        }
        catch (Exception ex)
        {
            _logger.Output(ex);
        }

        void ApplyWindowAttributes(Theme theme, BackdropType backdrop, CornerPreference corner)
        {
            PInvoke.EnableDarkMode(handle, EvaluateTheme(theme) == Theme.Dark);
            PInvoke.SetBackdropType(handle, window is null && backdrop == BackdropType.Glass ? BackdropType.None : backdrop);
            PInvoke.SetCornerPreference(handle, corner);
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

    #region Dispose

    private bool _disposed;

    void IDisposable.Dispose()
    {
        if (!_disposed)
        {
            _general.PropertyChanged -= OnOptionChanged;
            _theme.SystemThemeChanged -= OnThemeChanged;
            _window.WindowOpened -= OnWindowOpened;
            _resource.VisualStudioThemeChanged -= OnThemeChanged;

            _disposed = true;
        }
    }

    #endregion
}
