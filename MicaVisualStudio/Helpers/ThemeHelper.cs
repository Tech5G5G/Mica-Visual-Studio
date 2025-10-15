using Microsoft.Win32;
using Microsoft.VisualStudio.PlatformUI;

namespace MicaVisualStudio.Helpers;

public class ThemeHelper
{
    #region Visual Studio Theme

    public Theme VisualStudioTheme => visualStudioTheme;
    private Theme visualStudioTheme;

    public event EventHandler<Theme> VisualStudioThemeChanged;

    private Theme GetVisualStudioTheme() =>
        shell?.GetThemedWPFColor(MainWindowActiveCaptionKey).IsLight() == true ? Theme.Light : Theme.Dark;

    private readonly static ThemeResourceKey MainWindowActiveCaptionKey =
        new(category: new("624ed9c3-bdfd-41fa-96c3-7c824ea32e3d"), name: "MainWindowActiveCaption", ThemeResourceKeyType.BackgroundColor);

    private readonly IVsUIShell5 shell;

    #endregion

    #region System Theme

    private const string PersonalizeSettings = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public Theme SystemTheme => systemTheme;
    private Theme systemTheme;

    public event EventHandler<Theme> SystemThemeChanged;

    private Theme GetSystemTheme() =>
        (int)Registry.GetValue(PersonalizeSettings, valueName: "AppsUseLightTheme", defaultValue: 0) == 1 ? Theme.Light : Theme.Dark;

    #endregion

    public ThemeHelper()
    {
#pragma warning disable VSTHRD010 //Invoke single-threaded types on Main thread
        shell = (IVsUIShell5)Package.GetGlobalService(typeof(SVsUIShell));
#pragma warning restore VSTHRD010 //Invoke single-threaded types on Main thread

        visualStudioTheme = GetVisualStudioTheme();
        systemTheme = GetSystemTheme();

        SystemEvents.InvokeOnEventsThread(new Action(() => SystemEvents.UserPreferenceChanging += PreferenceChanging));
        VSColorTheme.ThemeChanged += (e) =>
        {
            visualStudioTheme = GetVisualStudioTheme();
            VisualStudioThemeChanged?.Invoke(shell, VisualStudioTheme);
        };

        //Dispose normally
        WindowManager.MainWindow.Unloaded += (s, e) => //Probably means application is closing, so unhook UserPreferenceChanging to prevent memory leaks
            SystemEvents.InvokeOnEventsThread(new Action(() => SystemEvents.UserPreferenceChanging -= PreferenceChanging));

        void PreferenceChanging(object sender, UserPreferenceChangingEventArgs args)
        {
            if (args.Category == UserPreferenceCategory.General)
            {
                systemTheme = GetSystemTheme();
                SystemThemeChanged?.Invoke(sender, systemTheme);
            }
        }
    }
}

public enum Theme
{
    VisualStudio,
    System,
    Light,
    Dark
}
