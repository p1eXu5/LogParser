﻿module LogParser.App.Program

open System.IO
open Serilog
open Serilog.Extensions.Logging
open Elmish.WPF
open LogParser.App.MainModel

let main (window) =
    let logger =
        LoggerConfiguration()
          .MinimumLevel.Override("Elmish.WPF.Update", Events.LogEventLevel.Verbose)
          .MinimumLevel.Override("Elmish.WPF.Bindings", Events.LogEventLevel.Verbose)
          .MinimumLevel.Override("Elmish.WPF.Performance", Events.LogEventLevel.Verbose)
          .WriteTo.Console(outputTemplate="[{Timestamp:HH:mm:ss:fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
          .WriteTo.Seq("http://localhost:5341")
          .CreateLogger()

    let loggerFactory = new SerilogLoggerFactory(logger)
    //let store = Infrastruture.FsStatementInMemoryStore.store



    WpfProgram.mkProgram Program.init Program.update Program.bindings
    |> WpfProgram.withLogger loggerFactory
    |> WpfProgram.startElmishLoop window