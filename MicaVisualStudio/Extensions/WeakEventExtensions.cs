using System.Collections.Generic;
using System.Windows.Data;

namespace System.Windows;

// https://agsmith.wordpress.com/2008/04/07/propertydescriptor-addvaluechanged-alternative/

public class PropertyChangeNotifier : DependencyObject, IDisposable
{
    private readonly WeakReference<DependencyObject> _propertySource;

    public PropertyChangeNotifier(DependencyObject source, string path) :
        this(source, new PropertyPath(path)) { }

    public PropertyChangeNotifier(DependencyObject source, DependencyProperty property) :
        this(source, new PropertyPath(property)) { }

    public PropertyChangeNotifier(DependencyObject source, PropertyPath path)
    {
        _propertySource = new(source);

        BindingOperations.SetBinding(this, ValueProperty, new Binding
        {
            Path = path,
            Mode = BindingMode.OneWay,
            Source = source
        });
    }

    public DependencyObject PropertySource =>
        _propertySource.TryGetTarget(out DependencyObject source) ? source : null;

    public object Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register("Value", typeof(object), typeof(PropertyChangeNotifier), new(null, new(OnValueChanged)));

    public event EventHandler ValueChanged;

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        (d as PropertyChangeNotifier)?.ValueChanged?.Invoke(d, EventArgs.Empty);
    }

    public void Dispose()
    {
        BindingOperations.ClearBinding(this, ValueProperty);
    }
}

public static class WeakEventExtensions
{
    private static readonly DependencyPropertyKey NotifiersListPropertyKey =
        DependencyProperty.RegisterAttachedReadOnly(
            "NotifiersList",
            typeof(List<PropertyChangeNotifier>),
            typeof(WeakEventExtensions),
            new(defaultValue: new List<PropertyChangeNotifier>()));

    private static readonly DependencyProperty NotifiersListProperty = NotifiersListPropertyKey.DependencyProperty;

    public static void AddWeakHandler<T>(this T source, RoutedEvent routedEvent, RoutedEventHandler handler) where T : FrameworkElement
    {
        WeakEventManager<T, RoutedEventArgs>.AddHandler(source, routedEvent.Name, (s, e) => handler(s, e));
    }

    public static void AddWeakOneTimeHandler<T>(this T source, RoutedEvent routedEvent, RoutedEventHandler handler) where T : FrameworkElement
    {
        WeakEventManager<T, RoutedEventArgs>.AddHandler(source, routedEvent.Name, Handler);

        void Handler(object sender, RoutedEventArgs e)
        {
            handler(sender, e);
            WeakEventManager<T, RoutedEventArgs>.RemoveHandler(source, routedEvent.Name, Handler);
        }
    }

    public static void AddWeakPropertyChangeHandler(this DependencyObject source, DependencyProperty property, EventHandler handler)
    {
        PropertyChangeNotifier notifier = new(source, property);
        notifier.ValueChanged += (s, _) => handler((s as PropertyChangeNotifier).PropertySource, EventArgs.Empty);

        (source.GetValue(NotifiersListProperty) as List<PropertyChangeNotifier>)?.Add(notifier);
    }

    public static void AddWeakOneTimePropertyChangeHandler(this DependencyObject source, DependencyProperty property, EventHandler handler)
    {
        PropertyChangeNotifier notifier = new(source, property);
        notifier.ValueChanged += ValueChanged;

        (source.GetValue(NotifiersListProperty) as List<PropertyChangeNotifier>)?.Add(notifier);

        void ValueChanged(object sender, EventArgs e)
        {
            if (sender is PropertyChangeNotifier notifier)
            {
                handler(notifier.PropertySource, EventArgs.Empty);
                notifier.ValueChanged -= ValueChanged;

                notifier.Dispose();
                (source.GetValue(NotifiersListProperty) as List<PropertyChangeNotifier>)?.Remove(notifier);
            }
        }
    }
}
