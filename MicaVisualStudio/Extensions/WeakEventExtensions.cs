using System.Windows.Data;

namespace MicaVisualStudio.Extensions;

/// <summary>
/// Represents a notifier that observes a <see cref="DependencyProperty"/> for changes from a specificed source.
/// </summary>
public class PropertyChangeNotifier : DependencyObject, IDisposable
{
    private readonly WeakReference<DependencyObject> propertySource;

    /// <summary>
    /// Initializes a new instance of <see cref="PropertyChangeNotifier"/>.
    /// </summary>
    /// <param name="source">The source from which property changes occur.</param>
    /// <param name="path">A path to the property to observe.</param>
    public PropertyChangeNotifier(DependencyObject source, string path) :
        this(source, new PropertyPath(path)) { }

    /// <summary>
    /// Initializes a new instance of <see cref="PropertyChangeNotifier"/>.
    /// </summary>
    /// <param name="source">The source from which property changes occur.</param>
    /// <param name="property">The <see cref="DependencyProperty"/> to observe.</param>
    public PropertyChangeNotifier(DependencyObject source, DependencyProperty property) :
        this(source, new PropertyPath(property)) { }

    /// <summary>
    /// Initializes a new instance of <see cref="PropertyChangeNotifier"/>.
    /// </summary>
    /// <param name="source">The source from which property changes occur.</param>
    /// <param name="path">A path to the property to observe.</param>
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
    /// If the source is unavailable, this returns <see langword="null"/>.
    /// </remarks>
    public DependencyObject PropertySource =>
        propertySource.TryGetTarget(out DependencyObject source) ? source : null;

    /// <summary>
    /// Gets or sets the value of the property.
    /// </summary>
    public object Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="Value"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register("Value", typeof(object), typeof(PropertyChangeNotifier), new(null, new(OnValueChanged)));

    /// <summary>
    /// Occurs when the value of the specified <see cref="DependencyProperty"/> has changed.
    /// </summary>
    public event EventHandler ValueChanged;

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        (d as PropertyChangeNotifier)?.ValueChanged?.Invoke(d, EventArgs.Empty);

    /// <summary>
    /// Clears the internal binding used by this <see cref="PropertyChangeNotifier"/>.
    /// </summary>
    public void Dispose() =>
        BindingOperations.ClearBinding(this, ValueProperty);
}

/// <summary>
/// Contains extensions for adding weak handlers to <see cref="RoutedEvent"/>s and property changes.
/// </summary>
public static class WeakEventExtensions
{
    #region Events

    /// <summary>
    /// Adds the specified <paramref name="handler"/> to the specified <paramref name="routedEvent"/> using <see cref="WeakEventManager"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="FrameworkElement"/>-derived type of <paramref name="source"/>.</typeparam>
    /// <param name="source">The source to attach the <paramref name="handler"/> to.</param>
    /// <param name="routedEvent">The <see cref="RoutedEvent"/> to attach the <paramref name="handler"/> to.</param>
    /// <param name="handler">The <see cref="RoutedEventHandler"/> that recieves events.</param>
    public static void AddWeakHandler<T>(this T source, RoutedEvent routedEvent, RoutedEventHandler handler) where T : FrameworkElement =>
        WeakEventManager<T, RoutedEventArgs>.AddHandler(source, routedEvent.Name, (s, e) => handler(s, e));


    /// <summary>
    /// Adds the specified <paramref name="handler"/> to the specified <paramref name="routedEvent"/> using <see cref="WeakEventManager"/>.
    /// </summary>
    /// <remarks>Once the <paramref name="handler"/> is invoked, it is removed from the specified <paramref name="routedEvent"/>.</remarks>
    /// <typeparam name="T">The <see cref="FrameworkElement"/>-derived type of <paramref name="source"/>.</typeparam>
    /// <param name="source">The source to attach the <paramref name="handler"/> to.</param>
    /// <param name="routedEvent">The <see cref="RoutedEvent"/> to attach the <paramref name="handler"/> to.</param>
    /// <param name="handler">The <see cref="RoutedEventHandler"/> that recieves events.</param>
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

    /// <summary>
    /// Adds a <see cref="PropertyChangeNotifier"/> to the specified <paramref name="property"/>.
    /// </summary>
    /// <param name="source">The <see cref="DependencyObject"/> from which property changes occur.</param>
    /// <param name="property">The <see cref="DependencyProperty"/> observed by <see cref="PropertyChangeNotifier"/>.</param>
    /// <param name="handler">The <see cref="EventHandler"/> to invoke once a property change occurs.</param>
    public static void AddWeakPropertyChangeHandler(this DependencyObject source, DependencyProperty property, EventHandler handler)
    {
        PropertyChangeNotifier notifier = new(source, property);
        notifier.ValueChanged += (s, e) => handler((s as PropertyChangeNotifier).PropertySource, EventArgs.Empty);
    }

    /// <summary>
    /// Adds a <see cref="PropertyChangeNotifier"/> to the specified <paramref name="property"/>.
    /// </summary>
    /// <remarks>Once the <paramref name="handler"/> is invoked, the <see cref="PropertyChangeNotifier"/> is disposed.</remarks>
    /// <param name="source">The <see cref="DependencyObject"/> from which property changes occur.</param>
    /// <param name="property">The <see cref="DependencyProperty"/> observed by <see cref="PropertyChangeNotifier"/>.</param>
    /// <param name="handler">The <see cref="EventHandler"/> to invoke once a property change occurs.</param>
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
