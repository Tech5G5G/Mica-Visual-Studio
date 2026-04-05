using System;
using Microsoft.Win32;
using MicaVisualStudio.Options;
using MicaVisualStudio.Contracts;

namespace MicaVisualStudio.Services;

public sealed class ThemeService : IThemeService, IDisposable
{
    public event EventHandler<Theme> SystemThemeChanged;

    public Theme SystemTheme => _theme;
    private Theme _theme;

    public ThemeService()
    {
        GetSystemTheme(out _theme);
        SystemEvents.InvokeOnEventsThread(new Action(() => SystemEvents.UserPreferenceChanging += OnPreferenceChanging));
    }

    private bool GetSystemTheme(out Theme theme)
    {
        theme = (int)Registry.GetValue(
            keyName: @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            valueName: "AppsUseLightTheme",
            defaultValue: 0)
            == 1 /* TRUE */ ? Theme.Light : Theme.Dark;
        return _theme != theme;
    }

    private void OnPreferenceChanging(object sender, UserPreferenceChangingEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General &&
            GetSystemTheme(out var theme))
        {
            SystemThemeChanged?.Invoke(this, _theme = theme);
        }
    }

    #region Dispose

    private bool _disposed;

    void IDisposable.Dispose()
    {
        if (!_disposed)
        {
            SystemEvents.InvokeOnEventsThread(new Action(() => SystemEvents.UserPreferenceChanging -= OnPreferenceChanging));
            SystemThemeChanged = null;

            _disposed = true;
        }
    }

    #endregion
}
