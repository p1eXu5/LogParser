using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LogParser.WpfClient.UserControls.ToolBars;

namespace LogParser.WpfClient.UserControls;

/// <summary>
/// Interaction logic for ToolBar.xaml
/// </summary>
public partial class LogParserToolBarTray : UserControl
{
    public LogParserToolBarTray()
    {
        InitializeComponent();
    }

    public bool RawLogsToggler
    {
        get => (bool)GetValue(RawLogsTogglerProperty);
        set => SetValue(RawLogsTogglerProperty, value);
    }

    // Using a DependencyProperty as the backing store for RawLogsToggler.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty RawLogsTogglerProperty =
        DependencyProperty.Register(nameof(RawLogsToggler), typeof(bool), typeof(LogParserToolBarTray), new PropertyMetadata(false));

    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (m_KibanaSearchToolBar.Visibility == Visibility.Visible)
        {
            m_KibanaSearchToolBar.Visibility = Visibility.Collapsed;
            m_KibanaAccountToolBar.Visibility = Visibility.Collapsed;
            m_KibanaCheckIcon.Visibility = Visibility.Hidden;
        }
        else
        {
            m_KibanaSearchToolBar.Visibility = Visibility.Visible;
            m_KibanaAccountToolBar.Visibility = Visibility.Visible;
            m_KibanaCheckIcon.Visibility = Visibility.Visible;
        }

        m_ToolBarTray.InvalidateMeasure();

        e.Handled = true;
    }
}
