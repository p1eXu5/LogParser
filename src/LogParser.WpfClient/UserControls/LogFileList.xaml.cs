using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LogParser.WpfClient.UserControls;

/// <summary>
/// Interaction logic for MainContent.xaml
/// </summary>
public partial class LogFileList : UserControl
{
    public LogFileList()
    {
        InitializeComponent();
    }

    public Visibility RawLogsInputVisibility
    {
        get { return (Visibility)GetValue(RawLogsInputVisibilityProperty); }
        set { SetValue(RawLogsInputVisibilityProperty, value); }
    }

    // Using a DependencyProperty as the backing store for RawLogsToggler.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty RawLogsInputVisibilityProperty =
        DependencyProperty.Register(nameof(RawLogsInputVisibility), typeof(Visibility), typeof(LogFileList), new PropertyMetadata(default(Visibility)));

    private void m_AddNewTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton rb)
        {
            return;
        }

        rb.IsChecked = true;

        ICommand addNewLogFileCommand = ((dynamic)DataContext).AddNewLogFileCommand;
        if (!addNewLogFileCommand.CanExecute(null))
        {
            rb.IsChecked = false;
            e.Handled = true;
            return;
        }

        addNewLogFileCommand.Execute(null);
        rb.IsChecked = false;
        e.Handled = true;
    }
}
