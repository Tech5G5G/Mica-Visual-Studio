using System.Reflection;
using System.Windows.Shapes;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Expression = System.Linq.Expressions.Expression;

namespace MicaVisualStudio.VisualStudio;

/// <summary>
/// Represents an observer that listens and styles Visual Studio windows.
/// </summary>
//This code is bad, but it works, so...
public sealed class VsWindowStyler : IVsWindowFrameEvents, IDisposable
{
    /// <summary>
    /// Gets the singleton instance of <see cref="VsWindowStyler"/>.
    /// </summary>
    public static VsWindowStyler Instance => field ??= new();

    #region Keys

    private const string MultiViewHostTypeName = "Microsoft.VisualStudio.Editor.Implementation.WpfMultiViewHost";

    private const string SolidBackgroundFillTertiaryLayeredKey = "VsBrush.SolidBackgroundFillTertiaryLayered",
        PopupBackgroundLayeredKey = "VsBrush.PopupBackgroundLayered";

    private readonly ThemeResourceKey SolidBackgroundFillTertiaryKey =
        new(category: new("73708ded-2d56-4aad-b8eb-73b20d3f4bff"), name: "SolidBackgroundFillTertiary", ThemeResourceKeyType.BackgroundColor);

    private readonly ThemeResourceKey TextFillPrimaryKey =
        new(category: new("73708ded-2d56-4aad-b8eb-73b20d3f4bff"), name: "TextFillPrimary", ThemeResourceKeyType.BackgroundBrush);

    private readonly ThemeResourceKey TextOnAccentFillPrimaryKey =
        new(category: new("73708ded-2d56-4aad-b8eb-73b20d3f4bff"), name: "TextOnAccentFillPrimary", ThemeResourceKeyType.BackgroundBrush);

    private readonly ThemeResourceKey ScrollBarBackgroundKey =
        new(category: new("{624ed9c3-bdfd-41fa-96c3-7c824ea32e3d}"), name: "ScrollBarBackground", ThemeResourceKeyType.BackgroundBrush);

    #endregion

    #region Shells

    private readonly IVsUIShell shell = VS.GetRequiredService<SVsUIShell, IVsUIShell>();
    private readonly IVsUIShell5 shell5 = VS.GetRequiredService<SVsUIShell, IVsUIShell5>();
    private readonly IVsUIShell7 shell7 = VS.GetRequiredService<SVsUIShell, IVsUIShell7>();

    private uint cookie;

    #endregion

    #region Functions

    private Func<IVsWindowFrame, DependencyObject> get_WindowFrame_FrameView;
    private Func<DependencyObject, object> get_View_Content;
    private Func<DependencyObject, bool> get_View_IsActive;
    private Func<object, bool> IsDockTarget;

    private DependencyProperty View_ContentProperty,
        View_IsActiveProperty;

    #endregion

    private ILHook visualHook, sourceHook;

    private bool makeLayered = true;

    private VsWindowStyler() { }

    /// <summary>
    /// Tells the <see cref="VsWindowStyler"/> to start listening to and styling Visual Studio windows.
    /// </summary>
    public void Listen()
    {
        if (disposed)
            return;

        #region Function Initialization

        var frameViewProp = Type.GetType("Microsoft.VisualStudio.Platform.WindowManagement.WindowFrame, Microsoft.VisualStudio.Platform.WindowManagement")
                                .GetProperty("FrameView");

        var frameParam = Expression.Parameter(typeof(IVsWindowFrame));
        get_WindowFrame_FrameView = frameParam.Convert(frameViewProp.DeclaringType)
                                              .Property(frameViewProp)
                                              .Convert<DependencyObject>()
                                              .Compile<IVsWindowFrame, DependencyObject>(frameParam);

        var viewType = Type.GetType("Microsoft.VisualStudio.PlatformUI.Shell.View, Microsoft.VisualStudio.Shell.ViewManager");
        var contentProp = viewType.GetProperty("Content");

        var viewParam = Expression.Parameter(typeof(DependencyObject));
        get_View_Content = viewParam.Convert(contentProp.DeclaringType)
                                    .Property(contentProp)
                                    .Compile<DependencyObject, object>(viewParam);

        View_ContentProperty = viewType.GetField("ContentProperty", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                                       .GetValue(null) as DependencyProperty;

        var isActiveProp = viewType.GetProperty("IsActive");
        get_View_IsActive = viewParam.Convert(isActiveProp.DeclaringType)
                                     .Property(isActiveProp)
                                     .Compile<DependencyObject, bool>(viewParam);

        View_IsActiveProperty = viewType.GetField("IsActiveProperty", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                                        .GetValue(null) as DependencyProperty;

        var dockType = Type.GetType("Microsoft.VisualStudio.PlatformUI.Shell.Controls.DockTarget, Microsoft.VisualStudio.Shell.ViewManager");
        var objectParam = Expression.Parameter(typeof(object));
        IsDockTarget = objectParam.TypeIs(dockType)
                                  .Compile<object, bool>(objectParam);

        #endregion

        #region Layered Brushes

        AddLayeredBrushes();
        (Application.Current.Resources.MergedDictionaries as INotifyCollectionChanged).CollectionChanged += (s, e) => AddLayeredBrushes();

        void AddLayeredBrushes()
        {
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
            }
        }

        #endregion

        #region Events

#pragma warning disable VSTHRD010 //Invoke single-threaded types on Main thread
        cookie = shell7.AdviseWindowFrameEvents(this);
#pragma warning restore VSTHRD010 //Invoke single-threaded types on Main thread

        EventManager.RegisterClassHandler(
            dockType,
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((s, e) => ApplyToDockTarget(s as Border)));

        if (AppDomain.CurrentDomain.GetAssemblies()
                                   .FirstOrDefault(i => i.GetName().Name == "Microsoft.VisualStudio.Editor.Implementation")?
                                   .GetTypes()
                                   .FirstOrDefault(i => i.FullName == MultiViewHostTypeName) is Type hostType)
            EventManager.RegisterClassHandler(
                hostType,
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler((s, e) => ApplyToContent(s as DockPanel, applyToDock: false)));

        WindowObserver.Instance.WindowOpened += (s, e) =>
        {
            if (s is not null)
                ApplyToWindow(s);
        };

        #endregion

        #region Hooks

        visualHook = CreatePostfix<Visual, Visual>(
            typeof(Visual).GetMethod("AddVisualChild", BindingFlags.Instance | BindingFlags.NonPublic),
            AddVisualChild);

        sourceHook = CreatePostfix<HwndSource, Visual>(
            typeof(HwndSource).GetProperty("RootVisual").SetMethod,
            RootVisualChanged);

        static void AddVisualChild(Visual instance, Visual child)
        {
            if (instance is ContentControl or ContentPresenter or Decorator or Panel && //Avoid unnecessary work
                instance is FrameworkElement content &&
                GetIsTracked(content) && Instance is VsWindowStyler styler)
                styler.ApplyToContent(content, applyToDock: false);
        }

        static void RootVisualChanged(HwndSource instance, Visual value)
        {
            if (value is null || instance.CompositionTarget is null)
                return;

            //Visual Studio popup
            if (value is FrameworkElement element &&
                element.Parent is Popup && //Check if root of popup
                element.FindDescendant<FrameworkElement>(i => i.Name == "DropShadowBorder") is Border drop)
            {
                //Remove current border and update background to be translucent
                drop.BorderBrush = Brushes.Transparent;
                drop.SetResourceReference(Border.BackgroundProperty, PopupBackgroundLayeredKey);

                //Get left-side offset to move window by
                var offset = GetPopupOffset(element);

                if (offset == default) //Get if any offset already cached
                    if (element.FindDescendant<ToolTip>() is ToolTip tip)
                    {
                        SetPopupOffset(element, offset = new(tip.Margin.Left, tip.Margin.Top));
                        tip.Margin = default;
                    }
                    else
                    {
                        SetPopupOffset(element, offset = new(drop.Margin.Left, drop.Margin.Top));
                        drop.Margin = default;
                    }

                //Get and update window position
                var deviceOffset = instance.CompositionTarget.TransformToDevice.Transform(offset);
                Interop.WindowHelper.OffsetWindow(instance.Handle, offset: new((int)deviceOffset.X, (int)deviceOffset.Y));

                //Add acrylic, shadow, and border
                Interop.WindowHelper.EnableWindowBlur(instance.Handle, enable: true);
                Interop.WindowHelper.SetCornerPreference(instance.Handle, CornerPreference.Round);
            }
            else if (value is not Window) //Avoid already handled values
                instance.CompositionTarget.BackgroundColor = Colors.Transparent;
        }

        #endregion

        makeLayered = General.Instance.LayeredWindows;

        ApplyToAllWindows();
        ApplyToAllWindowPanesAsync().Forget();
    }

    #region Apply To All

    private void ApplyToAllWindows() => WindowObserver.AllWindows.ForEach(ApplyToWindow);

    private async Task ApplyToAllWindowPanesAsync()
    {
        if (shell is null)
            return;

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        shell.GetToolWindowEnum(out IEnumWindowFrames toolEnum);
        shell.GetDocumentWindowEnum(out IEnumWindowFrames docEnum);

        foreach (var frame in toolEnum.ToEnumerable().Concat(docEnum.ToEnumerable()))
            ApplyToWindowFrame(frame);
    }

    #endregion

    private void ApplyToWindowFrame(IVsWindowFrame frame)
    {
        if (get_WindowFrame_FrameView(frame) is not DependencyObject view)
            return;

        if (get_View_Content(view) is not Grid host)
        {
            WeakReference<IVsWindowFrame> weakFrame = new(frame);

            view.AddWeakOneTimePropertyChangeHandler(View_ContentProperty, (s, e) =>
            {
                if (weakFrame.TryGetTarget(out IVsWindowFrame frame))
                    ApplyToWindowFrame(frame);
            });
            return;
        }

        ApplyToContent(host);
    }

    private void ApplyToWindow(Window window)
    {
        //Wizards (e.g. packaged app creation wizard)
        if (window.GetType().FullName == "Microsoft.VisualStudio.WizardFrameworkWpf.WizardBase")
        {
            ApplyToContent(window, applyToDock: false);
            return;
        }

        foreach (var element in window.FindDescendants<FrameworkElement>())

            //Warning dialog, footer
            if (window is DialogWindowBase && element is Button { Name: "OKButton" } button &&
                button.FindAncestor<FrameworkElement>()?.FindAncestor<Border>() is Border buttonFooter)
                buttonFooter.SetResourceReference(Border.BackgroundProperty, SolidBackgroundFillTertiaryLayeredKey);

            else if (element is Border border)
            {
                //Footer
                if (border.Name == "FooterBorder")
                    border.SetResourceReference(Border.BackgroundProperty, SolidBackgroundFillTertiaryLayeredKey);

                //Dock target
                else if (IsDockTarget(border))
                    ApplyToDockTarget(border);
            }
    }

    private void ApplyToDockTarget(Border dock, bool applyToContent = true)
    {
        if (dock.Name == "ViewFrameTarget")
            dock.SetResourceReference(Border.BackgroundProperty, SolidBackgroundFillTertiaryLayeredKey);
        else
            dock.Background = Brushes.Transparent; //Smoke layer underneath tabs

        var descendants = dock.FindDescendants<FrameworkElement>();
        if (!descendants.Any())
            return;

        //Content area
        descendants.FindElement<Border>("PART_ContentPanel")?
                   .SetResourceReference(Border.BackgroundProperty, SolidBackgroundFillTertiaryLayeredKey);

        //Title bar
        descendants.FindElement<Control>("PART_Header")?.Background = Brushes.Transparent;

        if (descendants.FindElement<Border>("ToolWindowBorder") is Border border) //Body
        {
            border.Background = Brushes.Transparent;

            if (applyToContent && border.FindDescendant<Grid>() is Grid host)
                ApplyToContent(host, applyToDock: false);
        }

        if (descendants.FindElement<Panel>("PART_TabPanel") is Panel tabs) //Tab strip
            foreach (var tab in tabs.Children.OfType<TabItem>()) //Tab items
            {
                tab.Background = Brushes.Transparent;

                if (tab.DataContext is not DependencyObject view)
                    continue;

                ApplyTabForeground(tab, view);

                if (GetIsTracked(tab))
                    continue;

                SetIsTracked(tab, value: true);
                WeakReference<TabItem> weakTab = new(tab);

                tab.AddWeakPropertyChangeHandler(TabItem.IsSelectedProperty, (s, e) =>
                {
                    if (s is TabItem tab && tab.DataContext is DependencyObject view)
                        ApplyTabForeground(tab, view);
                });
                view.AddPropertyChangeHandler(View_IsActiveProperty, (s, e) =>
                {
                    if (weakTab.TryGetTarget(out TabItem tab) && s is DependencyObject view)
                        ApplyTabForeground(tab, view);
                });

                void ApplyTabForeground(TabItem item, DependencyObject view) => tab.SetResourceReference(
                    Control.ForegroundProperty,
                    tab.IsSelected && get_View_IsActive(view) ? TextOnAccentFillPrimaryKey : TextFillPrimaryKey);
            }
    }

    private void ApplyToContent(FrameworkElement content, bool applyToDock = true)
    {
        if (!content.IsLoaded)
            content.AddWeakOneTimeHandler(FrameworkElement.LoadedEvent, (s, e) => ApplyToContent(s as FrameworkElement, applyToDock));

        if (applyToDock && content.FindAncestor<DependencyObject>(i => i.GetVisualOrLogicalParent(), IsDockTarget) is Border dock)
            ApplyToDockTarget(dock, applyToContent: false);

        foreach (var element in content.FindDescendants<FrameworkElement>().Append(content))
        {
            if (element is ContentControl or ContentPresenter or Decorator or Panel && !GetIsTracked(element))
                SetIsTracked(element, value: true); //Track visual children

            if (element is ToolBar bar)
            {
                bar.Background = bar.BorderBrush = Brushes.Transparent;
                (bar.Parent as ToolBarTray)?.Background = Brushes.Transparent;
            }

            else if (makeLayered && element is HwndHost { IsLoaded: true } host)
            {
                var sources = PresentationSource.CurrentSources.OfType<HwndSource>().ToArray();
                if (sources.Any(i => i.Handle == host.Handle))
                    continue;

                var children = Interop.WindowHelper.GetChildren(host.Handle);

                if (sources.FirstOrDefault(i => children.Contains(i.Handle)) is not HwndSource source)
                    Interop.WindowHelper.MakeLayered(host.Handle);
            }

            else if (element is Control control)
                switch (control.Name)
                {
                    case "gitWindowView" or //Git changes window
                        "focusedWindowView" or //Git repository window
                        "historyView" or //Commit history
                        "detailsView" or //Git commit details
                        "focusedDetailsContainer" or //Git commit details container
                        "teamExplorerFrame" or //Team explorer window
                        "createPullRequestView": //New PR window
                        control.Background = Brushes.Transparent;

                        foreach (var e in control.LogicalDescendants<FrameworkElement>().Append(control))
                            switch (e.Name)
                            {
                                //Section header
                                case "borderHeader" when e is Border { Style: Style bs } b:
                                    b.Style = new(bs.TargetType, bs)
                                    {
                                        Setters = { new Setter(Border.BorderBrushProperty, Brushes.Transparent) }
                                    };
                                    break;

                                //Command buttons
                                case "gitAction" or
                                    "detailsView" when
                                    e.TryFindResource("TESectionCommandButtonStyle") is Style { Setters.IsSealed: false } ss:
                                    Setter bg = new(Control.BackgroundProperty, Brushes.Transparent),
                                    bb = new(Control.BorderBrushProperty, Brushes.Transparent);

                                    ss.Setters.Add(bg);
                                    ss.Setters.Add(bb);

                                    if (ss.Triggers.OfType<Trigger>()
                                                   .FirstOrDefault(i => i.Property == System.Windows.UIElement.IsEnabledProperty) is Trigger t)
                                    {
                                        t.Setters.Add(bg);
                                        t.Setters.Add(bb);
                                    }
                                    break;

                                //???
                                case "thisPageControl" when e is Control c:
                                    c.Background = Brushes.Transparent;
                                    break;

                                //Team explorer, project selector
                                case "navControl" when e is Control c:
                                    c.Background = Brushes.Transparent;
                                    break;

                                //Git branch selector
                                case "branchesList" when e.GetVisualOrLogicalParent()
                                                          .GetVisualOrLogicalParent()
                                                          .GetVisualOrLogicalParent()
                                                          .GetVisualOrLogicalParent() is Control bc:
                                    bc.Background = Brushes.Transparent;
                                    break;

                                //Commit history
                                case "historyView" when e.GetVisualOrLogicalParent()
                                                         .GetVisualOrLogicalParent()
                                                         .GetVisualOrLogicalParent() is Border hb:
                                    hb.Background = Brushes.Transparent;
                                    break;

                                //Commit history list
                                case "historyListView" when e is ListView { View: GridView g }:
                                    g.ColumnHeaderContainerStyle = new(g.ColumnHeaderContainerStyle.TargetType, g.ColumnHeaderContainerStyle)
                                    {
                                        Setters =
                                        {
                                            new Setter(Control.BackgroundProperty, Brushes.Transparent),
                                            new Setter(Control.BorderBrushProperty, Brushes.Transparent)
                                        }
                                    };

                                    foreach (var c in g.Columns.Select(i => i.Header).OfType<Control>())
                                    {
                                        c.ApplyTemplate();
                                        c.FindDescendant<Border>(i => i.Name == "HeaderBorder")?.BorderBrush = Brushes.Transparent;
                                    }
                                    break;

                                //Commit diff
                                case "detailsViewMainGrid" when e is Grid g && g.GetVisualOrLogicalParent()
                                                                                .GetVisualOrLogicalParent() is Border db:
                                    db.Background = Brushes.Transparent;
                                    break;

                                //Commit diff info
                                case "pageContentViewer" when e.GetVisualOrLogicalParent()
                                                               .GetVisualOrLogicalParent() is Border pb:
                                    pb.Background = Brushes.Transparent;
                                    break;

                                //Commit diff presenter dock buttons
                                case "dockToBottomButton" or
                                    "dockToRightButton" or
                                    "undockButton" or
                                    "maximizeMinimizeButton" or
                                    "closeButton" when
                                    e is Button b:
                                    b.Style = new(b.Style.TargetType, b.Style)
                                    {
                                        Setters =
                                        {
                                            new Setter(Control.BackgroundProperty, Brushes.Transparent),
                                        }
                                    };
                                    break;

                                //Git push, pull etc. buttons
                                case "actionButton" or
                                    "fetchButton" or
                                    "pullButton" or
                                    "pushButton" or
                                    "syncButton" or
                                    "additionalOperationsButton" when
                                    e is Button b:
                                    b.Style = new(b.Style.TargetType, b.Style)
                                    {
                                        Triggers =
                                        {
                                            new Trigger
                                            {
                                                Property = System.Windows.UIElement.IsEnabledProperty,
                                                Value = false,
                                                Setters =
                                                {
                                                    new Setter(Control.BackgroundProperty, Brushes.Transparent),
                                                    new Setter(Control.BorderBrushProperty, Brushes.Transparent)
                                                }
                                            }
                                        }
                                    };
                                    break;

                                //Git repository window, presenters
                                case "detailsContent" or
                                    "detailsFullWindowContent" or
                                    "detailsRightContent" or
                                    "detailsBottomContent" when
                                    e is ContentControl c:
                                    SetIsTracked(c, value: true);
                                    break;

                                case "statusControl" or //Actions/tool bar
                                    "thisPageControl" or //Changes
                                    "inactiveRepoContent" or //Create repo
                                    "sectionContainer" or //Branches and tags
                                    "amendCheckBox" when //Checkbox... for amending...
                                    e is Control c:
                                    c.Background = Brushes.Transparent;
                                    break;

                                default:
                                    if (e is ItemsControl { ItemContainerStyle: Style ics } ic) //Changes
                                    {
                                        ic.Background = Brushes.Transparent;

                                        if (!ics.IsSealed)
                                        {
                                            ics.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
                                            ics.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
                                        }
                                    }
                                    break;
                            }
                        break;

                    //Host of WpfTextView I guess
                    case "WpfTextViewHost":
                        control.Resources["outlining.chevron.expanded.background"] =
                        control.Resources["outlining.chevron.collapsed.background"] = Brushes.Transparent;
                        break;

                    //Editor, output, etc. text
                    case "WpfTextView" when element is ContentControl:
                        control.Background = Brushes.Transparent;
                        control.FindDescendant<Canvas>()?.Background = Brushes.Transparent;
                        break;

                    //Packaged app configurations list
                    case "PackageConfigurationsList" when control is DataGrid grid:
                        grid.Background = grid.RowBackground = Brushes.Transparent;

                        if (grid.CellStyle is Style { Setters.IsSealed: false } cs)
                            cs.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
                        break;

                    //VSIX manfiest editor
                    case "VsixEditorControl":
                        control.Background = Brushes.Transparent;

                        foreach (var tab in control.FindDescendants<TabItem>())
                            tab.Background = Brushes.Transparent;
                        break;

                    default:
                        switch (control.GetType().FullName)
                        {
                            //AppxManifest editor
                            case "Microsoft.VisualStudio.AppxManifestDesigner.Designer.ManifestDesignerUserControlProxy":
                            case "Microsoft.VisualStudio.AppxManifestDesigner.Designer.ManifestDesignerUserControl":
                                control.Background = Brushes.Transparent;
                                break;

                            //Resource editor
                            case "Microsoft.VisualStudio.ResourceExplorer.UI.ResourceGroupEditorControl":
                                control.Background = Brushes.Transparent;
                                break;
                        }
                        break;
                }

            else if (element is Panel panel)
                switch (panel.GetType().FullName)
                {
                    //Editor window, root
                    case MultiViewHostTypeName:
                        element.Resources[ScrollBarBackgroundKey] = Brushes.Transparent;
                        break;

                    //Editor window, bottom container
                    case "Microsoft.VisualStudio.Text.Utilities.ContainerMargin" when
                        !panel.FindDescendants<Panel>().Any(i => i.GetType().FullName == "Microsoft.VisualStudio.Text.Utilities.ContainerMargin"):
                        panel.SetResourceReference(Panel.BackgroundProperty, SolidBackgroundFillTertiaryLayeredKey);
                        break;

                    //Editor window, left side icon container
                    case "Microsoft.VisualStudio.Text.Editor.Implementation.GlyphMarginGrid" when
                        panel.Background is SolidColorBrush solid && solid.Color != Brushes.Transparent.Color:
                        panel.SetResourceReference(Panel.BackgroundProperty, SolidBackgroundFillTertiaryLayeredKey);
                        break;

                    //Scroll bar intersection
                    case "Microsoft.VisualStudio.Editor.Implementation.BottomRightCornerSpacerMargin":
                        panel.Background = Brushes.Transparent;
                        break;

                    //Editor window, collapsed item container
                    case "Microsoft.VisualStudio.Text.Editor.Implementation.AdornmentLayer":
                        foreach (var rectangle in panel.FindDescendants<Rectangle>())
                            rectangle.SetResourceReference(Shape.FillProperty, SolidBackgroundFillTertiaryLayeredKey);
                        break;

                    default:
                        if (panel is DockPanel || (panel is Grid { Background: not null } && panel.FindAncestor<Control>() is not Button or TextBox))
                            panel.Background = Brushes.Transparent;
                        break;
                }

            else if (element is Border border)
                switch (border.GetType().FullName)
                {
                    //Output window, base layer
                    case "Microsoft.VisualStudio.PlatformUI.OutputWindow":
                        border.Background = Brushes.Transparent;
                        break;

                    //Editor window, file errors container
                    case "Microsoft.VisualStudio.UI.Text.Wpf.FileHealthIndicator.Implementation.FileHealthIndicatorMargin":
                        border.Background = Brushes.Transparent;
                        break;
                }
        }
    }

    private static ILHook CreatePostfix<T0, T1>(MethodInfo info, Action<T0, T1> action) =>
        new(info, context =>
        {
            ILCursor cursor = new(context);
            cursor.Index = cursor.Instrs.Count - 1; //Move cursor to end, but before return

            cursor.Emit(OpCodes.Ldarg_0); //this
            cursor.Emit(OpCodes.Ldarg_1); //First parameter

            cursor.EmitDelegate(action);
        });

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
    /// Identifies the MicaVisualStudio.VisualStudio.VsWindowStyler.IsTracked dependency property.
    /// </summary>
    public static readonly DependencyProperty IsTrackedProperty =
        DependencyProperty.RegisterAttached("IsTracked", typeof(bool), typeof(VsWindowStyler), new(defaultValue: false));

    #endregion

    #region PopupOffsetProperty

    /// <summary>
    /// Gets the value of the <see cref="PopupOffsetProperty"/> attached property from a given <see cref="FrameworkElement"/>.
    /// </summary>
    /// <param name="target">The <see cref="FrameworkElement"/> from which to read the property value.</param>
    /// <returns>The value of the <see cref="PopupOffsetProperty"/> attached property.</returns>
    public static Point GetPopupOffset(FrameworkElement target) =>
        (Point)target.GetValue(PopupOffsetProperty);

    /// <summary>
    /// Sets the value of the <see cref="PopupOffsetProperty"/> attached property from a given <see cref="FrameworkElement"/>.
    /// </summary>
    /// <param name="target">The <see cref="FrameworkElement"/> on which to set the attached property.</param>
    /// <param name="value">The property value to set.</param>
    public static void SetPopupOffset(FrameworkElement target, Point value) =>
        target.SetValue(PopupOffsetProperty, value);

    /// <summary>
    /// Identifies the MicaVisualStudio.VisualStudio.VsWindowStyler.IsTracked dependency property.
    /// </summary>
    public static readonly DependencyProperty PopupOffsetProperty =
        DependencyProperty.RegisterAttached("PopupOffset", typeof(Point), typeof(VsWindowStyler), new(defaultValue: default(Point)));

    #endregion

    #region IVsWindowFrameEvents

    public void OnFrameCreated(IVsWindowFrame frame) { }

    public void OnFrameDestroyed(IVsWindowFrame frame) { }

    public void OnFrameIsVisibleChanged(IVsWindowFrame frame, bool newIsVisible) { }

    public void OnFrameIsOnScreenChanged(IVsWindowFrame frame, bool newIsOnScreen)
    {
        if (newIsOnScreen)
            ApplyToWindowFrame(frame);
    }

    public void OnActiveFrameChanged(IVsWindowFrame oldFrame, IVsWindowFrame newFrame)
    {
        if (newFrame is not null)
            ApplyToWindowFrame(newFrame);
    }

    #endregion

    #region Dispose

    private bool disposed;

    /// <summary>
    /// Disposes the singleton instance of <see cref="VsWindowStyler"/>.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
            return;

        visualHook?.Dispose();
        sourceHook?.Dispose();
        visualHook = sourceHook = null;

#pragma warning disable VSTHRD010 //Invoke single-threaded types on Main thread
        if (cookie > 0)
            shell7.UnadviseWindowFrameEvents(cookie);
#pragma warning restore VSTHRD010 //Invoke single-threaded types on Main thread

        disposed = true;
    }

    #endregion
}
