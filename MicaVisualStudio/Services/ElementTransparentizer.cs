using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using MonoMod.RuntimeDetour;
using MicaVisualStudio.Interop;
using MicaVisualStudio.Contracts;
using MicaVisualStudio.Extensions;
using Expression = System.Linq.Expressions.Expression;
using IResourceManager = MicaVisualStudio.Contracts.IResourceManager;

namespace MicaVisualStudio.Services;

public class ElementTransparentizer : IElementTransparentizer, IDisposable
{
    private const string MultiViewHostTypeName = "Microsoft.VisualStudio.Editor.Implementation.WpfMultiViewHost";

    #region Keys

    private const string LayeredBrushKey = "VsBrush.SolidBackgroundFillTertiaryLayered";

    private static readonly ThemeResourceKey SolidBackgroundFillTertiaryKey =
        new(category: new("73708ded-2d56-4aad-b8eb-73b20d3f4bff"), name: "SolidBackgroundFillTertiary", ThemeResourceKeyType.BackgroundBrush);

    private static readonly ThemeResourceKey TextFillPrimaryKey =
    new(category: new("73708ded-2d56-4aad-b8eb-73b20d3f4bff"), name: "TextFillPrimary", ThemeResourceKeyType.BackgroundBrush);

    private static readonly ThemeResourceKey TextOnAccentFillPrimaryKey =
        new(category: new("73708ded-2d56-4aad-b8eb-73b20d3f4bff"), name: "TextOnAccentFillPrimary", ThemeResourceKeyType.BackgroundBrush);

    private readonly ThemeResourceKey ScrollBarBackgroundKey =
        new(category: new("{624ed9c3-bdfd-41fa-96c3-7c824ea32e3d}"), name: "ScrollBarBackground", ThemeResourceKeyType.BackgroundBrush);

    #endregion

    private readonly ILogger _logger;
    private readonly IGeneral _general;
    private readonly IWindowManager _window;
    private readonly IResourceManager _resource;

    private readonly Func<object, bool> IsDockTarget;
    private readonly Func<IVsWindowFrame, DependencyObject> get_WindowFrame_FrameView;

    private readonly DependencyProperty View_ContentProperty, View_IsActiveProperty;

    private readonly ILHook _visualDetour;

    private readonly bool _layeredWindows;

    public ElementTransparentizer(
        ILogger logger,
        IGeneral general,
        IWindowManager window,
        IResourceManager resource)
    {
        _logger = logger;
        _general = general;
        _window = window;
        _resource = resource;

        // Add layered brush
        resource.CustomResources.Add(LayeredBrushKey, new(SolidBackgroundFillTertiaryKey, (t, c) =>
            new SolidColorBrush(c with { A = 0xFF / 2 /* 50% opacity */ })));
        resource.AddCustomResources();

        // Check if enabled
        if (!_general.ForceTransparency)
        {
            return;
        }
        _layeredWindows = general.LayeredWindows;

        // Generate Visual.AddVisualChild detour
        _visualDetour = typeof(Visual).GetMethod("AddVisualChild", BindingFlags.Instance | BindingFlags.NonPublic)
                                      .CreateDetour<Visual, Visual>(AddVisualChild);

        // Generate function for WindowFrame.FrameView.get
        get_WindowFrame_FrameView = Type
            .GetType("Microsoft.VisualStudio.Platform.WindowManagement.WindowFrame, Microsoft.VisualStudio.Platform.WindowManagement")
            .GetProperty("FrameView")
            .CreateGetter<IVsWindowFrame, DependencyObject>();

        // Generate DockTarget type check
        var parameter = Expression.Parameter(typeof(object));
        var dockType = Type.GetType("Microsoft.VisualStudio.PlatformUI.Shell.Controls.DockTarget, Microsoft.VisualStudio.Shell.ViewManager");
        IsDockTarget = parameter.TypeIs(dockType)
                                .Compile<object, bool>(parameter);

        // Get View dependency properties
        var viewType = Type.GetType("Microsoft.VisualStudio.PlatformUI.Shell.View, Microsoft.VisualStudio.Shell.ViewManager");
        View_ContentProperty = viewType.GetDependencyProperty("Content");
        View_IsActiveProperty = viewType.GetDependencyProperty("IsActive");

        // Listen to window creation
        window.FrameIsOnScreenChanged += OnFrameIsOnScreenChanged;
        window.ActiveFrameChanged += OnActiveFrameChanged;
        window.WindowOpened += OnWindowOpened;

        // Listen to dependency object events
        EventManager.RegisterClassHandler(
            dockType,
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((s, e) => StyleElementTree(s as Border)));

        if (AppDomain.CurrentDomain.GetAssemblies()
                                   .FirstOrDefault(i => i.GetName().Name == "Microsoft.VisualStudio.Editor.Implementation")?
                                   .GetType(MultiViewHostTypeName) is Type hostType)
        {
            EventManager.RegisterClassHandler(
                hostType,
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler((s, e) => StyleElementTree(s as DockPanel)));
        }

        // Apply to all visible elements
        StyleAllWindows();
        StyleAllWindowFrames();
    }

    private void AddVisualChild(Visual instance, Visual child)
    {
        try
        {
        if (instance is ContentControl or ContentPresenter or Decorator or Panel && // Skip other types
            instance is FrameworkElement element && GetIsTracked(element))
        {
            StyleElementTree(element);
        }
    }
        catch (Exception ex)
        {
            _logger.Output(ex);
        }
    }

    private void OnFrameIsOnScreenChanged(IVsWindowFrame frame, bool isOnScreen)
    {
        if (isOnScreen)
        {
            StyleWindowFrame(frame);
        }
    }

    private void OnActiveFrameChanged(IVsWindowFrame frame, IVsWindowFrame newFrame)
    {
        if (newFrame is not null)
        {
            StyleWindowFrame(newFrame);
        }
    }

    private void OnWindowOpened(object sender, WindowActionEventArgs args)
    {
        if (args.Window is not null)
        {
            StyleWindow(args.Window);
        }
    }

    public void StyleAllWindows()
    {
        foreach (var window in _window.Windows.Values)
        {
            if (window is not null)
            {
                StyleWindow(window);
            }
        }
    }

    public void StyleWindow(Window window) =>
        StyleElementTree(window);

    public void StyleAllWindowFrames()
    {
        foreach (var frame in _window.WindowFrames)
        {
            StyleWindowFrame(frame);
        }
    }

    public void StyleWindowFrame(IVsWindowFrame frame)
    {
        try
        {
        if (get_WindowFrame_FrameView(frame) is not DependencyObject view)
        {
            return;
        }

        if (view.GetValue(View_ContentProperty) is not Grid host)
        {
            WeakReference<IVsWindowFrame> weakFrame = new(frame);

            view.AddWeakOneTimePropertyChangeHandler(View_ContentProperty, (s, e) =>
            {
                if (weakFrame.TryGetTarget(out IVsWindowFrame frame))
                {
                    StyleWindowFrame(frame);
                }
            });
        }
        else if (host.FindAncestor<DependencyObject>(i => i.GetVisualOrLogicalParent(), IsDockTarget) is Border dock)
        {
            StyleElementTree(dock);
        }
    }
        catch (Exception ex)
        {
            _logger.Output(ex);
        }
    }

    public void StyleElementTree(FrameworkElement element)
    {
        try
        {
        StyleTree(element.FindDescendants<FrameworkElement>().Append(element));
        }
        catch (Exception ex)
        {
            _logger.Output(ex);
        }
    }

    public void StyleTree(IEnumerable<FrameworkElement> tree)
    {
        foreach (var element in tree)
        {
            switch (element)
            {
                case ToolBar bar:
                    StyleToolBar(bar);
                    break;

                case TabItem tab:
                    StyleTabItem(tab);
                    break;

                case HwndHost host:
                    StyleHwndHost(host);
                    break;

                case Control control:
                    StyleControl(control);
                    break;

                case Panel panel:
                    StylePanel(panel);
                    break;

                case Border border:
                    if (IsDockTarget(border))
                    {
                        StyleDockTarget(border);
                    }
                    else
                    {
                        StyleBorder(border);
                    }
                    break;
            }

            if (element is ContentControl or ContentPresenter or Decorator or Panel)
            {
                // Track visual children
                SetIsTracked(element, value: true);
            }
        }
    }

    public void StyleDockTarget(Border dock)
    {
        // DockTarget is used in multiple ways

        // If name is ViewFrameTarget, it's the background behind a single floating tool window
        if (dock.Name == "ViewFrameTarget")
        {
            Layer(dock);
        }
        // Otherwise, make it transparent to remove the smoke layer behind tabs
        else
        {
            dock.Background = Brushes.Transparent;
        }
    }

    public void StyleToolBar(ToolBar bar)
    {
        if (bar.GetVisualOrLogicalParent() is not ToolBarTray { Name: "TopDockTray" })
        {
            bar.Background = bar.BorderBrush = Brushes.Transparent;
            (bar.Parent as ToolBarTray)?.Background = Brushes.Transparent;
        }
    }

    public void StyleTabItem(TabItem tab)
    {
        if (tab.DataContext is not DependencyObject view ||
            // Window frame, tab strip
            tab.GetVisualOrLogicalParent() is not FrameworkElement { Name: "PART_TabPanel" })
        {
            return;
        }

        tab.Background = Brushes.Transparent;
        tab.SetResourceReference(
            Control.ForegroundProperty,
            tab.IsSelected && (bool)view.GetValue(View_IsActiveProperty) ? TextOnAccentFillPrimaryKey : TextFillPrimaryKey);

        if (GetIsTracked(tab))
        {
            return;
        }

        SetIsTracked(tab, value: true);
        WeakReference<TabItem> weakTab = new(tab);

        tab.AddWeakPropertyChangeHandler(TabItem.IsSelectedProperty, (s, e) =>
        {
            if (s is TabItem tab)
            {
                StyleTabItem(tab);
            }
        });
        view.AddPropertyChangeHandler(View_IsActiveProperty, (s, e) =>
        {
            if (weakTab.TryGetTarget(out TabItem tab))
            {
                StyleTabItem(tab);
            }
        });
    }

    public void StyleHwndHost(HwndHost host)
    {
        if (!_layeredWindows)
        {
            return;
        }

        var handle = host.Handle;

        if (handle == IntPtr.Zero)
        {
            return;
        }

        var sources = PresentationSource.CurrentSources.OfType<HwndSource>()
                                                       .Select(i => i.Handle)
                                                       .ToArray();

        if (sources.Contains(handle))
        {
            return;
        }

        var children = WindowHelper.GetChildren(handle);

        if (!sources.Any(children.Contains))
        {
            WindowHelper.MakeLayered(handle);
        }
    }

    public void StyleControl(Control control)
    {
        switch (control.Name)
        {
            // Warning dialog, footer
            case "OKButton"
            when control is Button &&
                PresentationSource.FromVisual(control)?.RootVisual is DialogWindowBase &&
                control.FindAncestor<FrameworkElement>()?.FindAncestor<Border>() is Border footer:
                Layer(footer);
                return;

            // Host of WpfTextView I guess
            case "WpfTextViewHost":
                control.Resources["outlining.chevron.expanded.background"] =
                control.Resources["outlining.chevron.collapsed.background"] = Brushes.Transparent;

                if (control.FindDescendant<FrameworkElement>()?
                           .FindDescendant<Grid>() is { } hostGrid)
                {
                    hostGrid.Background = Brushes.Transparent;
                }
                return;

            // Editor, output, etc. text
            case "WpfTextView" when control is ContentControl:
                control.Background = Brushes.Transparent;
                control.FindDescendant<Canvas>()?.Background = Brushes.Transparent;
                return;

            // Packaged app configurations list
            case "PackageConfigurationsList" when control is DataGrid grid:
                grid.Background = grid.RowBackground = Brushes.Transparent;
                TransparentizeStyle(grid.CellStyle);
                return;

            // VSIX manfiest editor
            case "VsixEditorControl":
                control.Background = Brushes.Transparent;
                foreach (var tab in control.FindDescendants<TabItem>())
                {
                    tab.Background = Brushes.Transparent;
                }
                return;

            // Window frame, title bar
            case "PART_Header":
                control.Background = Brushes.Transparent;
                return;

            // Copilot window, message box
            case "ChatPrompt":
                control.Background = Brushes.Transparent;
                return;

            // Editor window, map scroll bar buttons
            case "UpButton" or "DownButton"
            when control is RepeatButton && control.FindDescendant<Border>() is { Name: "Border" } border:
                border.Background = Brushes.Transparent;
                return;

            // AppxManifest editor
            case "MainTabControl" when control.GetVisualOrLogicalParent()?
                                              .GetVisualOrLogicalParent() is Grid { Name: "LayoutRoot" } root:
                control.Background = Brushes.Transparent;
                root.Background = Brushes.Transparent;
                return;

            // Resource editor
            case "_resourceView"
            when control is ContentControl { Content: DockPanel panel }:
                panel.Background = Brushes.Transparent;
                return;

            #region Git Windows

            case "gitWindowView" or // Git changes window
                "focusedWindowView" or // Git repository window
                "detailsView" or // Git commit details
                "focusedDetailsContainer" or // Git commit details container
                "teamExplorerFrame" or // Team explorer window
                "createPullRequestView": // New PR window
            GitWindow:
                control.Background = Brushes.Transparent;
                StyleTree(control.LogicalDescendants<FrameworkElement>());
                return;

            // Commit history
            case "historyView" when control.GetVisualOrLogicalParent()?
                                           .GetVisualOrLogicalParent()?
                                           .GetVisualOrLogicalParent() is Border history:
                history.Background = Brushes.Transparent;
                goto GitWindow;

            // Git changes, command buttons
            case "gitAction" or "detailsView"
            when control.TryFindResource("TESectionCommandButtonStyle") is Style style:
                TransparentizeStyle(style);
                return;

            // Smth in a git window
            case "thisPageControl":
                control.Background = Brushes.Transparent;
                return;

            // Team explorer, project selector
            case "navControl":
                control.Background = Brushes.Transparent;
                return;

            // Git branch selector
            case "branchesList" when control.GetVisualOrLogicalParent()?
                                            .GetVisualOrLogicalParent()?
                                            .GetVisualOrLogicalParent()?
                                            .GetVisualOrLogicalParent() is Control branches:
                branches.Background = Brushes.Transparent;
                return;

            // Commit history list
            case "historyListView" when control is ListView { View: GridView { ColumnHeaderContainerStyle: { } style } grid }:
                grid.ColumnHeaderContainerStyle = SubclassStyle(style);

                foreach (var c in grid.Columns.Select(i => i.Header).OfType<Control>())
                {
                    c.ApplyTemplate();
                    c.FindDescendant<Border>(i => i.Name == "HeaderBorder")?.BorderBrush = Brushes.Transparent;
                }
                return;

            // Commit diff info
            case "pageContentViewer" when control.GetVisualOrLogicalParent()?
                                                 .GetVisualOrLogicalParent() is Border viewer:
                viewer.Background = Brushes.Transparent;
                return;

            // Commit diff presenter dock buttons
            case "dockToBottomButton" or
                "dockToRightButton" or
                "undockButton" or
                "maximizeMinimizeButton" or
                "closeButton"
            when control is Button { Style: { } style } button:
                button.Style = SubclassStyle(style);
                return;

            // Git push, pull etc. buttons
            case "actionButton" or
                "fetchButton" or
                "pullButton" or
                "pushButton" or
                "syncButton" or
                "additionalOperationsButton"
            when control is Button { Style: { } style } button:
                button.Style = SubclassStyle(style);
                return;

            // Git changes...
            case "statusControl" or // Actions/tool bar
                "inactiveRepoContent" or // Create repo
                "sectionContainer" or // Branches and tags
                "amendCheckBox": // Checkbox... for amending...
                control.Background = Brushes.Transparent;
                return;

            #endregion
        }

        switch (control.GetType().FullName)
        {
            // AppxManifest editor
            case "Microsoft.VisualStudio.AppxManifestDesigner.Designer.ManifestDesignerUserControlProxy":
            case "Microsoft.VisualStudio.AppxManifestDesigner.Designer.ManifestDesignerUserControl":
                control.Background = Brushes.Transparent;
                break;

            // Resource editor
            case "Microsoft.VisualStudio.ResourceExplorer.UI.ResourceGroupEditorControl":
                control.Background = Brushes.Transparent;
                break;
        }
    }

    public void StylePanel(Panel panel)
    {
        switch (panel.Name)
        {
            // Commit diff
            case "detailsViewMainGrid" when panel.GetVisualOrLogicalParent()?
                                                 .GetVisualOrLogicalParent() is Border details:
                details.Background = Brushes.Transparent;
                return;

            // Status bar
            case "StatusBarPanel":
                return;

            // Commit history, toolbar container
            case "toolbarGrid":
                panel.Background = Brushes.Transparent;
                return;
        }

        switch (panel.GetType().FullName)
        {
            // Editor window, root
            case MultiViewHostTypeName:
                panel.Resources[ScrollBarBackgroundKey] = Brushes.Transparent;
                break;

            // Editor window, bottom container
            case "Microsoft.VisualStudio.Text.Utilities.ContainerMargin"
            when !panel.FindDescendants<Panel>().Any(i => i.GetType().FullName == "Microsoft.VisualStudio.Text.Utilities.ContainerMargin"):
                Layer(panel);
                break;

            // Editor window, left side icon container
            case "Microsoft.VisualStudio.Text.Editor.Implementation.GlyphMarginGrid"
            when panel.Background is SolidColorBrush solid && solid.Color != Brushes.Transparent.Color:
                Layer(panel);
                break;

            // Scroll bar intersection
            case "Microsoft.VisualStudio.Editor.Implementation.BottomRightCornerSpacerMargin":
                panel.Background = Brushes.Transparent;
                break;

            // Editor window, collapsed item container
            case "Microsoft.VisualStudio.Text.Editor.Implementation.AdornmentLayer":
                foreach (var rectangle in panel.FindDescendants<Rectangle>())
                {
                    // Check for selected line highlight properties
                    if (rectangle.RadiusX > 0 || rectangle.StrokeThickness <= 0)
                    {
                        rectangle.SetResourceReference(Shape.FillProperty, LayeredBrushKey);
                    }
                }
                break;

            // Editor window, map scroll bar
            case "Microsoft.VisualStudio.Text.OverviewMargin.Implementation.OverviewElement":
                panel.Background = Brushes.Transparent;
                break;

            // Commit diff view
            case "Microsoft.VisualStudio.Differencing.Package.DiffControl":
                panel.Background = Brushes.Transparent;
                break;
        }
    }

    public void StyleBorder(Border border)
    {
        switch (border.Name)
        {
            // Window frame, content area
            case "PART_ContentPanel":
                Layer(border);
                return;

            // Window frame, body
            case "ToolWindowBorder":
                border.Background = Brushes.Transparent;
                return;

            // Start window, footer
            case "FooterBorder":
                Layer(border);
                return;

            // Git window, section header
            case "borderHeader" when border is { Style: { } style }:
                border.Style = SubclassStyle(style);
                return;
        }

        switch (border.GetType().FullName)
        {
            // Output window, root
            case "Microsoft.VisualStudio.PlatformUI.OutputWindow":
                border.Background = Brushes.Transparent;
                break;

            // Editor window, file errors container
            case "Microsoft.VisualStudio.UI.Text.Wpf.FileHealthIndicator.Implementation.FileHealthIndicatorMargin":
                border.Background = Brushes.Transparent;
                break;

            // Commit diff view, tool bar
            case "Microsoft.VisualStudio.Differencing.Package.DiffControlToolbar":
                border.Background = Brushes.Transparent;
                break;

            // List window, root
            case "Microsoft.VisualStudio.ErrorListPkg.TableControlToolWindowPaneBase+ContentWrapper":
                border.Resources.MergedDictionaries.Clear();
                break;
        }
    }

    public void Layer(Control control) =>
        control.SetResourceReference(Control.BackgroundProperty, LayeredBrushKey);

    public void Layer(Panel panel) =>
        panel.SetResourceReference(Panel.BackgroundProperty, LayeredBrushKey);

    public void Layer(Border border) =>
        border.SetResourceReference(Border.BackgroundProperty, LayeredBrushKey);

    public void TransparentizeStyle(Style style)
    {
        if (style is not { Setters.IsSealed: false, Triggers.IsSealed: false })
        {
            return;
        }

        Setter background = new(Control.BackgroundProperty, Brushes.Transparent);
        Setter border = new(Control.BorderBrushProperty, Brushes.Transparent);

        style.Setters.Add(background);
        style.Setters.Add(border);

        style.Triggers.Add(new Trigger
        {
            Property = UIElement.IsEnabledProperty,
            Value = false,
            Setters = { border, background }
        });
    }

    public Style SubclassStyle(Style style)
    {
        if (style.Setters.FirstOrDefault(i => (i as Setter)?.Property == IsTrackedProperty) is not null)
        {
            // Style already subclassed
            return style;
        }

        Style newStyle = new(style.TargetType, style)
        {
            Setters = { new Setter(IsTrackedProperty, value: false) }
        };
        TransparentizeStyle(newStyle);
        return newStyle;
    }

    #region IsTrackedProperty

    /// <summary>
    /// Gets the value of the <see cref="IsTrackedProperty"/> attached property from a given <see cref="FrameworkElement"/>.
    /// </summary>
    /// <param name="target">The <see cref="FrameworkElement"/> from which to read the property value.</param>
    /// <returns>The value of the <see cref="IsTrackedProperty"/> attached property.</returns>
    public static bool GetIsTracked(FrameworkElement target) =>
        (bool)target.GetValue(IsTrackedProperty);

    /// <summary>
    /// Sets the value of the <see cref="IsTrackedProperty"/> attached property from a given <see cref="FrameworkElement"/>.
    /// </summary>
    /// <param name="target">The <see cref="FrameworkElement"/> on which to set the attached property.</param>
    /// <param name="value">The property value to set.</param>
    public static void SetIsTracked(FrameworkElement target, bool value) =>
        target.SetValue(IsTrackedProperty, value);

    /// <summary>
    /// Identifies the MicaVisualStudio.Services.ElementTransparentizer.IsTracked dependency property.
    /// </summary>
    public static readonly DependencyProperty IsTrackedProperty =
        DependencyProperty.RegisterAttached("IsTracked", typeof(bool), typeof(ElementTransparentizer), new(defaultValue: false));

    #endregion

    #region Dispose

    private bool _disposed;

    void IDisposable.Dispose()
    {
        if (!_disposed)
        {
            _visualDetour?.Dispose();

            _window.FrameIsOnScreenChanged -= OnFrameIsOnScreenChanged;
            _window.ActiveFrameChanged -= OnActiveFrameChanged;

            _window.WindowOpened -= OnWindowOpened;

            _disposed = true;
        }
    }

    #endregion
}
