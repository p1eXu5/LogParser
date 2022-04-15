using LogParser.App.MainModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace LogParser.DesktopClient.Resources;

public class LogDataTemplateSelector : DataTemplateSelector
{
    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        //получаем вызывающий контейнер
        FrameworkElement? element = container as FrameworkElement;

        if (element != null && item != null)
        {
            if (((dynamic)item).IsTechnoLog)
                return element.FindResource("dt_TechnoLog") as DataTemplate;

            return element.FindResource("dt_TextLog") as DataTemplate;   
        }
        return null;
    }
}
