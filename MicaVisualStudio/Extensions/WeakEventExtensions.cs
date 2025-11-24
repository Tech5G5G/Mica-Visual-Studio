using System.Windows.Data;

namespace MicaVisualStudio.Extensions;

public class PropertyChangeNotifier : DependencyObject, IDisposable
{
    private readonly WeakReference<DependencyObject> propertySource;

    public PropertyChangeNotifier(DependencyObject source, string path) :
        this(source, new PropertyPath(path)) { }

    public PropertyChangeNotifier(DependencyObject source, DependencyProperty property) :
        this(source, new PropertyPath(property)) { }

    public PropertyChangeNotifier(DependencyObject source, PropertyPath path)
    {
        propertySource = new(source);

        BindingOperations.SetBinding(this, ValueProperty, new Binding
        {
            Path = path,
            Mode = BindingMode.OneWay,
            Source = source
        });
    }

    /// <summary>
    /// Gets the source from which property changes occur.
    /// </summary>
    /// <remarks>
    /// If the source is unavailable, this gets <see langword="null"/>
    /// </remarks>
    public DependencyObject PropertySource =>
        propertySource.TryGetTarget(out DependencyObject source) ? source : null;

    /// <summary>
    /// Gets or set the value of the property.
    /// </summary>
    public object Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register("Value", typeof(object), typeof(PropertyChangeNotifier), new(null, new(OnValueChanged)));

    public event EventHandler ValueChanged;

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        (d as PropertyChangeNotifier)?.ValueChanged?.Invoke(d, EventArgs.Empty);

    public void Dispose() =>
        BindingOperations.ClearBinding(this, ValueProperty);
}

public static class WeakEventExtensions
{
    #region Events

    public static void AddWeakHandler<T>(this T source, RoutedEvent routedEvent, RoutedEventHandler handler) where T : FrameworkElement =>
        WeakEventManager<T, RoutedEventArgs>.AddHandler(source, routedEvent.Name, (s, e) => handler(s, e));

    public static void AddWeakOneTimeHandler<T>(this T source, RoutedEvent routedEvent, RoutedEventHandler handler) where T : FrameworkElement
    {
        WeakEventManager<T, RoutedEventArgs>.AddHandler(source, routedEvent.Name, Handler);

        void Handler(object sender, RoutedEventArgs args)
        {
            handler(sender, args);
            WeakEventManager<T, RoutedEventArgs>.RemoveHandler(source, routedEvent.Name, Handler);
        }
    }

    #endregion

    #region Properties

    public static void AddWeakPropertyChangeHandler(this DependencyObject source, DependencyProperty property, EventHandler handler)
    {
        PropertyChangeNotifier notifier = new(source, property);
        notifier.ValueChanged += (s, e) => handler((s as PropertyChangeNotifier).PropertySource, EventArgs.Empty);
    }

    public static void AddWeakOneTimePropertyChangeHandler(this DependencyObject source, DependencyProperty property, EventHandler handler)
    {
        PropertyChangeNotifier notifier = new(source, property);
        notifier.ValueChanged += ValueChanged;

        void ValueChanged(object sender, EventArgs args)
        {
            if (sender is PropertyChangeNotifier notifier)
            {
                handler(notifier.PropertySource, EventArgs.Empty);
                notifier.Dispose();
            }
        }
    }

    #endregion
}
