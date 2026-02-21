using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.PlatformUI;
using MonoMod.RuntimeDetour;
using MicaVisualStudio.Enums;
using MicaVisualStudio.Interop;
using MicaVisualStudio.Contracts;
using MicaVisualStudio.Extensions;

namespace MicaVisualStudio.Services;

public class MenuAcrylicizer : IMenuAcrylicizer, IDisposable
{
    private const string PopupBackgroundKey = "VsBrush.PopupBackgroundLayered",
                         PopupBorderKey = "VsBrush.PopupBorderOnAcrylic";

    private static readonly ThemeResourceKey SolidBackgroundFillTertiaryKey =
        new(category: new("73708ded-2d56-4aad-b8eb-73b20d3f4bff"), name: "SolidBackgroundFillTertiary", ThemeResourceKeyType.BackgroundBrush);

    private readonly ILogger _logger;
    private readonly IGeneral _general;
    private readonly IResourceManager _resource;

    private readonly ILHook _sourceDetour;

    public MenuAcrylicizer(ILogger logger, IGeneral general, IResourceManager resource)
    {
        _logger = logger;
        _general = general;
        _resource = resource;

        // Add brushes
        resource.CustomResources.Add(PopupBackgroundKey, new(SolidBackgroundFillTertiaryKey, (t, c) =>
            new SolidColorBrush(t == Theme.Light || !c.IsGray() ?
                c with { A = 0xFF / 4 /* 25% opacity */ } :
                Color.FromArgb(0x01, 0x00, 0x00, 0x00)))); // Full acrylic experience for those who can handle it

        resource.CustomResources.Add(PopupBorderKey, new(baseResourceKey: null, (t, c) =>
            new SolidColorBrush(t == Theme.Light ? Color.FromArgb(0x20, 0x000, 0x00, 0x00) : Color.FromArgb(0x55, 0x000, 0x00, 0x00))));

        resource.AddCustomResources();

        // Check if enabled
        if (!general.AcrylicMenus)
        {
            return;
        }

        // Generate HwndSouce.RootVisual.set detour
        _sourceDetour = typeof(HwndSource).GetProperty("RootVisual").SetMethod
                                          .CreateDetour<HwndSource, Visual>(RootVisualChanged);
    }

    public void RootVisualChanged(HwndSource instance, Visual value)
    {
        try
        {
            if (value is null || instance.CompositionTarget is null)
            {
                return;
            }

            if (value is FrameworkElement root &&
                root.Parent is Popup popup) // Check if owned by popup
                                            // Popups use separate element as root of HwndSource
            {
                AcrylicizePopup(popup, instance, root);
            }
            else if (value is not Window) // Avoid already handled values
            {
                instance.CompositionTarget.BackgroundColor = Colors.Transparent;
            }
        }
        catch (Exception ex)
        {
            _logger.Output(ex);
        }
    }

    public void AcrylicizePopup(Popup popup, HwndSource source, FrameworkElement root = null)
    {
        if ((root ??= source.RootVisual as FrameworkElement) is null)
        {
            return;
        }

        if (root.FindDescendant<Border>(i => i.Name == "DropShadowBorder") is { } drop)
        {
            AcrylicizePopupInternal(popup, drop, source, root);
        }
        else if (root.FindDescendant<FrameworkElement>()?
                     .FindDescendant<FrameworkElement>()?
                     .FindDescendant<Decorator>() is { } callout &&
            // Pointing popup (e.g. CodeLens references popup)
            callout.GetType().FullName == "Microsoft.VisualStudio.Language.Intellisense.CodeLensCalloutBorder")
        {
            callout.SetResourceReference(Panel.BackgroundProperty, SolidBackgroundFillTertiaryKey);
        }
    }

    private void AcrylicizePopupInternal(Popup popup, Border drop, HwndSource source, FrameworkElement root)
    {
        (root.FindDescendant<ToolTip>() is ToolTip tip ? // Tool tips use themselves for margins
            tip : drop as FrameworkElement).Margin = default;

        // Check for popup margin accountment
        if (popup.HorizontalOffset == -12)
        {
            RemovePopupOffset(popup);
        }

        drop.CornerRadius = new(uniformRadius: 7);
        drop.SetResourceReference(Border.BackgroundProperty, PopupBackgroundKey);
        drop.SetResourceReference(Border.BorderBrushProperty, PopupBorderKey);

        WindowHelper.SetCornerPreference(source.Handle, CornerPreference.Round); // Add shadow and corners
        WindowHelper.EnableWindowBorder(source.Handle, enable: false); // Remove border (we have our own)

        // Current popup background color
        var color = (drop.Background as SolidColorBrush)?.Color ?? default;

        WindowHelper.EnableWindowBlur(
            source.Handle,
            fallback: _resource.VisualStudioTheme == Theme.Dark && color.IsGray() ?
                System.Drawing.Color.FromArgb(0x2C, 0x2C, 0x2C) : // Dark mode acrylic fallback
                System.Drawing.Color.FromArgb(color.R, color.G, color.B),
            enable: true);
    }

    public void RemovePopupOffset(Popup popup)
    {
        if (popup.Placement == PlacementMode.Custom)
        {
            const int LeftAlignedPlacementIndex = 1;

            var callback = popup.CustomPopupPlacementCallback;
            var originalXOffset = popup.HorizontalOffset;

            popup.CustomPopupPlacementCallback = (popupSize, targetSize, offset) =>
            {
                var placement = callback(popupSize, targetSize, offset).ToList();

                if (placement.Count >= LeftAlignedPlacementIndex + 1)
                {
                    var leftAlignedPlacement = placement[LeftAlignedPlacementIndex];

                    placement.Insert(
                        LeftAlignedPlacementIndex,
                        leftAlignedPlacement with
                        {
                            Point = new(
                                leftAlignedPlacement.Point.X + (originalXOffset * 2), // idk why 2x
                                leftAlignedPlacement.Point.Y)
                        });

                    placement.RemoveAt(LeftAlignedPlacementIndex + 1);
                }

                return [.. placement];
            };
        }

        popup.HorizontalOffset = 0;
        popup.UpdateLayout();
    }

    #region Dispose

    private bool _disposed;

    void IDisposable.Dispose()
    {
        if (!_disposed)
        {
            _sourceDetour?.Dispose();
            _disposed = true;
        }
    }

    #endregion
}
