using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MicaVisualStudio.UI.Converters;

public class EnumToInt32Converter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Enum constant ? System.Convert.ToInt32(constant) : DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Enum.ToObject(targetType, value);
    }
}
