using System.Windows;
using System.Windows.Controls;

namespace LogParser.DesktopClient.TemplateSelectors;

public class LogDataTemplateSelector : DataTemplateSelector
{
    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        //получаем вызывающий контейнер
        FrameworkElement? element = container as FrameworkElement;

        if (element != null && item != null)
        {
            if (((dynamic)item).IsTechLog)
                return element.FindResource("dt_TechLog") as DataTemplate;

            return element.FindResource("dt_TextLog") as DataTemplate;
        }
        return null;
    }
}
