using System.Windows.Shapes;

namespace MicaVisualStudio.VisualStudio;

public partial class VsWindowStyler
{
    private const string MultiViewHostTypeName = "Microsoft.VisualStudio.Editor.Implementation.WpfMultiViewHost";

    private bool layeredWindows = true, acrylicMenus = true;

    private void SubscribeToEvents()
    {
#pragma warning disable VSTHRD010 //Invoke single-threaded types on Main thread
        cookie = shell7.AdviseWindowFrameEvents(this);
#pragma warning restore VSTHRD010 //Invoke single-threaded types on Main thread

        EventManager.RegisterClassHandler(
            Type.GetType("Microsoft.VisualStudio.PlatformUI.Shell.Controls.DockTarget, Microsoft.VisualStudio.Shell.ViewManager"),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((s, e) => ApplyToDockTarget(s as Border)));

        if (AppDomain.CurrentDomain.GetAssemblies()
                                   .FirstOrDefault(i => i.GetName().Name == "Microsoft.VisualStudio.Editor.Implementation")?
                                   .GetType(MultiViewHostTypeName) is Type hostType)
            EventManager.RegisterClassHandler(
                hostType,
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler((s, e) => ApplyToContent(s as DockPanel, applyToDock: false)));

        WindowObserver.Instance.WindowOpened += (s, e) =>
        {
            if (s is not null)
                ApplyToWindow(s);
        };

        var general = General.Instance;
        layeredWindows = general.LayeredWindows;
        acrylicMenus = general.AcrylicMenus;
    }

    private void UnsubscribeFromEvents()
    {
#pragma warning disable VSTHRD010 //Invoke single-threaded types on Main thread
        if (cookie > 0)
            shell7.UnadviseWindowFrameEvents(cookie);
#pragma warning restore VSTHRD010 //Invoke single-threaded types on Main thread
    }

    #region To All

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

    #region Windows

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

    private void ApplyToPopup(HwndSource source, Popup popup, FrameworkElement root)
    {
        if (acrylicMenus && //Only proceed if acrylic menus are enabled
            root.FindDescendant<Border>(i => i.Name == "DropShadowBorder") is Border drop) //Shadow host
            AddAcrylicToPopup(source, popup, root, drop);

        //Pointing popup (e.g. CodeLens references popup)
        else if (root.FindDescendant<FrameworkElement>()?
                     .FindDescendant<FrameworkElement>()?
                     .FindDescendant<Decorator>() is Decorator callout &&
                 callout.GetType().FullName == "Microsoft.VisualStudio.Language.Intellisense.CodeLensCalloutBorder")
            callout.SetResourceReference(Panel.BackgroundProperty, SolidBackgroundFillTertiaryKey);
    }

    private void AddAcrylicToPopup(HwndSource source, Popup popup, FrameworkElement root, Border drop)
    {
        //Remove margin
        (root.FindDescendant<ToolTip>() is ToolTip tip ? //Tool tips use themselves for margins
            tip : drop as FrameworkElement).Margin = default;

        //Remove popup margin accountment
        if (popup.HorizontalOffset == -12)
        {
            //If menu has custom placement...
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
                        //... replace left-aligned placement and add back X-axis margin accountment
                        var leftAlignedPlacement = placement[LeftAlignedPlacementIndex];

                        placement.Insert(
                            LeftAlignedPlacementIndex,
                            new(
                                point: new(
                                    x: leftAlignedPlacement.Point.X + (originalXOffset * 2), //idk why 2x is needed
                                    y: leftAlignedPlacement.Point.Y),
                                leftAlignedPlacement.PrimaryAxis));
                        placement.RemoveAt(LeftAlignedPlacementIndex + 1); //Remove replaced placement
                    }

                    return [.. placement];
                };
            }

            popup.HorizontalOffset = 0;
            popup.UpdateLayout();
        }

        //Replace border and update background to be translucent
        drop.CornerRadius = new(uniformRadius: 7);
        drop.SetResourceReference(Border.BorderBrushProperty, PopupBorderOnAcrylicKey);
        drop.SetResourceReference(Border.BackgroundProperty, PopupBackgroundLayeredKey);

        WindowHelper.SetCornerPreference(source.Handle, CornerPreference.Round); //Add shadow and corners
        WindowHelper.EnableWindowBorder(source.Handle, enable: false); //Remove border (we have our own)

        //Current popup background color
        var color = (drop.Background as SolidColorBrush)?.Color ?? default;

        //Enable acrylic
        WindowHelper.EnableWindowBlur(
            source.Handle,
            fallback: VsColorManager.Instance.VisualStudioTheme == Theme.Dark && color.IsGray() ?
                System.Drawing.Color.FromArgb(0x2C, 0x2C, 0x2C) : //Dark mode acrylic fallback
                System.Drawing.Color.FromArgb(color.R, color.G, color.B),
            enable: true);
    }

    #endregion

    #region Content

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

            else if (layeredWindows && element is HwndHost { IsLoaded: true, Handle: IntPtr handle })
            {
                var sources = PresentationSource.CurrentSources.OfType<HwndSource>()
                                                               .Select(i => i.Handle)
                                                               .ToArray();

                if (sources.Any(i => i == handle))
                    continue;

                var children = WindowHelper.GetChildren(handle);
                if (!sources.Any(children.Contains))
                    WindowHelper.MakeLayered(handle);
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
                                                   .FirstOrDefault(i => i.Property == UIElement.IsEnabledProperty) is Trigger t)
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
                                    e is Button { Style: Style s } b:
                                    b.Style = new(s.TargetType, s)
                                    {
                                        Triggers =
                                        {
                                            new Trigger
                                            {
                                                Property = UIElement.IsEnabledProperty,
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
                            if (rectangle.RadiusX > 0 || rectangle.StrokeThickness <= 0) //Check for selected line highlight from fluent redesign
                                rectangle.SetResourceReference(Shape.FillProperty, SolidBackgroundFillTertiaryLayeredKey);
                        break;

                    default:
                        if (panel is DockPanel || (panel is Grid { Background: not null } && panel.FindAncestor<Control>() is not (Button or TextBox)))
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

    #endregion
}