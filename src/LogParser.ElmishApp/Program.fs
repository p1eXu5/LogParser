module LogParser.ElmishApp.Program

open System
open Serilog
open Serilog.Extensions.Logging
open Elmish
open Elmish.WPF
open LogParser.App
open LogParser.ElmishApp
open LogParser.ElmishApp.Models
open LogParser.ElmishApp.MainModel

open Microsoft.Extensions.Logging
open p1eXu5.FSharp.Reactive

let main (window, mainErrorQueue, dialogErrorQueue, settingsManager, logFile, loggerFactory: ILoggerFactory) =
    let logFileOpt =
        match logFile with
        | null -> None
        | _ -> Some logFile

    let subject = Subject.broadcast

    ////let subscribe m =
    ////    let logStream dispatch =
    ////        subject
    ////        |> Observable.subscribe (dispatch (MainModel.Msg.))
    ////    []

    //let appSubject = new AppSubject(loggerFactory.CreateLogger<AppSubject>())

    //let subscribe (appSubject: AppSubject) _ : Sub<MainModel.Msg> =
    //    let fooSub dispatch =
    //        let d =
    //            (appSubject :> IObservable<Guid * LogParseMsg>)
    //                .Subscribe(fun foo ->
    //                    // TODO: dispatch (FooMsg foo)
    //                    ()
    //                )
    //        { new IDisposable with
    //            member _.Dispose() =
    //                d.Dispose()
    //        }
    //    [ [ "appSubject" ], fooSub ]

    let assemblyVer = "Version " + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()

    let mainModelLogger = loggerFactory.CreateLogger<MainModel>()

    WpfProgram.mkProgram
        (MainModel.init settingsManager logFileOpt)
        (Program.update settingsManager mainErrorQueue subject mainModelLogger)
        (fun () -> MainModel.Bindings.bindings "Log Parser" assemblyVer mainErrorQueue dialogErrorQueue)
    // |> WpfProgram.withSubscription (subscribe appSubject)
    |> WpfProgram.withLogger loggerFactory
    |> WpfProgram.startElmishLoop window