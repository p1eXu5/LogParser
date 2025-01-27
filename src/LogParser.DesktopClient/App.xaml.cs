using LogParser.DesktopClient.ElmishApp.Interfaces;
using NspkXsdGenerator.DesktopClient;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace LogParser.DesktopClient;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly IErrorMessageQueue _errorMessageQueue;
    private string? _logFile;

    public App()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ru");

        CultureInfo ci = CultureInfo.CreateSpecificCulture(CultureInfo.CurrentCulture.Name);
        ci.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
        Thread.CurrentThread.CurrentCulture = ci;

        _errorMessageQueue = new ErrorMessageQueue();

        AppDomain.CurrentDomain.UnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        this.DispatcherUnhandledException += App_DispatcherUnhandledException;

        this.Activated += StartElmish;
    }

    private void StartElmish( object? sender, EventArgs e )
    {
        this.Activated -= StartElmish;
        ElmishApp.Program.main(MainWindow, _errorMessageQueue, SettingsManager.Instance, _logFile);
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        _errorMessageQueue.EnqueuError(e.Exception.Message + Environment.NewLine + e.Exception.StackTrace);
        e.Handled = false;
    }

    private void OnDispatcherUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        string errorMessage = e.ExceptionObject.ToString() + Environment.NewLine;
        _errorMessageQueue.EnqueuError(errorMessage);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        string errorMessage = e.Exception.InnerExceptions.First().Message + Environment.NewLine + e.Exception.GetType();
        _errorMessageQueue.EnqueuError(errorMessage);
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        if (e.Args.Length == 1 && File.Exists(e.Args[0]))
        {
            _logFile = e.Args[0];
        }
    }
}
