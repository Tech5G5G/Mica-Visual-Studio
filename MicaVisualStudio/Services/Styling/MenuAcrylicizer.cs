using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.PlatformUI;
using MonoMod.RuntimeDetour;
using MicaVisualStudio.Options;
using MicaVisualStudio.Interop;
using MicaVisualStudio.Contracts;
using MicaVisualStudio.Extensions;

namespace MicaVisualStudio.Services.Styling;

public class MenuAcrylicizer : IMenuAcrylicizer, IDisposable
{
    private const string PopupBackgroundKey = "VsBrush.PopupBackgroundLayered",
                         PopupBorderKey = "VsBrush.PopupBorderOnAcrylic";

    private static readonly ThemeResourceKey SolidBackgroundFillTertiaryKey =
        new(category: new("73708ded-2d56-4aad-b8eb-73b20d3f4bff"), name: "SolidBackgroundFillTertiary", ThemeResourceKeyType.BackgroundBrush);

    private static MenuAcrylicizer s_acrylicizer;

    private readonly ILogger _logger;
    private readonly IGeneral _general;
    private readonly IResourceManager _resource;

    private readonly CancellationTokenSource _source = new();

    private readonly bool _acrylicMenus;

    private ILHook _sourceHook;

    public MenuAcrylicizer(ILogger logger, IGeneral general, IResourceManager resource)
    {
        _logger = logger;
        _general = general;
        _resource = resource;

        // Add brushes
        resource.CustomResources.Add(PopupBackgroundKey, new(SolidBackgroundFillTertiaryKey, (t, c) =>
            new SolidColorBrush(t == Theme.Light || !c.IsGray ?
                c with { A = 0xFF / 4 /* 25% opacity */ } :
                Color.FromArgb(0x01, 0x00, 0x00, 0x00)))); // Full acrylic experience for those who can handle it

        resource.CustomResources.Add(PopupBorderKey, new(baseResourceKey: null, (t, _) =>
            new SolidColorBrush(t == Theme.Light ? Color.FromArgb(0x20, 0x000, 0x00, 0x00) : Color.FromArgb(0x55, 0x000, 0x00, 0x00))));

        resource.AddCustomResources();

        // Set flag
        _acrylicMenus = general.AcrylicMenus;

        // Generate HwndSouce.RootVisual.set hook on background thread
        s_acrylicizer = this;
        Task.Run(CreateHook, _source.Token).FireAndForget(logOnFailure: true);
    }

    private void CreateHook()
    {
        var token = _source.Token;
        if (token.IsCancellationRequested)
        {
            // Already disposed
            return;
        }

        var hook = typeof(HwndSource).GetProperty("RootVisual")
                                     .SetMethod
                                     .CreateHook<HwndSource, Visual>(RootVisualChanged);

        if (token.IsCancellationRequested)
        {
            hook.Dispose();
            return;
        }

        // Publish hook
        Interlocked.Exchange(ref _sourceHook, hook)?.Dispose();

        // Check if we disposed while publishing hook
        if (token.IsCancellationRequested)
        {
            Interlocked.Exchange(ref _sourceHook, null)?.Dispose();
            return;
        }
    }

    private static void RootVisualChanged(HwndSource instance, Visual value)
    {
        if (s_acrylicizer is null)
        {
            return;
        }

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
                s_acrylicizer.AcrylicizePopup(popup, instance, root);
            }
            else if (value is not Window) // Avoid already handled values
            {
                instance.CompositionTarget.BackgroundColor = Colors.Transparent;
            }
        }
        catch (Exception ex)
        {
            s_acrylicizer._logger.Output(ex);
        }
    }

    public void AcrylicizePopup(Popup popup, HwndSource source, FrameworkElement root = null)
    {
        if ((root ??= source.RootVisual as FrameworkElement) is null)
        {
            return;
        }

        if (_acrylicMenus && root.FindDescendant<Border>(b => b.Name == "ContentBorder") is { } content)
        {
            content.Background = Brushes.Transparent;
            content.BorderBrush = Brushes.Transparent;
        }

        if (_acrylicMenus && root.FindDescendant<Border>(b => b.Name == "DropShadowBorder") is { } drop)
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
        if (root.FindDescendant<ToolTip>() is ToolTip tip)
        {
            // Tool tips use themselves for margins
            tip.Margin = default;
        }
        else
        {
            drop.Margin = default;
            (drop.GetVisualOrLogicalParent() as Grid)?.Margin = default;
        }

        // Check for popup margin accountment
        if (popup.HorizontalOffset == -12)
        {
            RemovePopupOffset(popup);
        }

        drop.CornerRadius = new(uniformRadius: 7);
        drop.SetResourceReference(Border.BackgroundProperty, PopupBackgroundKey);
        drop.SetResourceReference(Border.BorderBrushProperty, PopupBorderKey);

        PInvoke.SetCornerPreference(source.Handle, CornerPreference.Round); // Add shadow and corners
        PInvoke.EnableWindowBorder(source.Handle, enable: false); // Remove border (we have our own)

        // Current popup background color
        var color = (drop.Background as SolidColorBrush)?.Color ?? default;

        PInvoke.EnableWindowBlur(
            source.Handle,
            fallback: _resource.VisualStudioTheme == Theme.Dark && color.IsGray ?
                System.Drawing.Color.FromArgb(0x2C, 0x2C, 0x2C) : // Dark mode acrylic fallback
                System.Drawing.Color.FromArgb(color.R, color.G, color.B),
            enable: true);
    }

    public void RemovePopupOffset(Popup popup)
    {
        if (popup.Placement == PlacementMode.Custom)
        {
            popup.CustomPopupPlacementCallback = SubclassPopupPlacementCallback(
                popup.CustomPopupPlacementCallback,
                popup.HorizontalOffset);
        }

        popup.HorizontalOffset = 0;
        popup.UpdateLayout();
    }

    private CustomPopupPlacementCallback SubclassPopupPlacementCallback(CustomPopupPlacementCallback callback, double horizontalOffset)
    {
        const int LeftPlacementIndex = 1;

        return (popupSize, targetSize, offset) =>
        {
            var placements = callback(popupSize, targetSize, offset);

            if (placements.Length >= LeftPlacementIndex + 1)
            {
                var leftPlacement = placements[LeftPlacementIndex];
                placements[LeftPlacementIndex] = leftPlacement with
                {
                    Point = new(
                        leftPlacement.Point.X + (horizontalOffset * 2), // idk why 2x
                        leftPlacement.Point.Y)
                };
            }

            return placements;
        };
    }

    #region Dispose

    private bool _disposed;

    void IDisposable.Dispose()
    {
        if (!_disposed)
        {
            _source.Cancel();
            _source.Dispose();

            s_acrylicizer = null;
            Interlocked.Exchange(ref _sourceHook, null)?.Dispose();

            _disposed = true;
        }
    }

    #endregion
}
