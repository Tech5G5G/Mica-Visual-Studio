using System;
using Microsoft.Win32;
using MicaVisualStudio.Options;
using MicaVisualStudio.Contracts;

namespace MicaVisualStudio.Services;

public class ThemeService : IThemeService, IDisposable
{
    /// <summary>
    /// Occurs when <see cref="SystemTheme"/> has changed.
    /// </summary>
    public event EventHandler<Theme> SystemThemeChanged;

    /// <summary>
    /// Gets the current <see cref="Theme"/> used by the system.
    /// </summary>
    public Theme SystemTheme => _theme;
    private Theme _theme = GetSystemTheme();

    public ThemeService() =>
        SystemEvents.InvokeOnEventsThread(new Action(() => SystemEvents.UserPreferenceChanging += OnPreferenceChanging));

    private static Theme GetSystemTheme()
    {
        return (int)Registry.GetValue(
            keyName: @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            valueName: "AppsUseLightTheme",
            defaultValue: 0)
            == 1 /* TRUE */ ? Theme.Light : Theme.Dark;
    }

    private void OnPreferenceChanging(object sender, UserPreferenceChangingEventArgs args)
    {
        var theme = _theme = GetSystemTheme();
        SystemThemeChanged?.Invoke(this, theme);
    }

    #region Dispose

    private bool _disposed;

    void IDisposable.Dispose()
    {
        if (!_disposed)
        {
            SystemEvents.InvokeOnEventsThread(new Action(() => SystemEvents.UserPreferenceChanging -= OnPreferenceChanging));
            _disposed = true;
        }
    }

    #endregion
}
