module LogParser.DesktopClient.ElmishApp.Program

open System.IO
open Serilog
open Serilog.Extensions.Logging
open Elmish.WPF
open LogParser.DesktopClient.ElmishApp
open LogParser.DesktopClient.ElmishApp.Models
open LogParser.DesktopClient.ElmishApp.MainModel

open Microsoft.Extensions.Logging

let main (window, errorQueue, settingsManager) =
    let logger =
        LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Override("Elmish.WPF.Update", Events.LogEventLevel.Verbose)
            .MinimumLevel.Override("Elmish.WPF.Bindings", Events.LogEventLevel.Verbose)
            .MinimumLevel.Override("Elmish.WPF.Performance", Events.LogEventLevel.Verbose)
            .MinimumLevel.Override("LogParser.App", Events.LogEventLevel.Verbose)
            .WriteTo.Debug(outputTemplate="[{Timestamp:HH:mm:ss:fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            // .WriteTo.Console(outputTemplate="[{Timestamp:HH:mm:ss:fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
#else
            .MinimumLevel.Override("Elmish.WPF.Update", Events.LogEventLevel.Information)
            .MinimumLevel.Override("Elmish.WPF.Bindings", Events.LogEventLevel.Information)
            .MinimumLevel.Override("Elmish.WPF.Performance", Events.LogEventLevel.Information)
            .MinimumLevel.Override("LogParser.App", Events.LogEventLevel.Information)
            .WriteTo.Seq("http://localhost:5341")
#endif
            .CreateLogger()

    let loggerFactory = new SerilogLoggerFactory(logger)
    //let store = Infrastruture.FsStatementInMemoryStore.store

    let mainModelLogger : ILogger = loggerFactory.CreateLogger("LogParser.App.MainModel.MainModel")

    mainModelLogger.LogDebug("debug")
    mainModelLogger.LogInformation("info")

    WpfProgram.mkProgram (MainModel.init errorQueue settingsManager) (Program.update mainModelLogger) MainModel.Bindings.bindings
    |> WpfProgram.withLogger loggerFactory
    |> WpfProgram.startElmishLoop window