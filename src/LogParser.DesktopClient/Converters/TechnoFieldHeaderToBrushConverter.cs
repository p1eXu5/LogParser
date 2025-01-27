using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace LogParser.DesktopClient.Converters
{
    public class TechFieldHeaderToBrushConverter : IValueConverter
    {
        private static string[] _orangeHeaders = { };
        private static string[] _greenHeaders = { "Message", "Status" };
        private static string[] _traceHeaders = { "TraceId", "SpanId", "ParentId", "HierarchicalTraceId", "LogSource" };

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s) {

                if (_orangeHeaders.Contains(s))
                {
                    return new BrushConverter().ConvertFromInvariantString("#FF8C00");
                }

                if (_greenHeaders.Contains(s)) {
                    return new BrushConverter().ConvertFromInvariantString("#87D778");
                }

                if (_traceHeaders.Contains(s)) {
                    return new BrushConverter().ConvertFromInvariantString("#E06795");
                }
            }

            return new BrushConverter().ConvertFromInvariantString("#92CAF4");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
