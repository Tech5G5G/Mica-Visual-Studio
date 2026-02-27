using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using MicaVisualStudio.Options;

namespace MicaVisualStudio.Resourcing;

public class ResourceManager : IResourceManager
{
    private static readonly ThemeResourceKey MainWindowActiveCaptionKey =
        new(category: new("624ed9c3-bdfd-41fa-96c3-7c824ea32e3d"), name: "MainWindowActiveCaption", ThemeResourceKeyType.BackgroundColor);

    private static Color TransparentWhite = Colors.Transparent,
                         TranslucentBlack = Color.FromArgb(0x01, 0x00, 0x00, 0x00); // Slight alpha to change icon foreground (basically invisible)

    public IDictionary<string, ResourceConfiguration> Configurations => _configs;
    private readonly Dictionary<string, ResourceConfiguration> _configs = [];

    public IDictionary<object, CustomResource> CustomResources => _resources;
    private readonly Dictionary<object, CustomResource> _resources = [];

    public Theme VisualStudioTheme => _theme;
    private Theme _theme;

    public event EventHandler<Theme> VisualStudioThemeChanged;

    private readonly IVsUIShell5 _shell5;

    public ResourceManager(IVsUIShell5 shell5)
    {
        _shell5 = shell5;

        GetTheme(out _theme);
        (Application.Current.Resources.MergedDictionaries as INotifyCollectionChanged).CollectionChanged += (s, e) =>
        {
            // Listen for new dictionaries
            if (e.Action is not (NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Replace))
            {
                return;
            }

            ConfigureResources();
            AddCustomResources();

            if (GetTheme(out var theme))
            {
                VisualStudioThemeChanged?.Invoke(this, _theme = theme);
            }
        };
    }

    public void ConfigureResources()
    {
        foreach (var dictionary in Application.Current.Resources.MergedDictionaries.OfType<DeferredResourceDictionaryBase>())
            foreach (DictionaryEntry entry in dictionary)
            {
                if (GetConfiguration(entry.Key) is not ResourceConfiguration config)
                {
                    continue;
                }

                switch (entry.Value)
                {
                    case SolidColorBrush solid:
                        {
                            var clone = solid.CloneCurrentValue();
                            clone.Color = DetermineColor(clone.Color, config);
                            dictionary[entry.Key] = clone;
                        }
                        break;


                    case GradientBrush gradient:
                        {
                            var clone = gradient.CloneCurrentValue();

                            foreach (var stop in clone.GradientStops)
                            {
                                stop.Color = DetermineColor(stop.Color, config);
                            }

                            dictionary[entry.Key] = clone;
                        }
                        break;

                    case Color color:
                        dictionary[entry.Key] = DetermineColor(color, config);
                        break;
                }
            }
    }

    public void AddCustomResources()
    {
        foreach (var dictionary in Application.Current.Resources.MergedDictionaries.OfType<DeferredResourceDictionaryBase>())
            foreach (var pair in _resources)
            {
                var resource = pair.Value;
                var value = resource.Factory(
                    _theme,
                    resource.BaseResourceKey is null ? default : _shell5.GetThemedWPFColor(resource.BaseResourceKey));

                if (!dictionary.Contains(pair.Key))
                {
                    dictionary.Add(pair.Key, value);
                }
            }
    }

    private bool GetTheme(out Theme theme)
    {
        theme = _shell5.GetThemedWPFColor(MainWindowActiveCaptionKey).IsLight() ? Theme.Light : Theme.Dark;
        return _theme != theme;
    }

    private ResourceConfiguration? GetConfiguration(object key)
    {
        return key switch
        {
            string str => _configs.TryGetValue(str.Replace("VsBrush.", null).Replace("VsColor.", null), out ResourceConfiguration config) ? config : null,

            ThemeResourceKey theme when theme.KeyType is ThemeResourceKeyType.BackgroundColor or ThemeResourceKeyType.BackgroundBrush =>
                _configs.TryGetValue(theme.Name, out ResourceConfiguration config) ? config : null,

            _ => null
        };
    }

    private Color DetermineColor(Color color, ResourceConfiguration config)
    {
        if (!config.IsTranslucent || (config.TransparentIfGray && color.IsGray))
        {
            return _theme == Theme.Light ? TransparentWhite : TranslucentBlack;
        }
        else
        {
            return Color.FromArgb(Math.Min(config.Opacity, color.A), color.R, color.G, color.B);
        }
    }
}
