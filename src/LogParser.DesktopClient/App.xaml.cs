using System;
using System.Globalization;
using System.Threading;
using System.Windows;

namespace LogParser.DesktopClient;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
        {
            CultureInfo ci = CultureInfo.CreateSpecificCulture(CultureInfo.CurrentCulture.Name);
            ci.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            Thread.CurrentThread.CurrentCulture = ci;

            this.Activated += StartElmish;
        }

        private void StartElmish( object? sender, EventArgs e )
        {
            this.Activated -= StartElmish;
            LogParser.App.Program.main(MainWindow);
        }
}
