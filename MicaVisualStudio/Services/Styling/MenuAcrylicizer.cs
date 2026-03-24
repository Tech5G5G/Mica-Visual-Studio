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
using MicaVisualStudio.Services.Resourcing;

namespace MicaVisualStudio.Services.Styling;

public sealed class MenuAcrylicizer : IMenuAcrylicizer, IDisposable
{
    private const string PopupBackgroundKey = "VsBrush.PopupBackgroundLayered",
                         PopupBorderKey = "VsBrush.PopupBorderOnAcrylic";

    private static MenuAcrylicizer s_acrylicizer;

    [ThreadStatic]
    private static Type t_calloutBorderType;

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
        resource.CustomResources.Add(PopupBackgroundKey, new(ThemeResourceKeys.SolidBackgroundFillTertiary, (t, c) =>
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
                // Check if owned by popup
                // Popups use separate element as root of HwndSource
                root.Parent is Popup popup)
            {
                s_acrylicizer.AcrylicizePopup(popup, instance, root);
            }
            // Avoid already handled values
            else if (value is not Window)
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
        if ((root ??= source.RootVisual as FrameworkElement) is not null && // Get root if not passed
            (!_acrylicMenus || !StylePopupTree(root, popup, source)) && // Acrylicize menu if available
            root.VisualChild()?
                .VisualChild()?
                .VisualChild<Decorator>() is { } callout) // Check for pointing popup element
        {
            HandleCalloutBorder(callout);
        }
    }

    private bool StylePopupTree(FrameworkElement root, Popup popup, HwndSource source)
    {
        var found = false;

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; ++i)
        {
            if (VisualTreeHelper.GetChild(root, i) is not FrameworkElement child)
            {
                continue;
            }

            if (child is ToolTip tip)
            {
                // Tool tips use themselves for shadow padding
                tip.Margin = default;
            }
            else if (child is Border { Name: { } name } border)
            {
                if (name == "ContentBorder")
                {
                    border.Background = Brushes.Transparent;
                    border.BorderBrush = Brushes.Transparent;
                }
                else if (name == "DropShadowBorder")
                {
                    HandlePopupBackground(popup, border, source);
                    found = true;
                }
            }

            if (StylePopupTree(child, popup, source))
            {
                found = true;
            }
        }

        return found;
    }

    private void HandlePopupBackground(Popup popup, Border drop, HwndSource source)
    {
        // Remove shadow padding
        drop.Margin = default;
        (drop.GetVisualOrLogicalParent() as Grid)?.Margin = default;

        // Check for popup margin accountment
        const int MarginAccountment = -12;
        if (popup.HorizontalOffset == MarginAccountment)
        {
            RemovePopupOffset(popup);
        }

        // Update corner radii to match those of DWM
        drop.CornerRadius = new(uniformRadius: 7); // 7 instead of 8 because DWM rounds corners
                                                   // differently than WPF (apparently)
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

    private void HandleCalloutBorder(Decorator callout)
    {
        var type = callout.GetType();
        if (t_calloutBorderType is null)
        {
            // Pointing popup (e.g. CodeLens references popup)
            if (type.FullName != "Microsoft.VisualStudio.Language.Intellisense.CodeLensCalloutBorder")
            {
                return;
            }

            t_calloutBorderType = type;
        }
        else if (t_calloutBorderType != type)
        {
            return;
        }

        callout.SetResourceReference(Panel.BackgroundProperty, ThemeResourceKeys.SolidBackgroundFillTertiary);
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
