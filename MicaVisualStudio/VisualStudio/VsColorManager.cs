using System.Collections;
using Microsoft.Internal.VisualStudio.PlatformUI;

namespace MicaVisualStudio.VisualStudio;

public sealed class VsColorManager
{
    public static VsColorManager Instance { get; } = new();

    private static Color TransparentWhite = Colors.Transparent,
        TranslucentBlack = Color.FromArgb(0x01, 0x00, 0x00, 0x00); //Slight alpha to change icon foreground (basically invisible)

    public ReadOnlyDictionary<string, ColorConfig> Configs => new(configs);
    private readonly Dictionary<string, ColorConfig> configs = [];

    #region Visual Studio Theme

    public Theme VisualStudioTheme => vsTheme;
    private Theme vsTheme;

    public event EventHandler<Theme> VisualStudioThemeChanged;

    private readonly ThemeResourceKey MainWindowActiveCaptionKey =
        new(category: new("624ed9c3-bdfd-41fa-96c3-7c824ea32e3d"), name: "MainWindowActiveCaption", ThemeResourceKeyType.BackgroundColor);

    private readonly IVsUIShell5 shell = VS.GetRequiredService<SVsUIShell, IVsUIShell5>();

    public Color GetColor(ThemeResourceKey key) =>
        shell?.GetThemedWPFColor(key) ?? default;

    /// <summary>
    /// Gets the current <see cref="Theme"/> of Visual Studio.
    /// </summary>
    /// <param name="theme">The <see cref="Theme"/> used by the Visual Studio.</param>
    /// <returns>Whether or not the value has changed; that is, if <paramref name="theme"/> is different from <see cref="vsTheme"/>.</returns>
    private bool GetVSTheme(out Theme theme)
    {
        theme = GetColor(MainWindowActiveCaptionKey).IsLight() == true ? Theme.Light : Theme.Dark;
        return vsTheme != theme;
    }

    #endregion

    private VsColorManager()
    {
        GetVSTheme(out vsTheme);
        (Application.Current.Resources.MergedDictionaries as INotifyCollectionChanged).CollectionChanged += (s, e) =>
        {
            UpdateColors();

            if (GetVSTheme(out Theme theme))
                VisualStudioThemeChanged?.Invoke(this, vsTheme = theme);
        };
    }

    public void UpdateColors()
    {
        foreach (var dictionary in Application.Current.Resources.MergedDictionaries.OfType<DeferredResourceDictionaryBase>())
            foreach (var obj in dictionary)
            {
                DictionaryEntry entry = (DictionaryEntry)obj;

                if (GetConfig(entry.Key) is not ColorConfig config)
                    continue;

                if (entry.Value is Brush brush)
                {
                    Brush clone = brush.CloneCurrentValue();

                    if (clone is SolidColorBrush solid)
                        solid.Color = DetermineColor(solid.Color, config);
                    else if (clone is GradientBrush gradient)
                        foreach (var stop in gradient.GradientStops)
                            stop.Color = DetermineColor(stop.Color, config);

                    dictionary[entry.Key] = clone;
                }
                else if (entry.Value is Color color)
                    dictionary[entry.Key] = DetermineColor(color, config);
            }
    }

    #region Configuration

    public void AddConfig(string key, ColorConfig config)
    {
        if (configs.ContainsKey(key))
            configs[key] = config;
        else
            configs.Add(key, config);
    }

    public void AddConfigs(Dictionary<string, ColorConfig> configs)
    {
        foreach (var config in configs)
            AddConfig(config.Key, config.Value);
    }

    public void RemoveConfig(string key)
    {
        if (configs.ContainsKey(key))
            configs.Remove(key);
    }

    public void RemoveConfigs(params string[] keys)
    {
        foreach (var key in keys)
            RemoveConfig(key);
    }

    private ColorConfig GetConfig(object key)
    {
        if (key is ThemeResourceKey theme &&
            (theme.KeyType == ThemeResourceKeyType.BackgroundColor || theme.KeyType == ThemeResourceKeyType.BackgroundBrush))
            return configs.TryGetValue(theme.Name, out ColorConfig config) ? config : null;
        else if (key is string str)
            return configs.TryGetValue(str.Replace("VsBrush.", null).Replace("VsColor.", null), out ColorConfig config) ? config : null;
        else
            return null;
    }

    #endregion

    private Color DetermineColor(Color color, ColorConfig config) =>
        !config.IsTranslucent || (config.TransparentOnGray && color.R == color.G && color.G == color.B) ?
        (vsTheme == Theme.Light ? TransparentWhite : TranslucentBlack) :
        Color.FromArgb(Math.Min(config.Opacity, color.A), color.R, color.G, color.B);
}

public class ColorConfig(bool transparentOnGray = true, bool translucent = false, byte opacity = 0x38)
{
    public static ColorConfig Default = new(),
        Layered = new(transparentOnGray: false, translucent: true, opacity: 0x7F);

    public bool TransparentOnGray { get; } = transparentOnGray;

    public bool IsTranslucent { get; } = translucent;

    public byte Opacity { get; } = opacity;
}
