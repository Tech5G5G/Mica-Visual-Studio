using System;
using Microsoft.Win32;
using MicaVisualStudio.Options;
using MicaVisualStudio.Contracts;

namespace MicaVisualStudio.Services;

public sealed class ThemeService : IThemeService, IDisposable
{
    public event EventHandler<Theme> SystemThemeChanged;

    public Theme SystemTheme => _theme;
    private Theme _theme = GetSystemTheme();

    public ThemeService()
    {
        SystemEvents.InvokeOnEventsThread(new Action(() => SystemEvents.UserPreferenceChanging += OnPreferenceChanging));
    }

    private static Theme GetSystemTheme()
    {
        return (int)Registry.GetValue(
            keyName: @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            valueName: "AppsUseLightTheme",
            defaultValue: 0)
            == 1 /* TRUE */ ? Theme.Light : Theme.Dark;
    }

    private void OnPreferenceChanging(object sender, UserPreferenceChangingEventArgs e)
    {
        SystemThemeChanged?.Invoke(this, _theme = GetSystemTheme());
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
