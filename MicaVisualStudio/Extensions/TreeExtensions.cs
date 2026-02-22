using System.Linq;
using System.Collections.Generic;
using System.Windows;

namespace MicaVisualStudio.Extensions;

/// <summary>
/// Contains extensions for traversing the WPF element tree.
/// </summary>
public static class TreeExtensions
{
    #region Logical

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
    public static IEnumerable<T> LogicalDescendants<T>(this DependencyObject parent) where T : DependencyObject =>
        LogicalDescendants(parent).OfType<T>();

    #endregion

    #region Utilities

    /// <summary>
    /// Finds the first <typeparamref name="T"/> in <paramref name="source"/> whose <see cref="FrameworkElement.Name"/> is <paramref name="name"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="FrameworkElement"/>-derived type of the element to find.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> of <see cref="FrameworkElement"/>s to filter through.</param>
    /// <param name="name">The <see cref="FrameworkElement.Name"/> to check for.</param>
    /// <returns>
    /// The first <typeparamref name="T"/> whose <see cref="FrameworkElement.Name"/> is <paramref name="name"/>.
    /// If none is found, <see langword="null"/>.
    /// </returns>
    public static T FindElement<T>(this IEnumerable<FrameworkElement> source, string name) where T : FrameworkElement =>
        source.FirstOrDefault(i => i is T element && element.Name == name) as T;

    #endregion
}