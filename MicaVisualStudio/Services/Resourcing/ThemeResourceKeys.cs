using Microsoft.VisualStudio.Shell;

namespace MicaVisualStudio.Services.Resourcing;

public static class ThemeResourceKeys
{
    public static readonly ThemeResourceKey SolidBackgroundFillTertiary =
        new(category: new("73708ded-2d56-4aad-b8eb-73b20d3f4bff"), name: "SolidBackgroundFillTertiary", ThemeResourceKeyType.BackgroundBrush);

    public static readonly ThemeResourceKey TextFillPrimary =
        new(category: new("73708ded-2d56-4aad-b8eb-73b20d3f4bff"), name: "TextFillPrimary", ThemeResourceKeyType.BackgroundBrush);

    public static readonly ThemeResourceKey TextOnAccentFillPrimary =
        new(category: new("73708ded-2d56-4aad-b8eb-73b20d3f4bff"), name: "TextOnAccentFillPrimary", ThemeResourceKeyType.BackgroundBrush);

    public static readonly ThemeResourceKey ScrollBarBackground =
        new(category: new("{624ed9c3-bdfd-41fa-96c3-7c824ea32e3d}"), name: "ScrollBarBackground", ThemeResourceKeyType.BackgroundBrush);

    public static readonly ThemeResourceKey MainWindowActiveCaption =
        new(category: new("624ed9c3-bdfd-41fa-96c3-7c824ea32e3d"), name: "MainWindowActiveCaption", ThemeResourceKeyType.BackgroundColor);
}
