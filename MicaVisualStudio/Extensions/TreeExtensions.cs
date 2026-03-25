using System.Windows;
using System.Windows.Media;

namespace MicaVisualStudio.Extensions;

public static class VisualTreeExtensions
{
    public static Visual VisualChild(this Visual parent)
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; ++i)
        {
            if (VisualTreeHelper.GetChild(parent, i) is Visual child)
            {
                return child;
            }
        }

        return null;
    }

    public static T VisualChild<T>(this Visual parent) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; ++i)
        {
            if (VisualTreeHelper.GetChild(parent, i) is T child)
            {
                return child;
            }
        }

        return null;
    }
}

public static class LogicalTreeExtensions
{
    public static T LogicalChild<T>(this DependencyObject parent) where T : DependencyObject
    {
        foreach (var child in LogicalTreeHelper.GetChildren(parent))
        {
            if (child is T value)
            {
                return value;
            }
        }

        return null;
    }
}