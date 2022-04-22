using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace LogParser.DesktopClient.Converters
{
    public class TechnoFieldHeaderConverter : IValueConverter
    {
        private static string[] _headers = { "Timespan", "Level", "Message" };
        private static string[] _traceHeaders = { "TraceId", "SpanId", "ParentId", "HierarchicalTraceId" };

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s) {
                if (_headers.Contains(s)) {
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
