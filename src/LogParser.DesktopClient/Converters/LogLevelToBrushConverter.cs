using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Extensions.Logging;

namespace LogParser.DesktopClient.Converters;

public class LogLevelToBrushConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string logLevel)
        {
            if (logLevel.Equals(nameof(LogLevel.Critical), StringComparison.InvariantCultureIgnoreCase))
            {
                return new BrushConverter().ConvertFromInvariantString("#FF0061");
            }

            if (logLevel.Equals(nameof(LogLevel.Error), StringComparison.InvariantCultureIgnoreCase))
            {
                return new BrushConverter().ConvertFromInvariantString("#E06795");
            }

            if (logLevel.Equals(nameof(LogLevel.Warning), StringComparison.InvariantCultureIgnoreCase))
            {
                return new BrushConverter().ConvertFromInvariantString("#FF8C00");
            }

            if (logLevel.StartsWith(nameof(LogLevel.Information), StringComparison.InvariantCultureIgnoreCase))
            {
                return new BrushConverter().ConvertFromInvariantString("#92CAF4");
            }

            if (logLevel.StartsWith(nameof(LogLevel.Trace), StringComparison.InvariantCultureIgnoreCase))
            {
                return new BrushConverter().ConvertFromInvariantString("#C0F492");
            }
        }

        return new BrushConverter().ConvertFromInvariantString("#E6E6E6");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return DependencyProperty.UnsetValue;
    }
}
