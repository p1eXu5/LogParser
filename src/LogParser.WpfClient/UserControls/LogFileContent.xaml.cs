using System.Windows;
using System.Windows.Controls;
using LogParser.WpfClient.UserControls.ToolBars;

namespace LogParser.WpfClient.UserControls;

/// <summary>
/// Interaction logic for LogFileContent.xaml
/// </summary>
public partial class LogFileContent : UserControl
{
    public LogFileContent()
    {
        InitializeComponent();
    }

    public Visibility RawLogsInputVisibility
    {
        get { return (Visibility)GetValue(VisibilityProperty); }
        set { SetValue(VisibilityProperty, value); }
    }

    // Using a DependencyProperty as the backing store for RawLogsToggler.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty RawLogsInputVisibilityProperty =
        DependencyProperty.Register(nameof(RawLogsInputVisibility), typeof(Visibility), typeof(LogFileContent), new PropertyMetadata(default(Visibility)));
}
