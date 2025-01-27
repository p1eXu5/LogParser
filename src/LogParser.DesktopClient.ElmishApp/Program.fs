module LogParser.DesktopClient.ElmishApp.Program

open System.IO
open Serilog
open Serilog.Extensions.Logging
open Elmish.WPF
open LogParser.DesktopClient.ElmishApp
open LogParser.DesktopClient.ElmishApp.Models
open LogParser.DesktopClient.ElmishApp.MainModel

open Microsoft.Extensions.Logging


let [<Literal>] debugLogLevel = Events.LogEventLevel.Debug

let main (window, errorQueue, settingsManager, logFile) =
    let logger =
        LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Override("Elmish.WPF.Update", debugLogLevel)
            .MinimumLevel.Override("Elmish.WPF.Bindings", debugLogLevel)
            .MinimumLevel.Override("Elmish.WPF.Performance", debugLogLevel)
            .MinimumLevel.Override("LogParser.App", Events.LogEventLevel.Debug)
            .WriteTo.Debug(outputTemplate="[{Timestamp:HH:mm:ss:fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            // .WriteTo.Console(outputTemplate="[{Timestamp:HH:mm:ss:fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
#else
            .MinimumLevel.Override("Elmish.WPF.Update", Events.LogEventLevel.Error)
            .MinimumLevel.Override("Elmish.WPF.Bindings", Events.LogEventLevel.Error)
            .MinimumLevel.Override("Elmish.WPF.Performance", Events.LogEventLevel.Error)
            .MinimumLevel.Override("LogParser.App", Events.LogEventLevel.Error)
            .WriteTo.Seq("http://localhost:5341")
#endif
            .CreateLogger()

    let loggerFactory = new SerilogLoggerFactory(logger)
    //let store = Infrastruture.FsStatementInMemoryStore.store

    let mainModelLogger : ILogger = loggerFactory.CreateLogger("LogParser.App.MainModel.MainModel")

    mainModelLogger.LogDebug("debug")
    mainModelLogger.LogInformation("info")

    let logFileOpt =
        match logFile with
        | null -> None
        | _ -> Some logFile

    WpfProgram.mkProgram (MainModel.init errorQueue settingsManager logFileOpt) (Program.update settingsManager mainModelLogger) MainModel.Bindings.bindings
    |> WpfProgram.withLogger loggerFactory
    |> WpfProgram.startElmishLoop window