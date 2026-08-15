using System.Windows;

namespace LogParser.WpfClient.Utilities;

internal static class VisualTreeHelper
{
    internal static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

            if (child is T typed)
            {
                return typed;
            }

            var result = FindVisualChild<T>(child);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
