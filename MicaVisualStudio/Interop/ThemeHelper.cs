using Microsoft.Win32;

namespace MicaVisualStudio.Interop;

public class ThemeHelper : IDisposable
{
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

    public Theme SystemTheme => sysTheme;
    private Theme sysTheme;

    public event EventHandler<Theme> SystemThemeChanged;

    private Theme GetSystemTheme() =>
        (int)Registry.GetValue(PersonalizeSettings, valueName: "AppsUseLightTheme", defaultValue: 0) == 1 ? Theme.Light : Theme.Dark;

    #endregion

    public static ThemeHelper Instance { get; } = new();

    private ThemeHelper()
    {
        sysTheme = GetSystemTheme();
        SystemEvents.InvokeOnEventsThread(new Action(() => SystemEvents.UserPreferenceChanging += PreferenceChanging));
    }

    private void PreferenceChanging(object sender, UserPreferenceChangingEventArgs args)
    {
        if (args.Category == UserPreferenceCategory.General)
            SystemThemeChanged?.Invoke(this, sysTheme = GetSystemTheme());
    }

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

    ~ThemeHelper() => Dispose(disposing: false);

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            SystemEvents.InvokeOnEventsThread(new Action(() => SystemEvents.UserPreferenceChanging -= PreferenceChanging));
            disposed = true;
        }
    }

    #endregion
}
