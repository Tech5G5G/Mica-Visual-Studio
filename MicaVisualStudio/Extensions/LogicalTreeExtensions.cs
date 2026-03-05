using System.Linq;
using System.Collections.Generic;
using System.Windows;

namespace MicaVisualStudio.Extensions;

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

    public static IEnumerable<T> LogicalDescendants<T>(this DependencyObject parent) where T : DependencyObject
    {
        return LogicalDescendants(parent).OfType<T>(); 
    }
}