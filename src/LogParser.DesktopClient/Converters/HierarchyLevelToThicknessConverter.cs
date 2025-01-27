using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LogParser.DesktopClient.Converters;

public class HierarchyLevelToThicknessConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int hierarchyLevel)
        {
            if (hierarchyLevel <= 0)
            {
                return new Thickness(0);
            }

            return new Thickness(10 * hierarchyLevel, 0, 0, 0);
        }

        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return DependencyProperty.UnsetValue;
    }
}
