module LogParser.ElmishApp.Program

open System

open Elmish
open Elmish.WPF
open Microsoft.Extensions.Logging
open Serilog
open Serilog.Extensions.Logging
open p1eXu5.FSharp.Reactive

open LogParser.App
open LogParser.App.LogRepository
open LogParser.ElmishApp
open LogParser.ElmishApp.Models
open LogParser.ElmishApp.MainModel

open LogParser.ElmishApp.Loggers
open LogParser.ElmishApp.Interfaces

let main (
    window,
    mainErrorQueue,
    dialogErrorQueue,
    settingsManager: ISettingsManager,
    logFile,
    loggerFactory: ILoggerFactory)
    =
    let logFileOpt =
        match logFile with
        | null -> None
        | _ -> Some logFile

    let subject = Subject.broadcast

    //let subscribe m =
    //    let logStream dispatch =
    //        subject
    //        |> Observable.subscribe (dispatch (MainModel.Msg.))
    //    []


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
    let appSubjectLogger = loggerFactory.CreateLogger<AppSubject>()
    let fileStorageLogger = loggerFactory.CreateLogger<FileStorage>()
    let logRepositoryLogger = loggerFactory.CreateLogger<LogRepository>()

    let appConfig = settingsManager.AppConfig

    let appSubject = 
        AppSubject.init
            (AppSubjectLogger.init appSubjectLogger)
            appConfig

    let fileStorage =
        FileStorage.init
            (FileStorageLogger.init fileStorageLogger)

    let initLogRepository =
        LogRepository.init
            appConfig
            appSubject
            fileStorage
            (LogRepositoryLogger.init logRepositoryLogger)

    let updateLogFileModel =
        LogFileModel.Program.update
            mainErrorQueue

    let updateLogFileListModel =
        LogFileListModel.Program.update updateLogFileModel

    WpfProgram.mkProgram
        (MainModel.init settingsManager initLogRepository logFileOpt)
        (Program.update
            settingsManager
            mainErrorQueue
            updateLogFileListModel
        )
        (fun () -> MainModel.Bindings.bindings "Log Parser" assemblyVer mainErrorQueue dialogErrorQueue)
    // |> WpfProgram.withSubscription (subscribe appSubject)
    |> WpfProgram.withLogger loggerFactory
    |> WpfProgram.startElmishLoop window