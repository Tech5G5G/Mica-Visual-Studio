using Microsoft.Internal.VisualStudio.PlatformUI;

namespace MicaVisualStudio.VisualStudio;

public partial class VsWindowStyler
{
    #region Keys

    private const string SolidBackgroundFillTertiaryLayeredKey = "VsBrush.SolidBackgroundFillTertiaryLayered",
        PopupBackgroundLayeredKey = "VsBrush.PopupBackgroundLayered",
        PopupBorderOnAcrylicKey = "VsBrush.PopupBorderOnAcrylic";

    private readonly ThemeResourceKey SolidBackgroundFillTertiaryKey =
        new(category: new("73708ded-2d56-4aad-b8eb-73b20d3f4bff"), name: "SolidBackgroundFillTertiary", ThemeResourceKeyType.BackgroundBrush);

    private readonly ThemeResourceKey TextFillPrimaryKey =
        new(category: new("73708ded-2d56-4aad-b8eb-73b20d3f4bff"), name: "TextFillPrimary", ThemeResourceKeyType.BackgroundBrush);

    private readonly ThemeResourceKey TextOnAccentFillPrimaryKey =
        new(category: new("73708ded-2d56-4aad-b8eb-73b20d3f4bff"), name: "TextOnAccentFillPrimary", ThemeResourceKeyType.BackgroundBrush);

    private readonly ThemeResourceKey ScrollBarBackgroundKey =
        new(category: new("{624ed9c3-bdfd-41fa-96c3-7c824ea32e3d}"), name: "ScrollBarBackground", ThemeResourceKeyType.BackgroundBrush);

    #endregion

    private void AddBrushes(bool subscribe = true)
    {
        if (subscribe)
            (Application.Current.Resources.MergedDictionaries as INotifyCollectionChanged).CollectionChanged += (s, e) => AddBrushes(subscribe: false);

        var color = shell5?.GetThemedWPFColor(SolidBackgroundFillTertiaryKey) ?? default;

        SolidColorBrush halfBrush = new(color with { A = 0xFF / 2 }); //50% opacity
        SolidColorBrush quarterBrush = new(color with { A = 0xFF / 4 }); //25% opacity

        foreach (var dictionary in Application.Current.Resources.MergedDictionaries.OfType<DeferredResourceDictionaryBase>())
        {
            if (!dictionary.Contains(SolidBackgroundFillTertiaryLayeredKey))
                dictionary.Add(SolidBackgroundFillTertiaryLayeredKey, halfBrush);

            if (!dictionary.Contains(PopupBackgroundLayeredKey))
                dictionary.Add(
                    PopupBackgroundLayeredKey,
                    VsColorManager.Instance.VisualStudioTheme == Theme.Dark && color.IsGray() ?
                    new SolidColorBrush(Color.FromArgb(0x01, 0x00, 0x00, 0x00)) : //Full acrylic experience for those who can handle it
                    quarterBrush);

            if (!dictionary.Contains(PopupBorderOnAcrylicKey))
                dictionary.Add(
                   PopupBorderOnAcrylicKey,
                   new SolidColorBrush(VsColorManager.Instance.VisualStudioTheme == Theme.Dark ?
                   Color.FromArgb(0x55, 0x000, 0x00, 0x00) :
                   Color.FromArgb(0x20, 0x000, 0x00, 0x00)));
        }
    }
}