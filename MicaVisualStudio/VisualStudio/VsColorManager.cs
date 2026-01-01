using System.Collections;
using Microsoft.Internal.VisualStudio.PlatformUI;

namespace MicaVisualStudio.VisualStudio;

/// <summary>
/// Represent a manager for the various <see cref="Color"/>s and <see cref="Brush"/>es used by Visual Studio.
/// </summary>
public sealed class VsColorManager
{
    /// <summary>
    /// Gets the singleton instance of <see cref="VsColorManager"/>.
    /// </summary>
    public static VsColorManager Instance => field ??= new();

    private static Color TransparentWhite = Colors.Transparent,
        TranslucentBlack = Color.FromArgb(0x01, 0x00, 0x00, 0x00); //Slight alpha to change icon foreground (basically invisible)

    /// <summary>
    /// Gets a <see cref="ReadOnlyDictionary{TKey, TValue}"/> containing all the <see cref="ColorConfig"/>s currently in effect.
    /// </summary>
    public ReadOnlyDictionary<string, ColorConfig> Configs => new(configs);
    private readonly Dictionary<string, ColorConfig> configs = [];

    #region Visual Studio Theme

    /// <summary>
    /// Gets the current <see cref="Theme"/> used by Visual Studio.
    /// </summary>
    public Theme VisualStudioTheme => vsTheme;
    private Theme vsTheme;

    /// <summary>
    /// Occurs when <see cref="VisualStudioTheme"/> has changed.
    /// </summary>
    public event EventHandler<Theme> VisualStudioThemeChanged;

    private readonly ThemeResourceKey MainWindowActiveCaptionKey =
        new(category: new("624ed9c3-bdfd-41fa-96c3-7c824ea32e3d"), name: "MainWindowActiveCaption", ThemeResourceKeyType.BackgroundColor);

    private readonly IVsUIShell5 shell = VS.GetRequiredService<SVsUIShell, IVsUIShell5>();

    /// <summary>
    /// Gets the current <see cref="Theme"/> of Visual Studio.
    /// </summary>
    /// <param name="theme">The <see cref="Theme"/> used by the Visual Studio.</param>
    /// <returns>Whether or not the value has changed; that is, if <paramref name="theme"/> is different from <see cref="vsTheme"/>.</returns>
    private bool GetVSTheme(out Theme theme)
    {
        theme = shell?.GetThemedWPFColor(MainWindowActiveCaptionKey).IsLight() == true ? Theme.Light : Theme.Dark;
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

    /// <summary>
    /// Updates the <see cref="Color"/>s and <see cref="Brush"/>es used by Visual Studio.
    /// </summary>
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

    /// <summary>
    /// Adds <paramref name="config"/> to <see cref="Configs"/>.
    /// </summary>
    /// <param name="key">A key to configure.</param>
    /// <param name="config">A <see cref="ColorConfig"/> containing configuration information.</param>
    public void AddConfig(string key, ColorConfig config)
    {
        if (configs.ContainsKey(key))
            configs[key] = config;
        else
            configs.Add(key, config);
    }

    /// <summary>
    /// Adds multiple <paramref name="configs"/> to <see cref="Configs"/>.
    /// </summary>
    /// <param name="configs">A <see cref="Dictionary{TKey, TValue}"/> containing keys and their <see cref="ColorConfig"/>.</param>
    public void AddConfigs(Dictionary<string, ColorConfig> configs)
    {
        foreach (var config in configs)
            AddConfig(config.Key, config.Value);
    }

    /// <summary>
    /// Removes a <see cref="ColorConfig"/> from <see cref="Configs"/>.
    /// </summary>
    /// <param name="key">The key for which the <see cref="ColorConfig"/> is to be removed for.</param>
    public void RemoveConfig(string key)
    {
        if (configs.ContainsKey(key))
            configs.Remove(key);
    }

    /// <summary>
    /// Removes multiple <see cref="ColorConfig"/>s from <see cref="Configs"/>.
    /// </summary>
    /// <param name="keys">The keys for which the <see cref="ColorConfig"/>s are to be removed for.</param>
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
        !config.IsTranslucent || (config.TransparentOnGray && color.IsGray()) ?
        (vsTheme == Theme.Light ? TransparentWhite : TranslucentBlack) :
        Color.FromArgb(Math.Min(config.Opacity, color.A), color.R, color.G, color.B);
}

/// <summary>
/// Represents the configuration for a <see cref="Color"/> or <see cref="Brush"/> used by Visual Studio.
/// </summary>
/// <param name="transparentOnGray">Whether or not to make the <see cref="Color"/> or <see cref="Brush"/> completely transparent if it is gray.</param>
/// <param name="translucent">Whether or not to allow <paramref name="opacity"/>.</param>
/// <param name="opacity">The opacity to make the <see cref="Color"/> or <see cref="Brush"/>.</param>
public class ColorConfig(bool transparentOnGray = true, bool translucent = false, byte opacity = 0x38)
{
    /// <summary>
    /// Represents the default <see cref="ColorConfig"/> for a <see cref="Color"/> or <see cref="Brush"/>.
    /// </summary>
    /// <remarks>Equivalent to a <see cref="ColorConfig"/> initialized to its default value; that is, with no parameters modified.</remarks>
    public static readonly ColorConfig Default = new();
    /// <summary>
    /// Represents the <see cref="ColorConfig"/> for a layered <see cref="Color"/> or <see cref="Brush"/>.
    /// </summary>
    /// <remarks>
    /// Equivalent to a <see cref="ColorConfig"/> with the following properties:
    /// <list type="bullet">
    /// <item>
    /// <see cref="TransparentOnGray"/> to <see langword="false"/>
    /// </item>
    /// <item>
    /// <see cref="IsTranslucent"/> to <see langword="true"/>.
    /// </item>
    /// <item>
    /// <see cref="Opacity"/> to <c>0x7F</c>.
    /// </item>
    /// </list>
    /// </remarks>
    public static readonly ColorConfig Layered = new(transparentOnGray: false, translucent: true, opacity: 0x7F);

    public bool TransparentOnGray { get; } = transparentOnGray;

    public bool IsTranslucent { get; } = translucent;

    public byte Opacity { get; } = opacity;
}
