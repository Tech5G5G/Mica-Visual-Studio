using System.Linq;
using System.Collections.Generic;
using System.Windows;

namespace MicaVisualStudio.Extensions;

/// <summary>
/// Contains extensions for traversing the logical element tree.
/// </summary>
public static class LogicalTreeExtensions
{
    public static T LogicalDescendant<T>(this DependencyObject parent) where T : DependencyObject
    {
        var children = LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>().ToArray();

        foreach (var child in children)
        {
            if (child is T value)
            {
                return value;
            }
        }

        foreach (var child in children)
        {
            if (child.LogicalDescendant<T>() is T value)
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the logical descendants of <paramref name="parent"/> using <see cref="LogicalTreeHelper"/>.
    /// </summary>
    /// <param name="parent">The parent of the children to get.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="DependencyObject"/>s containing the logical descendants of <paramref name="parent"/>.</returns>
    public static IEnumerable<DependencyObject> LogicalDescendants(this DependencyObject parent)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
        {
            yield return child;

            foreach (var obj in LogicalDescendants(child))
            {
                yield return obj;
            }
        }
    }

    /// <summary>
    /// Gets the logical descendants of <paramref name="parent"/> using <see cref="LogicalTreeHelper"/>.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="DependencyObject"/>s to filter.</typeparam>
    /// <param name="parent">The parent of the children to get.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of <typeparamref name="T"/> containing the logical descendants of <paramref name="parent"/>.</returns>
    public static IEnumerable<T> LogicalDescendants<T>(this DependencyObject parent) where T : DependencyObject
    {
        return LogicalDescendants(parent).OfType<T>(); 
    }
}