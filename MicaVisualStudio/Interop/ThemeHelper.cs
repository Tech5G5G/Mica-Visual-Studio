using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using MicaVisualStudio.Options;

namespace MicaVisualStudio.Interop;

/// <summary>
/// Represents a helper for getting and receiving updates about the system theme.
/// </summary>
public sealed class ThemeHelper : IDisposable
{
    /// <summary>
    /// Gets the singleton instance of <see cref="ThemeHelper"/>.
    /// </summary>
    public static ThemeHelper Instance => field ??= new();

    #region PInvoke

    [DllImport("uxtheme.dll", EntryPoint = "#135")]
    private static extern int SetPreferredAppMode(PreferredAppMode preferredAppMode);

    [DllImport("uxtheme.dll", EntryPoint = "#136")]
    private static extern void FlushMenuThemes();

    private enum PreferredAppMode
    {
        ForceDark = 2,
        ForceLight = 3
    }

    #endregion

    #region System Theme

    private const string PersonalizeSettings = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    /// <summary>
    /// Gets the current <see cref="Theme"/> used by the system.
    /// </summary>
    public Theme SystemTheme => sysTheme;
    private Theme sysTheme;

    /// <summary>
    /// Occurs when <see cref="SystemTheme"/> has changed.
    /// </summary>
    public event EventHandler<Theme> SystemThemeChanged;

    private Theme GetSystemTheme() =>
        (int)Registry.GetValue(PersonalizeSettings, valueName: "AppsUseLightTheme", defaultValue: 0) == 1 ? // TRUE
        Theme.Light : Theme.Dark;

    #endregion

    private ThemeHelper()
    {
        sysTheme = GetSystemTheme();
        SystemEvents.InvokeOnEventsThread(new Action(() => SystemEvents.UserPreferenceChanging += PreferenceChanging));
    }

    private void PreferenceChanging(object sender, UserPreferenceChangingEventArgs args) =>
        SystemThemeChanged?.Invoke(this, sysTheme = GetSystemTheme());

    /// <summary>
    /// Sets the theme of the current app.
    /// </summary>
    /// <remarks>Setting the app theme affects the theme of the system menu.</remarks>
    /// <param name="theme">The <see cref="Theme"/> to set the app theme to.</param>
    public void SetAppTheme(Theme theme)
    {
        SetPreferredAppMode(theme switch
        {
            Theme.Dark => PreferredAppMode.ForceDark,
            Theme.Light or _ => PreferredAppMode.ForceLight
        });

        FlushMenuThemes();
    }

    #region Dispose

    private bool disposed;

    /// <summary>
    /// Disposes the singleton instance of <see cref="ThemeHelper"/>.
    /// </summary>
    /// <remarks>Calling this method will stop the <see cref="SystemThemeChanged"/> event from occuring.</remarks>
    public void Dispose()
    {
        if (!disposed)
        {
            SystemEvents.InvokeOnEventsThread(new Action(() => SystemEvents.UserPreferenceChanging -= PreferenceChanging));
            disposed = true;
        }
    }

    #endregion
}
