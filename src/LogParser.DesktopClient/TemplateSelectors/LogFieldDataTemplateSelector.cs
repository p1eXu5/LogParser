using System.Windows;
using System.Windows.Controls;
using static LogParser.DesktopClient.ElmishApp.Models.TechFieldModelModule;

namespace LogParser.DesktopClient.TemplateSelectors;

public class LogFieldDataTemplateSelector : DataTemplateSelector
{
    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        //получаем вызывающий контейнер
        FrameworkElement? element = container as FrameworkElement;

        if (element != null && item != null)
        {
            switch (((dynamic)item).Tag)
            {
                case Tag.SimpleField:
                    return element.FindResource("dt_SimpleField") as DataTemplate;

                case Tag.JsonField:
                    return element.FindResource("dt_Json") as DataTemplate;

                case Tag.AnnotatedJsonField:
                    return element.FindResource("dt_AnnotatedJson") as DataTemplate;

                case Tag.WithPostfixAnnotatedJsonField:
                    return element.FindResource("dt_WithPostfixAnnotatedJson") as DataTemplate;
            }
        }
        return null;
    }
}
