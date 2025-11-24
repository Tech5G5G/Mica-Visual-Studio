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

public static class WeakReferenceExtensions
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

    #region Utilities

    /// <summary>
    /// Determines whether <paramref name="source"/> contains a <see cref="WeakReference{T}"/> to <paramref name="value"/>.
    /// </summary>
    /// <remarks>
    /// If <paramref name="source"/> is a <see cref="List{T}"/>, also removes redundant references; that is, references that no longer reference anything.
    /// </remarks>
    /// <typeparam name="T">The type of elements in <paramref name="source"/> that are weakly-referenced.</typeparam>
    /// <param name="source">A sequence in which to locate a weakly-referenced value.</param>
    /// <param name="value">The weakly-referenced value to locate in the sequence.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="source"/> contains a <see cref="WeakReference{T}"/> to <paramref name="value"/>. Otherwise, <see langword="false"/>.
    /// </returns>
    public static bool Contains<T>(this IEnumerable<WeakReference<T>> source, T value) where T : class
    {
        if (source is List<WeakReference<T>> list)
        {
            for (int i = list.Count - 1; i >= 0; --i)
                if (!list[i].TryGetTarget(out T element))
                    list.RemoveAt(i); //Remove redundant reference
                else if (element == value)
                    return true;
        }
        else
        {
            foreach (var weakElement in source)
                if (weakElement.TryGetTarget(out T element) && element == value)
                    return true;
        }

        return false;
    }

    #endregion
}
