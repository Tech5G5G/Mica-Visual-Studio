using System.Reflection;
using System.Windows.Shapes;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Expression = System.Linq.Expressions.Expression;

namespace MicaVisualStudio.VisualStudio;

//This code is bad, but it works, so...
public sealed class VsWindowStyler : IVsWindowFrameEvents, IDisposable
{
    public static VsWindowStyler Instance { get; } = new();

    #region Keys

    private const string SolidBackgroundFillTertiaryLayeredKey = "VsBrush.SolidBackgroundFillTertiaryLayered";

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

    private readonly uint cookie;

    #endregion

    #region Dependency Properties

    private readonly Func<IVsWindowFrame, DependencyObject> get_WindowFrame_FrameView;
    private readonly Func<DependencyObject, object> get_View_Content;
    private readonly Func<DependencyObject, bool> get_View_IsActive;
    private readonly Func<object, bool> IsDockTarget;

    private readonly DependencyProperty View_ContentProperty,
        View_IsActiveProperty;

    #endregion

    private readonly ILHook hook;

    private readonly List<WeakReference<TabItem>> tabItems = [];
    private readonly List<WeakReference<FrameworkElement>> elements = [];

    private VsWindowStyler()
    {
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

        #region Layered Brush

        AddLayeredBrush();
        (Application.Current.Resources.MergedDictionaries as INotifyCollectionChanged).CollectionChanged += (s, e) => AddLayeredBrush();

        void AddLayeredBrush()
        {
            var color = shell5?.GetThemedWPFColor(SolidBackgroundFillTertiaryKey) ?? default;
            color.A = 0xFF / 2; //50% opacity

            SolidColorBrush brush = new(color);
            foreach (var dictionary in Application.Current.Resources.MergedDictionaries.OfType<DeferredResourceDictionaryBase>())
                if (!dictionary.Contains(SolidBackgroundFillTertiaryLayeredKey))
                    dictionary.Add(SolidBackgroundFillTertiaryLayeredKey, brush);
        }

        #endregion

        #region Listeners

#pragma warning disable VSTHRD010 //Invoke single-threaded types on Main thread
        cookie = shell7.AdviseWindowFrameEvents(this);
#pragma warning restore VSTHRD010 //Invoke single-threaded types on Main thread

        EventManager.RegisterClassHandler(
            dockType,
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((s, e) => ApplyToDockTarget(s as Border)));

        WindowManager.Instance.WindowOpened += (s, e) =>
        {
            if (s is not null)
                ApplyToWindow(s);
        };

        hook = new(typeof(Visual).GetMethod("AddVisualChild", BindingFlags.Instance | BindingFlags.NonPublic), context =>
            {
            ILCursor cursor = new(context) { Index = 0 };

            cursor.Emit(OpCodes.Ldarg_0); //this (Visual)
            cursor.Emit(OpCodes.Ldarg_1); //child

            cursor.EmitDelegate(VisualChildAdded);
        });

        static void VisualChildAdded(Visual instance, Visual child)
        {
            if (instance is ContentPresenter or Decorator or Panel && //Avoid other types
                instance is FrameworkElement content &&
                Instance is VsWindowStyler styler && styler.elements.Contains(content))
                styler.ApplyToContent(content, applyToDock: false);
        }

        #endregion

        ApplyToAllWindows();
        ApplyToAllWindowPanesAsync().Forget();
    }

    #region Apply To All

    private void ApplyToAllWindows() => WindowManager.AllWindows.ForEach(ApplyToWindow);

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
        var descendants = window.FindDescendants<FrameworkElement>();

        //Footer
        descendants.FindElement<Border>("FooterBorder")?
                   .SetResourceReference(Border.BackgroundProperty, SolidBackgroundFillTertiaryLayeredKey);

        //Warning dialog
        if (window is DialogWindowBase &&
            descendants.FindElement<Button>("OKButton") is Button button &&
            button.FindAncestor<FrameworkElement>() is FrameworkElement parent &&
            parent.FindAncestor<Border>() is Border buttonFooter)
            buttonFooter.SetResourceReference(Border.BackgroundProperty, SolidBackgroundFillTertiaryLayeredKey);

        foreach (var descendant in descendants)
            if (descendant is Border border && IsDockTarget(border))
                ApplyToDockTarget(border);
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

                if (tabItems.Contains(tab))
                    continue;

                WeakReference<TabItem> weakTab = new(tab);
                tabItems.Add(weakTab);

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
            if (element is ContentPresenter or Decorator or Panel && !elements.Contains(element))
                elements.Add(new(element)); //Track visual children

            if (element is ToolBar bar)
        {
                bar.Background = bar.BorderBrush = Brushes.Transparent;
            (bar.Parent as ToolBarTray)?.Background = Brushes.Transparent;
        }
            else if (element is Control control)
                switch (element.Name)
                {
                    //Git changes window
                    case "gitWindowView":
                        control.Background = Brushes.Transparent;

                        foreach (var e in control.LogicalDescendants<FrameworkElement>())
                            if (e is Control c &&
                                (c.Name == "statusControl" || //Actions/tool bar
                                c.Name == "thisPageControl" || //Changes
                                c.Name == "inactiveRepoContent")) //Create repo
                                c.Background = Brushes.Transparent;
                        break;

                    //Host of WpfTextView I guess
                    case "WpfTextViewHost":
                        control.Resources["outlining.chevron.expanded.background"] =
                        control.Resources["outlining.chevron.collapsed.background"] = Brushes.Transparent;
                        break;

                    //Document, output, etc. text
                    case "WpfTextView" when element is ContentControl:
                        control.Background = Brushes.Transparent;
                        control.FindDescendant<Canvas>()?.Background = Brushes.Transparent;
                        break;
                }
            else if (element is Panel panel)
            {
                if (element is DockPanel)
                    panel.Background = Brushes.Transparent;

                switch (panel.GetType().FullName)
                {
                    //Editor window, root
                    case "Microsoft.VisualStudio.Editor.Implementation.WpfMultiViewHost":
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
                        if (panel is Grid { Background: not null } && panel.FindAncestor<Control>() is not Button or TextBox)
                            panel.Background = Brushes.Transparent;
                        break;
                }
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

    public void Dispose()
    {
        if (!disposed)
        {
            hook?.Dispose();

#pragma warning disable VSTHRD010 //Invoke single-threaded types on Main thread
            shell7.UnadviseWindowFrameEvents(cookie);
#pragma warning restore VSTHRD010 //Invoke single-threaded types on Main thread

            disposed = true;
        }
    }

    #endregion
}
