#pragma warning disable IDE0060 // Remove unused parameter

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.VisualStudio.PlatformUI;
using MicaVisualStudio.Extensions;
using MicaVisualStudio.Services.Resourcing;
using Transparentizer = MicaVisualStudio.Services.Styling.ElementTransparentizer;

namespace MicaVisualStudio.Services.Styling;

public partial class ElementTransparentizer
{
    private static readonly ConcurrentDictionary<Type, Action<Transparentizer, Control>> s_controlTypeHandlers = new()
    {
        [typeof(Control)] = ElementHandlers.Noop,
        [typeof(UserControl)] = ElementHandlers.Noop,

        [typeof(Label)] = ElementHandlers.Noop,
        [typeof(TextBox)] = ElementHandlers.Noop,
        [typeof(RichTextBox)] = ElementHandlers.Noop,

        [typeof(Button)] = ElementHandlers.Noop,
        [typeof(CheckBox)] = ElementHandlers.Noop,
        [typeof(ComboBox)] = ElementHandlers.Noop,
        [typeof(RadioButton)] = ElementHandlers.Noop,
        [typeof(RepeatButton)] = ElementHandlers.Noop,
        [typeof(ToggleButton)] = ElementHandlers.Noop,

        [typeof(ContentControl)] = ElementHandlers.Noop,
        [typeof(HeaderedContentControl)] = ElementHandlers.Noop,
        [typeof(Expander)] = ElementHandlers.Noop,
        [typeof(GroupBox)] = ElementHandlers.Noop,

        [typeof(Slider)] = ElementHandlers.Noop,
        [typeof(ScrollBar)] = ElementHandlers.Noop,
        [typeof(Separator)] = ElementHandlers.Noop,
        [typeof(ProgressBar)] = ElementHandlers.Noop,

        [typeof(ScrollViewer)] = ElementHandlers.Noop,
        [typeof(ItemsControl)] = ElementHandlers.Noop,
        [typeof(HeaderedItemsControl)] = ElementHandlers.Noop,
        [typeof(ListBox)] = ElementHandlers.Noop,
        [typeof(ListView)] = ElementHandlers.Noop,
        [typeof(TreeView)] = ElementHandlers.Noop,
        [typeof(TabControl)] = ElementHandlers.Noop,

        [typeof(TabItem)] = ElementHandlers.Noop,
        [typeof(MenuItem)] = ElementHandlers.Noop,
        [typeof(ListBoxItem)] = ElementHandlers.Noop,
        [typeof(ComboBoxItem)] = ElementHandlers.Noop,
        [typeof(ListViewItem)] = ElementHandlers.Noop,
        [typeof(TreeViewItem)] = ElementHandlers.Noop,
        [typeof(StatusBarItem)] = ElementHandlers.Noop,

        [typeof(ToolBar)] = ElementHandlers.Noop,
        [typeof(StatusBar)] = ElementHandlers.Noop,

        [typeof(Window)] = ElementHandlers.Noop,
        [typeof(ToolTip)] = ElementHandlers.Noop,
        [typeof(Menu)] = ElementHandlers.Noop,
        [typeof(ContextMenu)] = ElementHandlers.Noop
    };

    private static readonly Dictionary<string, Action<Transparentizer, Control>> s_controlHandlers = new()
    {
        // Generic Visual Studio toolbar
        { "Microsoft.VisualStudio.PlatformUI.VsToolBar", ControlHandlers.HandleToolBar },

        // Warning dialog, footer
        { "OKButton", ControlHandlers.HandleDialogOKButton },

        // Host of WpfTextView I guess
        { "WpfTextViewHost", ControlHandlers.HandleTextViewHost },

        // Editor, output, etc. text
        { "WpfTextView", ControlHandlers.HandleTextView },

        // Packaged app configurations list
        { "PackageConfigurationsList", ControlHandlers.HandlePackageConfigsList },

        // VSIX manifest editor...
        { "VsixEditorControl", ElementHandlers.HandleElement },
        // Tab control
        { "Microsoft.VisualStudio.PackageManifestEditor.Controls.EditorTabControl", ControlHandlers.HandleVsixManifestEditorTabs },
        // Dialogs
        { "Microsoft.VisualStudio.PackageManifestEditor.Dialogs.InstallationTargetsDialog", ControlHandlers.HandleVsixManifestEditorDialog },
        { "Microsoft.VisualStudio.PackageManifestEditor.Dialogs.AssetsDialog", ControlHandlers.HandleVsixManifestEditorDialog },
        { "Microsoft.VisualStudio.PackageManifestEditor.Dialogs.DependenciesDialog", ControlHandlers.HandleVsixManifestEditorDialog},
        { "Microsoft.VisualStudio.PackageManifestEditor.Dialogs.PrerequisitesDialog", ControlHandlers.HandleVsixManifestEditorDialog },

        // Window frame, title bar
        { "PART_Header", ElementHandlers.HandleElement },

        // Window frame, tab item
        { "Microsoft.VisualStudio.PlatformUI.Shell.Controls.DocumentTabItem", ControlHandlers.HandleTabItem },
        { "Microsoft.VisualStudio.PlatformUI.Shell.Controls.GroupControlTabItem", ControlHandlers.HandleTabItem },
        
        // Copilot window, message box
        { "ChatPrompt", ElementHandlers.HandleElement },

        // Editor window, map scroll bar buttons
        { "UpButton", ControlHandlers.HandleMapScrollBarButton },
        { "DownButton", ControlHandlers.HandleMapScrollBarButton },

        // AppxManifest editor
        { "MainTabControl", ControlHandlers.HandleAppxManifestEditor },
        { "Microsoft.VisualStudio.AppxManifestDesigner.Designer.ManifestDesignerUserControl", ElementHandlers.HandleElement },
        { "Microsoft.VisualStudio.AppxManifestDesigner.Designer.ManifestDesignerUserControlProxy", ElementHandlers.HandleElement },

        // Resource editor
        { "_resourceView", ControlHandlers.HandleResourceEditor },
        { "Microsoft.VisualStudio.ResourceExplorer.UI.ResourceGroupEditorControl", ElementHandlers.HandleElement },

        // Code coverage, column headers
        { "GridHeader", ElementHandlers.HandleElement<DataGrid> },

        // Pull Members Up, member list
        { "MemberSelectionGrid", ControlHandlers.HandlePullMemberList },

        // Document Outline ...
        // Root
        { "DocumentOutline", ControlHandlers.HandleDocumentOutline },
        // List view
        { "SymbolTree", ControlHandlers.HandleSymbolTree },
        // Designer root
        { "DocumentOutlinePaneHolder", ElementHandlers.HandleElement },

        // JSON editor, schema selector
        { "Microsoft.WebTools.Languages.Json.VS.Schema.DropdownMargin.JsonSchemaDropdown", ControlHandlers.HandleSchemaSelector },

        // Memory layout
        { "Microsoft.VisualStudio.VC.MemoryViewer.MemoryViewerControl", ElementHandlers.HandleAndClearDictionaries },

        #region Git Windows

        // Git changes window
        { "gitWindowView", GitControlHandlers.HandleGitWindowRoot },
        // Git repository window
        { "focusedWindowView", GitControlHandlers.HandleGitWindowRoot },
        // Git commit details
        { "detailsView", GitControlHandlers.HandleGitWindowRoot },
        // Git commit details container
        { "focusedDetailsContainer", GitControlHandlers.HandleGitWindowRoot },
        // Team explorer window
        { "teamExplorerFrame", GitControlHandlers.HandleGitWindowRoot },
        // New PR window
        { "createPullRequestView", GitControlHandlers.HandleGitWindowRoot },
        // Pull request window
        { "pullRequestView", GitControlHandlers.HandleGitWindowRoot },

        // Commit history
        { "historyView", GitControlHandlers.HandleCommitHistory },

        // Smth in a git window
        { "thisPageControl", ElementHandlers.HandleElement },

        // Team explorer, project selector
        { "navControl", ElementHandlers.HandleElement },

        // Git branch selector
        { "branchesList", GitControlHandlers.HandleBranchSelector },

        // Commit history list
        { "historyListView", GitControlHandlers.HandleCommitList },

        // Commit diff info
        { "pageContentViewer", GitControlHandlers.HandleCommitDiff },

        // Commit diff presenter dock buttons
        { "dockToBottomButton", ControlHandlers.HandleStyledButton },
        { "dockToRightButton", ControlHandlers.HandleStyledButton },
        { "undockButton", ControlHandlers.HandleStyledButton },
        { "maximizeMinimizeButton", ControlHandlers.HandleStyledButton },
        { "closeButton", ControlHandlers.HandleStyledButton },
        
        // Git push, pull etc. buttons
        { "actionButton", ControlHandlers.HandleStyledButton },
        { "fetchButton", ControlHandlers.HandleStyledButton },
        { "pullButton", ControlHandlers.HandleStyledButton },
        { "pushButton", ControlHandlers.HandleStyledButton },
        { "syncButton", ControlHandlers.HandleStyledButton },
        { "additionalOperationsButton", ControlHandlers.HandleStyledButton },

        // GitHub PR, overview
        { "OverviewListBox", GitControlHandlers.HandlePROverview },

        // Git changes...
        // Actions toolbar
        { "statusControl", ElementHandlers.HandleElement },
        // Create repo
        { "inactiveRepoContent", ElementHandlers.HandleElement },
        // Branches and tags
        { "sectionContainer", ElementHandlers.HandleElement }

        #endregion
    };

    private static class ControlHandlers
    {
        public static void HandleToolBar(Transparentizer transparentizer, Control control)
        {
            if (control is ToolBar bar &&             // Main window, toolbar tray
                bar.GetVisualOrLogicalParent() is not ToolBarTray { Name: "TopDockTray" })
            {
                bar.Background = bar.BorderBrush = Brushes.Transparent;
                (bar.Parent as ToolBarTray)?.Background = Brushes.Transparent;
            }
        }

        public static void HandleDialogOKButton(Transparentizer transparentizer, Control control)
        {
            if (control is Button &&
                Window.GetWindow(control) is DialogWindowBase &&
                control.GetVisualOrLogicalParent()?
                       .GetVisualOrLogicalParent() is Border footer)
            {
                transparentizer.Layer(footer);
            }
        }

        public static void HandleTextViewHost(Transparentizer transparentizer, Control control)
        {
            control.Resources["outlining.chevron.expanded.background"] =
            control.Resources["outlining.chevron.collapsed.background"] = Brushes.Transparent;

            control.LogicalChild<Grid>()?.Background = Brushes.Transparent;
        }

        public static void HandleTextView(Transparentizer transparentizer, Control control)
        {
            control.Background = Brushes.Transparent;
            control.LogicalChild<Canvas>()?.Background = Brushes.Transparent;
        }

        public static void HandlePackageConfigsList(Transparentizer transparentizer, Control control)
        {
            if (control is DataGrid grid)
            {
                grid.Background = grid.RowBackground = Brushes.Transparent;
                transparentizer.TransparentizeStyle(grid.CellStyle);
            }
        }

        public static void HandleVsixManifestEditorTabs(Transparentizer transparentizer, Control control)
        {
            if (control.VisualChild()?
                       .VisualChild<Panel>() is { Name: "HeaderPanel" } panel)
            {
                foreach (var child in panel.Children)
                {
                    if (child is TabItem tab)
                    {
                        tab.Background = Brushes.Transparent;
                    }
                }
            }
        }

        public static void HandleVsixManifestEditorDialog(Transparentizer transparentizer, Control control)
        {
            if (control.GetVisualOrLogicalParent()?
                       .GetVisualOrLogicalParent()?
                       .GetVisualOrLogicalParent()?
                       .GetVisualOrLogicalParent() is DialogWindow window)
            {
                control.Background = window.Background = Brushes.Transparent;
            }
        }

        public static void HandleTabItem(Transparentizer transparentizer, Control control)
        {
            if (control is not TabItem { DataContext: DependencyObject view } tab)
            {
                return;
            }

            tab.Background = Brushes.Transparent;
            tab.SetResourceReference(
                Control.ForegroundProperty,
                tab.IsSelected && (bool)view.GetValue(transparentizer.View_IsActiveProperty) ?
                    ThemeResourceKeys.TextOnAccentFillPrimary : ThemeResourceKeys.TextFillPrimary);

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
                    UseTransparentizer(t => HandleTabItem(t, tab));
                }
            });
            view.AddWeakPropertyChangeHandler(transparentizer.View_IsActiveProperty, (_, _) =>
            {
                if (weakTab.TryGetTarget(out TabItem tab))
                {
                    UseTransparentizer(t => HandleTabItem(t, tab));
                }
            });
        }

        public static void HandleMapScrollBarButton(Transparentizer transparentizer, Control control)
        {
            if (control is RepeatButton && control.VisualChild<Border>() is { Name: "Border" } border)
            {
                border.Background = Brushes.Transparent;
            }
        }

        public static void HandleAppxManifestEditor(Transparentizer transparentizer, Control control)
        {
            control.Background = Brushes.Transparent;

            if (control.GetVisualOrLogicalParent()?
                       .GetVisualOrLogicalParent() is Grid { Name: "LayoutRoot" } root)
            {
                root.Background = Brushes.Transparent;
            }
        }

        public static void HandleResourceEditor(Transparentizer transparentizer, Control control)
        {
            if (control is ContentControl { Content: DockPanel panel })
            {
                panel.Background = Brushes.Transparent;
            }
        }

        public static void HandlePullMemberList(Transparentizer transparentizer, Control control)
        {
            if (control is DataGrid { RowStyle: { } style } grid)
            {
                grid.RowStyle = transparentizer.SubclassStyle(style);
            }
        }

        public static void HandleDocumentOutline(Transparentizer transparentizer, Control control)
        {
            if (control.GetVisualOrLogicalParent() is not Panel adapter)
            {
                return;
            }

            transparentizer.Layer(adapter);

            if (!GetIsTracked(control))
            {
                adapter.SizeChanged += OnSizeChanged;
                SetIsTracked(control, value: true);
            }

            static void OnSizeChanged(object sender, RoutedEventArgs e)
            {
                if (sender is Panel adapter)
                {
                    // Use SetResourceReference manually to avoid UseTransparentizer just for Layer
                    adapter.SetResourceReference(Panel.BackgroundProperty, LayeredBrushKey);
                }
            }
        }

        public static void HandleSymbolTree(Transparentizer transparentizer, Control control)
        {
            if (control is TreeView)
            {
                control.VisualChild()?
                       .VisualChild()?
                       .VisualChild()?
                       .VisualChild<Rectangle>()?
                       .Fill = Brushes.Transparent;
            }
        }

        public static void HandleSchemaSelector(Transparentizer transparentizer, Control control)
        {
            control.VisualChild()?
                   .VisualChild()?
                   .VisualChild<Grid>()?
                   .Background = Brushes.Transparent;
        }

        public static void HandleStyledButton(Transparentizer transparentizer, Control control)
        {
            if (control is Button { Style: { } style })
            {
                control.Style = transparentizer.SubclassStyle(style);
            }
        }
    }

    private static class GitControlHandlers
    {
        public static void HandleGitWindowRoot(Transparentizer transparentizer, Control control)
        {
            control.Background = Brushes.Transparent;

            // Git command button style
            if (control.TryFindResource("TESectionCommandButtonStyle") is Style style)
            {
                transparentizer.TransparentizeStyle(style);
            }

            transparentizer.StyleElementTree(control, TreeType.Logical);
        }

        public static void HandleCommitHistory(Transparentizer transparentizer, Control control)
        {
            if (control.GetVisualOrLogicalParent()?
                       .GetVisualOrLogicalParent()?
                       .GetVisualOrLogicalParent() is Border history)
            {
                history.Background = Brushes.Transparent;
            }

            HandleGitWindowRoot(transparentizer, control);
        }

        public static void HandleBranchSelector(Transparentizer transparentizer, Control control)
        {
            if (control.GetVisualOrLogicalParent()?
                       .GetVisualOrLogicalParent()?
                       .GetVisualOrLogicalParent()?
                       .GetVisualOrLogicalParent() is Control branches)
            {
                branches.Background = Brushes.Transparent;
            }
        }

        public static void HandleCommitList(Transparentizer transparentizer, Control control)
        {
            if (control is ListView { View: GridView { ColumnHeaderContainerStyle: { } style } grid })
            {
                grid.ColumnHeaderContainerStyle = transparentizer.SubclassStyle(style);

                foreach (var column in grid.Columns)
                {
                    if (column.Header is not Control header)
                    {
                        continue;
                    }

                    header.ApplyTemplate();

                    if (header.VisualChild()?
                              .VisualChild<Border>() is { Name: "HeaderBorder" } border)
                    {
                        border.BorderBrush = Brushes.Transparent;
                    }
                }
            }
        }

        public static void HandleCommitDiff(Transparentizer transparentizer, Control control)
        {
            if (control.GetVisualOrLogicalParent()?
                       .GetVisualOrLogicalParent() is Border viewer)
            {
                viewer.Background = Brushes.Transparent;
            }
        }

        public static void HandlePROverview(Transparentizer transparentizer, Control control)
        {
            if (control is ListBox { ItemContainerStyle: { } style } list)
            {
                list.Background = Brushes.Transparent;
                list.ItemContainerStyle = transparentizer.SubclassStyle(style);
            }
        }
    }
}

public partial class ElementTransparentizer
{
    private static readonly ConcurrentDictionary<Type, Action<Transparentizer, Panel>> s_panelTypeHandlers = new()
    {
        [typeof(Panel)] = ElementHandlers.Noop,
        [typeof(Canvas)] = ElementHandlers.Noop,
        [typeof(Grid)] = ElementHandlers.Noop,

        [typeof(DockPanel)] = ElementHandlers.Noop,
        [typeof(StackPanel)] = ElementHandlers.Noop,
        [typeof(VirtualizingPanel)] = ElementHandlers.Noop,
        [typeof(WrapPanel)] = ElementHandlers.Noop
    };

    private static readonly Dictionary<string, Action<Transparentizer, Panel>> s_panelHandlers = new()
    {
        // Commit diff
        { "detailsViewMainGrid", PanelHandlers.HandleCommitDiff },

        // Editor window, loading placeholder
        { "StackPanel_LoadingDocumentUI", ElementHandlers.HandleElement },

        // Commit history, toolbar container
        { "toolbarGrid", ElementHandlers.HandleElement },

        // Pull request window, toolbar
        { "targetAndSourceBranchPickers", ElementHandlers.HandleElement },

        // Full-screen title bar
        { "MenuBarDockPanel", ElementHandlers.HandleElement },

        // Document Outline, designer item container
        { "SplitterGrid", PanelHandlers.HandleDocumentOutlineItemContainer },

        // Merge editor
        { "mainLayoutPanel", ElementHandlers.HandleElement<Grid> },

        // Editor window, root
        { MultiViewHostTypeName, PanelHandlers.HandleMultiViewHost },

        // Editor window, bottom container
        { "Microsoft.VisualStudio.Text.Utilities.ContainerMargin", PanelHandlers.HandleContainerMargin },

        // Editor window, left side icon container
        { "Microsoft.VisualStudio.Text.Editor.Implementation.GlyphMarginGrid", PanelHandlers.HandleGlyphMargin },

        // Scroll bar intersection
        { "Microsoft.VisualStudio.Editor.Implementation.BottomRightCornerSpacerMargin", ElementHandlers.HandleElement },

        // Editor window, collapsed item container
        { "Microsoft.VisualStudio.Text.Editor.Implementation.AdornmentLayer", PanelHandlers.HandleAdornmentLayer },

        // Editor window, map scroll bar
        { "Microsoft.VisualStudio.Text.OverviewMargin.Implementation.OverviewElement", ElementHandlers.HandleElement },
        
        // Commit diff view
        { "Microsoft.VisualStudio.Differencing.Package.DiffControl", ElementHandlers.HandleElement },

        // Memory layout, item list
        { "Microsoft.VisualStudio.VC.MemoryViewer.MemoryLayoutCanvas", PanelHandlers.HandleMemoryLayout }
    };

    private static class PanelHandlers
    {
        public static void HandleCommitDiff(Transparentizer transparentizer, Panel panel)
        {
            if (panel.GetVisualOrLogicalParent()?
                     .GetVisualOrLogicalParent() is Border details)
            {
                details.Background = Brushes.Transparent;
            }
        }

        public static void HandleDocumentOutlineItemContainer(Transparentizer transparentizer, Panel panel)
        {
            if (panel is Grid)
            {
                transparentizer.Layer(panel);

                foreach (var child in panel.Children)
                {
                    if (child is Border border)
                    {
                        border.Background = Brushes.Transparent;
                    }
                }
            }
        }

        public static void HandleMultiViewHost(Transparentizer transparentizer, Panel panel)
        {
            panel.Resources[ThemeResourceKeys.ScrollBarBackground] = Brushes.Transparent;
        }

        public static void HandleContainerMargin(Transparentizer transparentizer, Panel panel)
        {
            if (!panel.FindDescendants<Panel>().Any(p => p.GetType().FullName == "Microsoft.VisualStudio.Text.Utilities.ContainerMargin"))
            {
                transparentizer.Layer(panel);
            }
        }

        public static void HandleGlyphMargin(Transparentizer transparentizer, Panel panel)
        {
            if (panel.Background is SolidColorBrush solid && solid.Color != Colors.Transparent)
            {
                transparentizer.Layer(panel);
            }
        }

        public static void HandleAdornmentLayer(Transparentizer transparentizer, Panel panel)
        {
            var alreadyLayered = false;

            foreach (var child in panel.Children)
            {
                if (child is not Rectangle rectangle ||
                    // Check for collapsed item properties
                    (rectangle.RadiusX == 0 && rectangle.StrokeThickness > 0))
                {
                    continue;
                }

                if (alreadyLayered)
                {
                    // Avoid layering more than once
                    rectangle.Fill = Brushes.Transparent;
                }
                else
                {
                    rectangle.SetResourceReference(Shape.FillProperty, LayeredBrushKey);
                    alreadyLayered = true;
                }
            }
        }

        public static void HandleMemoryLayout(Transparentizer transparentizer, Panel panel)
        {
            if (panel.VisualChild<Line>() is null &&
                panel.GetType().GetProperty("FontBrush", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public) is { } property &&
                panel.TryFindResource(ThemeResourceKeys.SolidBackgroundFillTertiary) is Brush brush)
            {
                property.SetValue(panel, brush);
            }
        }
    }
}

public partial class ElementTransparentizer
{
    private static readonly ConcurrentDictionary<Type, Action<Transparentizer, Border>> s_borderTypeHandlers = [];

    private static readonly Dictionary<string, Action<Transparentizer, Border>> s_borderHandlers = new()
    {
        // Window frame, content area
        { "PART_ContentPanel", ElementHandlers.HandleLayeredElement },

        // Window frame, body
        { "ToolWindowBorder", ElementHandlers.HandleElement },

        // Start window, footer
        { "FooterBorder", ElementHandlers.HandleLayeredElement },

        // Git window, section header
        { "borderHeader", BorderHandlers.HandleSectionHeader },

        // Output window, root
        { "Microsoft.VisualStudio.PlatformUI.OutputWindow", ElementHandlers.HandleElement },

        // Editor window, file errors container
        { "Microsoft.VisualStudio.UI.Text.Wpf.FileHealthIndicator.Implementation.FileHealthIndicatorMargin", ElementHandlers.HandleElement },

        // Editor window, PR comment toolbar
        { "Microsoft.VisualStudio.Commenting.Presentation.Comments.Margin.CommentToolbar", ElementHandlers.HandleElement },

        // Commit diff view, toolbar
        { "Microsoft.VisualStudio.Differencing.Package.DiffControlToolbar", ElementHandlers.HandleElement },

        // List window, root
        { "Microsoft.VisualStudio.ErrorListPkg.TableControlToolWindowPaneBase+ContentWrapper", ElementHandlers.HandleAndClearDictionaries }
    };

    private static class BorderHandlers
    {
        public static void HandleSectionHeader(Transparentizer transparentizer, Border border)
        {
            if (border is { Style: { } style })
            {
                border.Style = transparentizer.SubclassStyle(style);
            }
        }
    }
}

public partial class ElementTransparentizer
{
    private static class ElementHandlers
    {
        public static void Noop(Transparentizer transparentizer, FrameworkElement element) { }

        public static void HandleElement(Transparentizer transparentizer, FrameworkElement element)
        {
            element.SetValue(Panel.BackgroundProperty, Brushes.Transparent);
        }

        public static void HandleElement<T>(Transparentizer transparentizer, FrameworkElement element)
            where T : FrameworkElement
        {
            if (element is T)
            {
                element.SetValue(Panel.BackgroundProperty, Brushes.Transparent);
            }
        }

        public static void HandleLayeredElement(Transparentizer transparentizer, FrameworkElement element)
        {
            transparentizer.Layer(element);
        }

        public static void HandleLayeredElement<T>(Transparentizer transparentizer, FrameworkElement element)
           where T : FrameworkElement
        {
            if (element is T)
            {
                transparentizer.Layer(element);
            }
        }

        public static void HandleAndClearDictionaries(Transparentizer transparentizer, FrameworkElement element)
        {
            element.Resources.MergedDictionaries.Clear();
        }
    }
}
