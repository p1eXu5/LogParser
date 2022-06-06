using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LogParser.App.TechnoField;
using System.Windows.Media;

namespace LogParser.DesktopClient.Resources;

public class LogFieldDataTemplateSelector : DataTemplateSelector
{
    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        //получаем вызывающий контейнер
        FrameworkElement? element = container as FrameworkElement;

        if (element != null && item != null)
        {
            switch (((dynamic)item).Tag) {
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
