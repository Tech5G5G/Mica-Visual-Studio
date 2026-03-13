using System;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Expressions;
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
using MicaVisualStudio.Options;
using MicaVisualStudio.Contracts;
using MicaVisualStudio.Extensions;
using MicaVisualStudio.Services.Windowing;
using Expression = System.Linq.Expressions.Expression;
using IResourceManager = MicaVisualStudio.Contracts.IResourceManager;

namespace MicaVisualStudio.Services.Styling;

public class ElementTransparentizer : IElementTransparentizer, IDisposable
{
    private const string DocOutlineWindowClassName = "VsDocOutlineTool",
                         MultiViewHostTypeName = "Microsoft.VisualStudio.Editor.Implementation.WpfMultiViewHost";

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

    private static ElementTransparentizer s_transparentizer;

    private readonly ILogger _logger;
    private readonly IGeneral _general;
    private readonly IWindowManager _window;
    private readonly IResourceManager _resource;

    private readonly CancellationTokenSource _source = new();

    private readonly WinEventHook _eventHook;

    private readonly Func<object, bool> IsDockTarget;
    private readonly Func<IVsWindowFrame, DependencyObject> get_WindowFrame_FrameView;

    private readonly DependencyProperty View_ContentProperty, View_IsActiveProperty;

    private readonly bool _layeredWindows;

    private ILHook _visualHook;

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
        resource.CustomResources.Add(LayeredBrushKey, new(SolidBackgroundFillTertiaryKey, (_, c) =>
            new SolidColorBrush(c with { A = 0xFF / 2 /* 50% opacity */ })));
        resource.AddCustomResources();

        // Check if enabled
        _layeredWindows = general.LayeredWindows;
        if (!general.ForceTransparency)
        {
            return;
        }

        // Generate Visual.AddVisualChild detour on background thread
        s_transparentizer = this;
        Task.Run(CreateHook, _source.Token).FireAndForget(logOnFailure: true);

        // Generate functions and get dependency properties
        var dockType = Type.GetType("Microsoft.VisualStudio.PlatformUI.Shell.Controls.DockTarget, Microsoft.VisualStudio.Shell.ViewManager");
        PerformReflection(dockType, out get_WindowFrame_FrameView, out IsDockTarget, out View_ContentProperty, out View_IsActiveProperty);

        // Listen to dependency object events
        RegisterClassHandlers(dockType);

        // Listen to window creation
        window.FrameIsOnScreenChanged += OnFrameIsOnScreenChanged;
        window.ActiveFrameChanged += OnActiveFrameChanged;
        window.WindowOpened += OnWindowOpened;

        // Create event hook for window reparent
        using var process = Process.GetCurrentProcess();
        _eventHook = new(Event.ParentChange, EventFlags.OutOfContext, process.Id);
        _eventHook.EventOccurred += OnEventOccurred;

        // Apply to all visible elements
        StyleAllWindows();
        StyleAllWindowFrames();
    }

    private void PerformReflection(
        Type dockType,
        out Func<IVsWindowFrame, DependencyObject> getFrameView,
        out Func<object, bool> isDockTarget,
        out DependencyProperty contentProperty,
        out DependencyProperty isActiveProperty)
    {
        // Generate function for WindowFrame.FrameView.get
        getFrameView = Type.GetType("Microsoft.VisualStudio.Platform.WindowManagement.WindowFrame, Microsoft.VisualStudio.Platform.WindowManagement")
                           .GetProperty("FrameView")
                           .CreateGetter<IVsWindowFrame, DependencyObject>();

        // Get View dependency properties
        var viewType = Type.GetType("Microsoft.VisualStudio.PlatformUI.Shell.View, Microsoft.VisualStudio.Shell.ViewManager");
        contentProperty = viewType.GetDependencyProperty("Content");
        isActiveProperty = viewType.GetDependencyProperty("IsActive");

        // Generate DockTarget type check
        var parameter = Expression.Parameter(typeof(object));
        isDockTarget = parameter.TypeIs(dockType)
                                .Compile<object, bool>(parameter);
    }

    private void RegisterClassHandlers(Type dockType)
    {
        EventManager.RegisterClassHandler(
            dockType,
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(static (s, _) => UseTransparentizer(t => t.StyleElementTree(s as Border))));

        if (AppDomain.CurrentDomain.GetAssemblies()
                                   .FirstOrDefault(a => a.GetName().Name == "Microsoft.VisualStudio.Editor.Implementation")?
                                   .GetType(MultiViewHostTypeName) is Type hostType)
        {
            EventManager.RegisterClassHandler(
                hostType,
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(static (s, _) => UseTransparentizer(t => t.StyleElementTree(s as DockPanel))));
        }
    }

    private void CreateHook()
    {
        var token = _source.Token;
        if (token.IsCancellationRequested)
        {
            // Already disposed
            return;
        }

        var hook = typeof(Visual).GetMethod("AddVisualChild", BindingFlags.Instance | BindingFlags.NonPublic)
                                 .CreateHook<Visual, Visual>(AddVisualChild);

        if (token.IsCancellationRequested)
        {
            hook.Dispose();
            return;
        }

        // Publish hook
        Interlocked.Exchange(ref _visualHook, hook)?.Dispose();

        // Check if we disposed while publishing hook
        if (token.IsCancellationRequested)
        {
            Interlocked.Exchange(ref _visualHook, null)?.Dispose();
            return;
        }
    }

    private static void AddVisualChild(Visual instance, Visual child)
    {
        UseTransparentizer(t =>
        {
            if (instance is ContentControl or ContentPresenter or Decorator or Panel && // Skip other types
                instance is FrameworkElement element && GetIsTracked(element))
            {
                t.StyleElementTree(element);
            }
        });
    }

    private void OnFrameIsOnScreenChanged(IVsWindowFrame frame, bool isOnScreen)
    {
        try
        {
            if (isOnScreen)
            {
                StyleWindowFrame(frame);
            }
        }
        catch (Exception ex)
        {
            _logger.Output(ex);
        }
    }

    private void OnActiveFrameChanged(IVsWindowFrame frame, IVsWindowFrame newFrame)
    {
        try
        {
            if (newFrame is not null)
            {
                StyleWindowFrame(newFrame);
            }
        }
        catch (Exception ex)
        {
            _logger.Output(ex);
        }
    }

    private void OnWindowOpened(object sender, WindowActionEventArgs e)
    {
        try
        {
            if (e.Window is not null)
            {
                StyleWindow(e.Window);
            }
        }
        catch (Exception ex)
        {
            _logger.Output(ex);
        }
    }

    private void OnEventOccurred(WinEventHook sender, EventOccuredEventArgs e)
    {
        if (PInvoke.GetClassName(PInvoke.GetOwner(e.WindowHandle)) == DocOutlineWindowClassName)
        {
            StyleHwnd(e.WindowHandle);
        }
    }

    public void StyleAllWindows()
    {
        foreach (var window in _window.Windows.Values)
        {
            if (window is not null)
            {
                try
                {
                    StyleWindow(window);
                }
                catch (Exception ex)
                {
                    _logger.Output(ex);
                }
            }
        }
    }

    public void StyleWindow(Window window)
    {
        StyleElementTree(window);
    }

    public void StyleAllWindowFrames()
    {
        foreach (var frame in _window.WindowFrames)
        {
            try
            {
                StyleWindowFrame(frame);
            }
            catch (Exception ex)
            {
                _logger.Output(ex);
            }
        }
    }

    public void StyleWindowFrame(IVsWindowFrame frame)
    {
        if (get_WindowFrame_FrameView(frame) is not DependencyObject view)
        {
            return;
        }

        if (view.GetValue(View_ContentProperty) is not Grid host)
        {
            WeakReference<IVsWindowFrame> weakFrame = new(frame);

            view.AddWeakOneTimePropertyChangeHandler(View_ContentProperty, (s, _) =>
            {
                if (weakFrame.TryGetTarget(out IVsWindowFrame frame))
                {
                    UseTransparentizer(t => t.StyleWindowFrame(frame));
                }
            });
        }
        else if (host.FindAncestor<DependencyObject>(o => o.GetVisualOrLogicalParent(), IsDockTarget) is Border dock)
        {
            StyleElementTree(dock);
        }
        else if (!host.IsLoaded)
        {
            host.AddWeakOneTimeHandler(FrameworkElement.LoadedEvent, static (s, _) =>
            {
                UseTransparentizer(t =>
                {
                    if ((s as Border)?.FindAncestor<DependencyObject>(o => o.GetVisualOrLogicalParent(), t.IsDockTarget) is Border dock)
                    {
                        t.StyleElementTree(dock);
                    }
                });
            });
        }
    }

    public void StyleElementTree(FrameworkElement element)
    {
        StyleTree(element.FindDescendants<FrameworkElement>().Append(element));
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

    public void StyleHwnd(nint handle)
    {
        if (HwndSource.FromHwnd(handle) is { RootVisual: FrameworkElement element })
        {
            StyleElementTree(element);
            return;
        }

        var children = PInvoke.GetChildren(handle);
        if (PInvoke.GetClassName(handle) == "Static" && children.Count() == 0)
        {
            return;
        }

        var layer = true;
        foreach (var child in children)
        {
            if (HwndSource.FromHwnd(child) is { RootVisual: FrameworkElement childElement })
            {
                StyleElementTree(childElement);
                layer = false;
            }
            else if (PInvoke.GetClassName(child) is
                DocOutlineWindowClassName or // Document Outline window
                "Chrome_WidgetWin_0") // Web view window
            {
                layer = false;
            }
        }

        if (layer)
        {
            PInvoke.AddLayeredAttributes(handle);
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

        tab.AddWeakPropertyChangeHandler(TabItem.IsSelectedProperty, static (s, _) =>
        {
            if (s is TabItem tab)
            {
                UseTransparentizer(t => t.StyleTabItem(tab));
            }
        });
        view.AddWeakPropertyChangeHandler(View_IsActiveProperty, (_, _) =>
        {
            if (weakTab.TryGetTarget(out TabItem tab))
            {
                UseTransparentizer(t => t.StyleTabItem(tab));
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
        if (handle != IntPtr.Zero)
        {
            StyleHwnd(handle);
        }
    }

    public void StyleControl(Control control)
    {
        switch (control.Name)
        {
            // Warning dialog, footer
            case "OKButton"
            when control is Button &&
                Window.GetWindow(control) is DialogWindowBase &&
                control.GetVisualOrLogicalParent()?
                       .GetVisualOrLogicalParent() is Border footer:
                Layer(footer);
                return;

            // Host of WpfTextView I guess
            case "WpfTextViewHost":
                control.Resources["outlining.chevron.expanded.background"] =
                control.Resources["outlining.chevron.collapsed.background"] = Brushes.Transparent;

                control.LogicalDescendant<Grid>()?.Background = Brushes.Transparent;
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

            // VSIX manifest editor
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
            case "MainTabControl"
            when control.GetVisualOrLogicalParent()?
                        .GetVisualOrLogicalParent() is Grid { Name: "LayoutRoot" } root:
                control.Background = root.Background = Brushes.Transparent;
                return;

            // Resource editor
            case "_resourceView"
            when control is ContentControl { Content: DockPanel panel }:
                panel.Background = Brushes.Transparent;
                return;

            // Code coverage, column headers
            case "GridHeader" when control is DataGrid:
                control.Background = Brushes.Transparent;
                return;

            // Pull Members Up, member list
            case "MemberSelectionGrid"
            when control is DataGrid { RowStyle: { } style } grid:
                grid.RowStyle = SubclassStyle(style);
                return;

            // Document Outline, root
            case "DocumentOutline"
            when control.GetVisualOrLogicalParent() is Panel adapter:
                Layer(adapter);

                if (!GetIsTracked(adapter))
                {
                    adapter.SizeChanged += OnSizeChanged;
                    SetIsTracked(adapter, value: true);
                }

                static void OnSizeChanged(object sender, RoutedEventArgs e)
                {
                    if (sender is Panel adapter)
                    {
                        UseTransparentizer(t => t.Layer(adapter));
                    }
                }
                return;

            // Document Outline, list view
            case "SymbolTree"
            when control is TreeView && control.FindDescendant<Rectangle>() is { } rectangle:
                rectangle.Fill = Brushes.Transparent;
                return;

            // Document Outline, designer root
            case "DocumentOutlinePaneHolder":
                control.Background = Brushes.Transparent;
                return;

            #region Git Windows

            case "gitWindowView" or // Git changes window
                "focusedWindowView" or // Git repository window
                "detailsView" or // Git commit details
                "focusedDetailsContainer" or // Git commit details container
                "teamExplorerFrame" or // Team explorer window
                "createPullRequestView": // New PR window
            GitWindow:
                {
                    control.Background = Brushes.Transparent;

                    // Git command buttons
                    if (control.TryFindResource("TESectionCommandButtonStyle") is Style style)
                    {
                        TransparentizeStyle(style);
                    }

                    StyleTree(control.LogicalDescendants<FrameworkElement>());
                }
                return;

            // Commit history
            case "historyView"
            when control.GetVisualOrLogicalParent()?
                        .GetVisualOrLogicalParent()?
                        .GetVisualOrLogicalParent() is Border history:
                history.Background = Brushes.Transparent;
                goto GitWindow;

            // Smth in a git window
            case "thisPageControl":
                control.Background = Brushes.Transparent;
                return;

            // Team explorer, project selector
            case "navControl":
                control.Background = Brushes.Transparent;
                return;

            // Git branch selector
            case "branchesList"
            when control.GetVisualOrLogicalParent()?
                        .GetVisualOrLogicalParent()?
                        .GetVisualOrLogicalParent()?
                        .GetVisualOrLogicalParent() is Control branches:
                branches.Background = Brushes.Transparent;
                return;

            // Commit history list
            case "historyListView"
            when control is ListView { View: GridView { ColumnHeaderContainerStyle: { } style } grid }:
                grid.ColumnHeaderContainerStyle = SubclassStyle(style);

                foreach (var c in grid.Columns.Select(c => c.Header).OfType<Control>())
                {
                    c.ApplyTemplate();
                    c.FindDescendant<Border>(b => b.Name == "HeaderBorder")?.BorderBrush = Brushes.Transparent;
                }
                return;

            // Commit diff info
            case "pageContentViewer"
            when control.GetVisualOrLogicalParent()?
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
            case "statusControl" or // Actions/toolbar
                "inactiveRepoContent" or // Create repo
                "sectionContainer": // Branches and tags
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

            // JSON editor, schema selector
            case "Microsoft.WebTools.Languages.Json.VS.Schema.DropdownMargin.JsonSchemaDropdown"
            when control.FindDescendant<Grid>() is { } grid:
                grid.Background = Brushes.Transparent;
                break;

            // Memory layout
            case "Microsoft.VisualStudio.VC.MemoryViewer.MemoryViewerControl":
                control.Resources.MergedDictionaries.Clear();
                break;
        }
    }

    public void StylePanel(Panel panel)
    {
        switch (panel.Name)
        {
            // Commit diff
            case "detailsViewMainGrid"
            when panel.GetVisualOrLogicalParent()?
                      .GetVisualOrLogicalParent() is Border details:
                details.Background = Brushes.Transparent;
                return;

            // Status bar
            case "StatusBarPanel":
                return;

            // Editor window, loading placeholder
            case "StackPanel_LoadingDocumentUI":
                panel.Background = Brushes.Transparent;
                return;

            // Commit history, toolbar container
            case "toolbarGrid":
                panel.Background = Brushes.Transparent;
                return;

            // Pull request window, toolbar
            case "targetAndSourceBranchPickers":
                panel.Background = Brushes.Transparent;
                return;

            // Full-screen title bar
            case "MenuBarDockPanel":
                panel.Background = Brushes.Transparent;
                return;

            // Document Outline, designer item container
            case "SplitterGrid" when panel is Grid grid:
                Layer(grid);

                foreach (var border in grid.Children.OfType<Border>())
                {
                    border.Background = Brushes.Transparent;
                }
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
            when !panel.FindDescendants<Panel>().Any(p => p.GetType().FullName == "Microsoft.VisualStudio.Text.Utilities.ContainerMargin"):
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

            // Memory layout, item list
            case "Microsoft.VisualStudio.VC.MemoryViewer.MemoryLayoutCanvas"
            when panel.FindDescendant<Line>() is null &&
                panel.GetType().GetProperty("FontBrush", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public) is { } property &&
                panel.TryFindResource(SolidBackgroundFillTertiaryKey) is Brush brush:
                property.SetValue(panel, brush);
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

            // Editor window, PR comment toolbar
            case "Microsoft.VisualStudio.Commenting.Presentation.Comments.Margin.CommentToolbar":
                border.Background = Brushes.Transparent;
                break;

            // Commit diff view, toolbar
            case "Microsoft.VisualStudio.Differencing.Package.DiffControlToolbar":
                border.Background = Brushes.Transparent;
                break;

            // List window, root
            case "Microsoft.VisualStudio.ErrorListPkg.TableControlToolWindowPaneBase+ContentWrapper":
                border.Resources.MergedDictionaries.Clear();
                break;
        }
    }

    public void Layer(Control control)
    {
        control.SetResourceReference(Control.BackgroundProperty, LayeredBrushKey);
    }

    public void Layer(Panel panel)
    {
        panel.SetResourceReference(Panel.BackgroundProperty, LayeredBrushKey);
    }

    public void Layer(Border border)
    {
        border.SetResourceReference(Border.BackgroundProperty, LayeredBrushKey);
    }

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
        if (style.Setters.FirstOrDefault(s => (s as Setter)?.Property == IsTrackedProperty) is not null)
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

    private static void UseTransparentizer(Action<ElementTransparentizer> action)
    {
        if (s_transparentizer is null)
        {
            return;
        }

        try
        {
            action(s_transparentizer);
        }
        catch (Exception ex)
        {
            s_transparentizer._logger.Output(ex);
        }
    }

    #region IsTrackedProperty

    private static bool GetIsTracked(FrameworkElement target)
    {
        return (bool)target.GetValue(IsTrackedProperty);
    }

    private static void SetIsTracked(FrameworkElement target, bool value)
    {
        target.SetValue(IsTrackedProperty, value);
    }

    private static readonly DependencyProperty IsTrackedProperty =
        DependencyProperty.RegisterAttached("IsTracked", typeof(bool), typeof(ElementTransparentizer), new(defaultValue: false));

    #endregion

    #region Dispose

    private bool _disposed;

    void IDisposable.Dispose()
    {
        if (!_disposed)
        {
            _source.Cancel();
            _source.Dispose();

            s_transparentizer = null;
            Interlocked.Exchange(ref _visualHook, null)?.Dispose();

            _eventHook?.Dispose();

            _window.FrameIsOnScreenChanged -= OnFrameIsOnScreenChanged;
            _window.ActiveFrameChanged -= OnActiveFrameChanged;
            _window.WindowOpened -= OnWindowOpened;

            _disposed = true;
        }
    }

    #endregion
}
