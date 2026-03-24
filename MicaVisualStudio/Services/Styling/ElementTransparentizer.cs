using System;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Controls;
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

public sealed partial class ElementTransparentizer : IElementTransparentizer, IDisposable
{
    private const string DocOutlineWindowClassName = "VsDocOutlineTool",
                         MultiViewHostTypeName = "Microsoft.VisualStudio.Editor.Implementation.WpfMultiViewHost";

    private const string LayeredBrushKey = "VsBrush.SolidBackgroundFillTertiaryLayered";

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
        resource.CustomResources.Add(LayeredBrushKey, new(ThemeResourceKeys.SolidBackgroundFillTertiary, (_, c) =>
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
            new RoutedEventHandler(static (s, _) => UseTransparentizer(t => t.StyleElementTree(s as Border, TreeType.Visual))));

        if (AppDomain.CurrentDomain.GetAssemblies()
                                   .FirstOrDefault(a => a.GetName().Name == "Microsoft.VisualStudio.Editor.Implementation")?
                                   .GetType(MultiViewHostTypeName) is Type hostType)
        {
            EventManager.RegisterClassHandler(
                hostType,
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(static (s, _) => UseTransparentizer(t => t.StyleElementTree(s as DockPanel, TreeType.Visual))));
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
        // Skip untracked types
        if (instance is ContentControl or ContentPresenter or Decorator or Panel)
        {
            UseTransparentizer(Action);
        }

        void Action(ElementTransparentizer transparentizer)
            {
            if (instance is FrameworkElement element && GetIsTracked(element) &&
                child is FrameworkElement childElement)
            {
                transparentizer.StyleProcedure(element);
                transparentizer.StyleElementTree(childElement, TreeType.Visual);
            }
    }
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
        try
        {
        if (PInvoke.GetClassName(PInvoke.GetOwner(e.WindowHandle)) == DocOutlineWindowClassName)
        {
            StyleHwnd(e.WindowHandle);
        }
    }
        catch (Exception ex)
        {
            _logger.Output(ex);
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
            StyleElementTree(window, TreeType.Visual);
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
            StyleElementTree(dock, TreeType.Visual);
        }
        else if (!host.IsLoaded)
        {
            host.AddWeakOneTimeHandler(FrameworkElement.LoadedEvent, static (s, _) =>
            {
                UseTransparentizer(t =>
                {
                    if ((s as Border)?.FindAncestor<DependencyObject>(o => o.GetVisualOrLogicalParent(), t.IsDockTarget) is Border dock)
                    {
                        t.StyleElementTree(dock, TreeType.Visual);
                    }
                });
            });
        }
    }

    public void StyleHwnd(nint handle)
    {
        if (HwndSource.FromHwnd(handle) is { RootVisual: FrameworkElement element })
        {
            StyleElementTree(element, TreeType.Visual);
            return;
        }

        var children = PInvoke.GetChildren(handle);
        if (PInvoke.GetClassName(handle) == "Static" && children.Count() == 0)
        {
            return;
        }

        var layer = _layeredWindows;
        for (int i = 0; i < children.Count; ++i)
        {
            var child = children[i];

            if (HwndSource.FromHwnd(child) is { RootVisual: FrameworkElement childElement })
            {
                StyleElementTree(childElement, TreeType.Visual);
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

    public void StyleElementTree(FrameworkElement element, TreeType type)
    {
        if (type == TreeType.Visual)
        {
            StyleProcedure(element);

            var count = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < count; ++i)
            {
                if (VisualTreeHelper.GetChild(element, i) is FrameworkElement child)
                {
                    StyleElementTree(child, TreeType.Visual);
                }
            }
        }
        else
        {
            foreach (var child in LogicalTreeHelper.GetChildren(element))
            {
                if (child is FrameworkElement childElement)
                {
                    StyleProcedure(childElement);
                    StyleElementTree(childElement, TreeType.Logical);
                }
            }
        }
    }

    private void StyleProcedure(FrameworkElement element)
    {
        switch (element)
        {
            case Control control:
                StyleControl(control);
                break;

            case Panel panel:
                StylePanel(panel);
                SetIsTracked(element, value: true);
                return;

            case Border border:
                if (IsDockTarget(border))
                {
                    StyleDockTarget(border);
                }
                else
                {
                    StyleBorder(border);
                }
                SetIsTracked(element, value: true);
                return;

            case HwndHost host:
                StyleHwndHost(host);
                return;
        }

        if (element is ContentControl or ContentPresenter or Decorator)
        {
            // Track visual children
            SetIsTracked(element, value: true);
        }
    }

    private void StyleBackgroundElement<T>(
        T element,
        Dictionary<string, Action<ElementTransparentizer, T>> handlers,
        ConcurrentDictionary<Type, Action<ElementTransparentizer, T>> typeHandlers)
        where T : FrameworkElement
    {
        // Check for action using name...
        if (handlers.TryGetValue(element.Name, out var nameAction))
        {
            nameAction(this, element);
            return;
        }

        // and fallback to type checking if none found
        var type = element.GetType();

        // Check type itself first...
        if (typeHandlers.TryGetValue(type, out var typeAction))
        {
            typeAction(this, element);
        }
        // but check using type name if necessary...
        else if (handlers.TryGetValue(type.FullName, out var typeNameAction))
        {
            typeNameAction(this, element);
            // and cache action for type to avoid using Type.FullName again
            typeHandlers[type] = typeNameAction;
        }
    }

    public void StyleControl(Control control)
    {
        StyleBackgroundElement(control, s_controlHandlers, s_controlTypeHandlers);
    }

    public void StylePanel(Panel panel)
    {
        StyleBackgroundElement(panel, s_panelHandlers, s_panelTypeHandlers);
    }

    public void StyleBorder(Border border)
    {
        StyleBackgroundElement(border, s_borderHandlers, s_borderTypeHandlers);
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

    public void StyleHwndHost(HwndHost host)
    {
        var handle = host.Handle;
        if (handle != IntPtr.Zero)
        {
            StyleHwnd(handle);
        }
    }

    public void Layer(FrameworkElement element)
                {
        element.SetResourceReference(Panel.BackgroundProperty, LayeredBrushKey);
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
