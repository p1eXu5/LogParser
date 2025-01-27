using System.Windows;
using System.Windows.Input;

namespace LogParser.DesktopClient;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
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
