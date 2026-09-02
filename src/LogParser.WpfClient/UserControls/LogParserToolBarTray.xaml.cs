using System.Windows;
using System.Windows.Controls;

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

    /// <summary>
    /// For raw logs visibility controlling from menu. <see cref="LogFileContent"/>.
    /// </summary>
    public bool RawLogsToggler
    {
        get => (bool)GetValue(RawLogsTogglerProperty);
        set => SetValue(RawLogsTogglerProperty, value);
    }

    public static readonly DependencyProperty RawLogsTogglerProperty =
        DependencyProperty.Register(nameof(RawLogsToggler), typeof(bool), typeof(LogParserToolBarTray), new PropertyMetadata(false));

    private void ToggleKibanaToolBarsVisibility(object sender, RoutedEventArgs e)
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

    private void ToggleStandardBarVisibility(object sender, RoutedEventArgs e)
    {
        if (m_StandardToolBar.Visibility == Visibility.Visible)
        {
            m_StandardToolBar.Visibility = Visibility.Collapsed;
            m_StandardBarCheckIcon.Visibility = Visibility.Hidden;
        }
        else
        {
            m_StandardToolBar.Visibility = Visibility.Visible;
            m_StandardBarCheckIcon.Visibility = Visibility.Visible;
        }

        m_ToolBarTray.InvalidateMeasure();

        e.Handled = true;
    }

    private void ToggleFiltersBarVisibility(object sender, RoutedEventArgs e)
    {
        if (m_FiltersToolBar.Visibility == Visibility.Visible)
        {
            m_FiltersToolBar.Visibility = Visibility.Collapsed;
            m_FiltersBarCheckIcon.Visibility = Visibility.Hidden;
        }
        else
        {
            m_FiltersToolBar.Visibility = Visibility.Visible;
            m_FiltersBarCheckIcon.Visibility = Visibility.Visible;
        }

        m_ToolBarTray.InvalidateMeasure();

        e.Handled = true;
    }
}
