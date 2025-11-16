using System.Reflection;
using MonoMod.RuntimeDetour;
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

    private readonly Hook hook;
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
        var borderParam = Expression.Parameter(typeof(object));
        IsDockTarget = borderParam.TypeIs(dockType)
                                  .Compile<object, bool>(borderParam);

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

        hook = new(
            typeof(Visual).GetMethod("AddVisualChild", BindingFlags.Instance | BindingFlags.NonPublic),
            new Action<Action<Visual, Visual>, Visual, Visual>((orig, instance, child) =>
            {
                orig(instance, child); //Invoke original method

                elements.RemoveAll(i => !i.TryGetTarget(out _)); //Remove redundant references
                if (instance is FrameworkElement content &&
                    elements.Any(i => i.TryGetTarget(out FrameworkElement element) && element == content))
                    ApplyToContent(content, applyToDock: false);
            }));

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

                WeakReference<TabItem> weakTab = new(tab);

                ApplyTabForeground(tab, view);
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

        var descendants = content.FindDescendants<FrameworkElement>().Append(content);
        if (!descendants.Any())
            return;

        elements.RemoveAll(i => !i.TryGetTarget(out _)); //Remove redundant references
        foreach (var element in descendants.Where(i => i is ContentPresenter || i is Decorator || i is Panel))
            if (!elements.Any(i => i.TryGetTarget(out FrameworkElement e) && e == element))
                elements.Add(new(element));

        if (descendants.FindElement<ToolBar>(string.Empty) is ToolBar bar) //Tool bar
        {
            bar.BorderBrush = bar.Background = Brushes.Transparent;
            (bar.Parent as ToolBarTray)?.Background = Brushes.Transparent;
        }

        if (descendants.FindElement<Control>("gitWindowView") is Control gitWindow) //BONUS: Git changes window
        {
            gitWindow.Background = Brushes.Transparent;

            foreach (var control in gitWindow.LogicalDescendants<Control>())
                if (control is not Button && control is not TextBox)
                    control.Background = Brushes.Transparent;
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
