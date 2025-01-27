using System.Windows;
using System.Windows.Controls;

namespace LogParser.DesktopClient.UserControls.ToolBars
{
    /// <summary>
    /// Interaction logic for KibanaAccountToolBar.xaml
    /// </summary>
    public partial class KibanaAccountToolBar : ToolBar
    {
        public KibanaAccountToolBar()
        {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (this.DataContext != null)
            { 
                ((dynamic)this.DataContext).KibanaPassword = ((PasswordBox)sender).Password; 
            }
        }
    }
}
