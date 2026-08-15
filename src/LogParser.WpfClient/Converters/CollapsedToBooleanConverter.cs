using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LogParser.WpfClient.Converters;

internal sealed class CollapsedToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility visibility && visibility == Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool isCollapsed && isCollapsed ? Visibility.Collapsed : Visibility.Visible;
    }
}
