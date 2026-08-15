using System;
using System.Globalization;
using System.Windows.Data;

namespace LogParser.WpfClient.Converters;

internal class IdEqualsSelectedIdConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] == null || values[1] == null) return false;
        return values[0].Equals(values[1]);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return new object[] { Binding.DoNothing, Binding.DoNothing };
    }
}