namespace MicaVisualStudio.Extensions;

public static class LogicalTreeExtensions
{
    public static IEnumerable<DependencyObject> LogicalDescendants(this DependencyObject parent)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
        {
            yield return child;

            foreach (var obj in LogicalDescendants(child))
                yield return obj;
        }
    }

    public static IEnumerable<T> LogicalDescendants<T>(this DependencyObject parent) where T : DependencyObject =>
        LogicalDescendants(parent).OfType<T>();
}