using System;
using System.Globalization;
using System.Windows.Data;

namespace LogParser.WpfClient.Converters;

internal sealed class StringExistanceToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => !string.IsNullOrWhiteSpace(value as string);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
