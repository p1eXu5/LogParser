using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using LogParser.ElmishApp.Interfaces;
using Microsoft.Extensions.Logging;

namespace LogParser.WpfClient;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private Bootstrap? _bootstrap;
    private ILogger<App>? _logger;
    private IErrorMessageQueue? _errorMessageQueue;
    private readonly CancellationTokenSource _cts = new();

    public App()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ru");

        CultureInfo ci = CultureInfo.CreateSpecificCulture(CultureInfo.CurrentCulture.Name);
        ci.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
        Thread.CurrentThread.CurrentCulture = ci;

        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    private async void Application_Startup(object sender, StartupEventArgs e)
        => await BootstrapApplicationAsync(e);

    private async Task BootstrapApplicationAsync(StartupEventArgs e)
    {
        _bootstrap = Bootstrap.Build<Bootstrap>(e.Args);
        await _bootstrap.StartHostAsync(_cts.Token);

        _errorMessageQueue = _bootstrap.GetRequiredKeyedService<IErrorMessageQueue>("main");
        _logger = _bootstrap.GetLogger<App>();
        _logger.LogInformation("Bootstrapped.");

        string? openningLogFile = null;

        if (e.Args.Length == 1 && File.Exists(e.Args[0]))
        {
            openningLogFile = e.Args[0];
        }

        var mainWindow = new MainWindow();

        ElmishApp.Program.main(
            mainWindow,
            _errorMessageQueue,
            _bootstrap.GetRequiredKeyedService<IErrorMessageQueue>("dialog"),
            _bootstrap.GetRequiredService<ISettingsManager>(),
            openningLogFile,
            _bootstrap.GetRequiredService<ILoggerFactory>()
        );

        mainWindow.Show();
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        _cts.Cancel();

        if (_bootstrap is not null)
        {
            _bootstrap.StopHostAsync(TimeSpan.FromSeconds(5))
                .GetAwaiter()
                .GetResult();
        }

        _cts.Dispose();
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.LogError(e.Exception, "App dispatcher unhandled exception!");
#if DEBUG
        _errorMessageQueue?.EnqueueError(e.Exception.Message + Environment.NewLine + e.Exception.StackTrace);
#else
        _errorMessageQueue?.EnqueueError("Error!");
#endif
        e.Handled = false;
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        string errorMessage = e.ExceptionObject.ToString() + Environment.NewLine;
        _logger?.LogError(errorMessage);
#if DEBUG
        _errorMessageQueue?.EnqueueError(errorMessage);
#else
        _errorMessageQueue?.EnqueueError("Error!");
#endif
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        string errorMessage = e.Exception.InnerExceptions.First().Message + Environment.NewLine + e.Exception.GetType();
        _logger?.LogError(errorMessage);
#if DEBUG
        _errorMessageQueue?.EnqueueError(errorMessage);
#else
        _errorMessageQueue?.EnqueueError("Error!");
#endif
    }
}
