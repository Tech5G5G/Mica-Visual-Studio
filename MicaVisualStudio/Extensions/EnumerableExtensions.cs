namespace MicaVisualStudio.Extensions;

public static class EnumerableExtensions
{
    public static T FindElement<T>(this IEnumerable<FrameworkElement> source, string name) where T : FrameworkElement =>
        source.FirstOrDefault(i => i is T element && element.Name == name) as T;

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
}
