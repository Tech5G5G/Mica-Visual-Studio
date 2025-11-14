using System.Reflection;
using System.Collections.Specialized;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Expression = System.Linq.Expressions.Expression;

namespace MicaVisualStudio.VisualStudio;

//This code is bad, but it works, so...
public class VsWindowStyler : IVsWindowFrameEvents
{
    public static VsWindowStyler Instance { get; } = new();

    private const string SolidBackgroundFillTertiaryLayeredKey = "VsBrush.SolidBackgroundFillTertiaryLayered";

    private readonly ThemeResourceKey SolidBackgroundFillTertiaryKey =
        new(category: new("73708ded-2d56-4aad-b8eb-73b20d3f4bff"), name: "SolidBackgroundFillTertiary", ThemeResourceKeyType.BackgroundColor);

    private readonly IVsUIShell shell = VS.GetRequiredService<SVsUIShell, IVsUIShell>();
    private readonly IVsUIShell5 shell5 = VS.GetRequiredService<SVsUIShell, IVsUIShell5>();

    private readonly Func<IVsWindowFrame, DependencyObject> get_WindowFrame_FrameView;
    private readonly Func<DependencyObject, object> get_View_Content;
    private readonly Func<object, bool> isDockTarget;

    private readonly DependencyProperty viewContentProperty;

    private readonly HashSet<WeakReference<ContentPresenter>> presenters = [];

    private VsWindowStyler()
    {
#pragma warning disable VSTHRD010 //Invoke single-threaded types on Main thread
        VS.GetRequiredService<SVsUIShell, IVsUIShell7>().AdviseWindowFrameEvents(this);
#pragma warning restore VSTHRD010 //Invoke single-threaded types on Main thread

        #region Functions Initialization

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

        viewContentProperty = viewType.GetField("ContentProperty", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                                      .GetValue(null) as DependencyProperty;

        var dockType = Type.GetType("Microsoft.VisualStudio.PlatformUI.Shell.Controls.DockTarget, Microsoft.VisualStudio.Shell.ViewManager");
        var borderParam = Expression.Parameter(typeof(object));
        isDockTarget = borderParam.TypeIs(dockType)
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

            foreach (var dictionary in Application.Current.Resources.MergedDictionaries.Where(i => i is DeferredResourceDictionaryBase))
                if (!dictionary.Contains(SolidBackgroundFillTertiaryLayeredKey))
                    dictionary.Add(SolidBackgroundFillTertiaryLayeredKey, brush);
        }

        #endregion

        #region Listeners

        EventManager.RegisterClassHandler(
            dockType,
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((s, e) => ApplyToDockTarget(s as Border)));

        WindowManager.Instance.WindowOpened += (s, e) =>
        {
            if (s is not null)
                ApplyToWindow(s);
        };

        #endregion

        ApplyToAllWindows();
        ApplyToAllWindowPanesAsync().Forget();
    }

    #region Apply To All

    private void ApplyToAllWindows() => Application.Current.Windows.Cast<Window>()
                                                                   .ToList()
                                                                   .ForEach(ApplyToWindow);

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
        ThreadHelper.ThrowIfNotOnUIThread();

        if (get_WindowFrame_FrameView(frame) is not DependencyObject view)
            return;

        if (get_View_Content(view) is not Grid host)
        {
            WeakReference<IVsWindowFrame> weakFrame = new(frame);

            view.AddWeakOneTimePropertyChangeHandler(viewContentProperty, (s, e) =>
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

        if (descendants.FindElement<Border>("FooterBorder") is Border footer)
            footer.SetResourceReference(Border.BackgroundProperty, SolidBackgroundFillTertiaryLayeredKey);

        if (window is DialogWindowBase &&
            descendants.FindElement<Button>("OKButton") is Button button &&
            button.FindAncestor<FrameworkElement>() is FrameworkElement parent &&
            parent.FindAncestor<Border>() is Border buttonFooter)
            buttonFooter.SetResourceReference(Border.BackgroundProperty, SolidBackgroundFillTertiaryLayeredKey);

        foreach (var descendant in descendants)
            if (descendant is Border border && isDockTarget(border))
                ApplyToDockTarget(border);
    }

    private void ApplyToDockTarget(Border dock, bool applyToContent = true)
    {
        if (dock.Name == "ViewFrameTarget")
            dock.SetResourceReference(Border.BackgroundProperty, SolidBackgroundFillTertiaryLayeredKey);
        else
            dock.Background = Brushes.Transparent; //Smoke layer underneath tabs

        var descendants = dock.FindDescendants<FrameworkElement>();

        if (descendants.FindElement<Border>("PART_ContentPanel") is Border panel) //Content area
            panel.SetResourceReference(Border.BackgroundProperty, SolidBackgroundFillTertiaryLayeredKey);

        if (descendants.FindElement<Control>("PART_Header") is Control header) //Title bar
            header.Background = Brushes.Transparent;

        if (descendants.FindElement<Border>("ToolWindowBorder") is Border border) //Body
        {
            border.Background = Brushes.Transparent;

            if (applyToContent && border.FindDescendant<Grid>() is Grid host)
                ApplyToContent(host, applyToDock: false);
        }

        if (descendants.FindElement<Panel>("PART_TabPanel") is Panel tabs) //Tab strip
            foreach (var tab in tabs.Children.OfType<TabItem>()) //Tab items
                tab.Background = Brushes.Transparent;
    }

    private void ApplyToContent(FrameworkElement content, bool applyToDock = true)
    {
        if (!content.IsLoaded)
            content.AddWeakOneTimeHandler(FrameworkElement.LoadedEvent, (s, e) => ApplyToContent(s as FrameworkElement, applyToDock));

        if (applyToDock && content.FindAncestor<DependencyObject>(i => i.GetVisualOrLogicalParent(), x => isDockTarget(x)) is Border dock)
            ApplyToDockTarget(dock, applyToContent: false);

        var descendants = content.FindDescendants<FrameworkElement>().Append(content);
        if (!descendants.Any())
            return;

        presenters.RemoveWhere(i => !i.TryGetTarget(out _)); //Remove redundant references
        foreach (var presenter in descendants.OfType<ContentPresenter>())
        {
            if (presenters.Any(i => i.TryGetTarget(out ContentPresenter p) && p == presenter))
                continue;

            presenter.AddWeakPropertyChangeHandler(ContentPresenter.ContentProperty, (s, e) =>
            {
                if (s is ContentPresenter presenter &&
                    presenter.Content is FrameworkElement element)
                    ApplyToContent(element, applyToDock: false);
            });

            presenters.Add(new(presenter));
        }

        if (descendants.FindElement<ToolBar>(string.Empty) is ToolBar bar) //Tool bar
        {
            bar.BorderBrush = bar.Background = Brushes.Transparent;

            if (bar.Parent is ToolBarTray tray)
                tray.Background = Brushes.Transparent;
        }

        if (descendants.FindElement<Control>("gitWindowView") is Control gitWindow) //BONUS: Git changes window
        {
            gitWindow.Background = Brushes.Transparent;

            foreach (var control in gitWindow.LogicalDescendants<Control>())
            {
                if (control is not Button && control is not TextBox)
                    control.Background = Brushes.Transparent;
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

    public void OnActiveFrameChanged(IVsWindowFrame oldFrame, IVsWindowFrame newFrame) => ApplyToWindowFrame(newFrame);

    #endregion
}
