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
/// Interaction logic for RawLogsInput.xaml
/// </summary>
public partial class RawLogsInput : UserControl
{
    public RawLogsInput()
    {
        InitializeComponent();
    }

    private void TextBox_PreviewDragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            // Note that you can have more than one file.
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            // Assuming you have one file that you care about, pass it off to whatever
            // handling code you have defined.
            ICommand openFileCommand = ((dynamic)DataContext).DrugFileCommand;
            if (openFileCommand.CanExecute(files[0]))
            {
                openFileCommand.Execute(files[0]);
            }

            e.Handled = true;
        }
    }
}
