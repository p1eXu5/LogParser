namespace rec LogParser.DesktopClient.ElmishApp.Models

open System

open Elmish
open Elmish.Extensions
open LogParser.DesktopClient.ElmishApp
open LogParser.DesktopClient.ElmishApp.Interfaces


type MainModel =
    {
        AssemblyVersion: string
        LogFile: MainModel.LogFile

        Input: string option
        KibanaInput: string option
        SelectedInput: int

        //Output: string option
        Logs: MainModel.LogModel list
        ProcessId: Guid
        Loading: bool

        TraceId: string option
        LogCount: int
        LogsDate: DateTime option

        KibanaBaseUri: string option
        KibanaLogin: string option
        KibanaPassword: string option

        ShowMode: MainModel.ShowMode

        PinnedFieldName: string option

        ShowInnerHierarchyLogs: bool

        ErrorMessageQueue : IErrorMessageQueue
        SettingsManager: ISettingsManager
    }


module MainModel =

    type LogFile =
        | Existing of string
        | New


    type LogModel =
            | TechnoLog of TechnoLog
            | TextLog of TextLog
    type ShowMode =
            | All
            | OnlyParsedLogs


    type Msg =
        | InputChanged of string option
        | KibanaInputChanged of string option
        | SetSelectedInput of int
        | LogsChanged of (LogModel list * Guid)
        | PastFromClipboardRequested
        | CleanInputRequested
        | LogParsingRequested of Operation<unit, unit>
        | TechnoFieldMsg of key: string * TechnoField.Msg
        | TechnoLogMsg of logKey: Guid * Msg
        | CopyLogCommand
        | OpenFile
        | SaveFileAs
        | SaveFile
        | NewFile
        | SearchKibanaLogs of Operation<unit, string seq>
        | SetTraceId of string option
        | SetLogsDate of DateTime option
        | SetKibanaBaseUri of string option
        | SetKibanaLogin of string option
        | SetKibanaPassword of string option
        | CopyKibanaRequestToClipboard
        | ToggleShowAll of bool
        | ToggleShowInnerHierarchyLogs of bool
        | OnError of exn
        | SaveKibanaBaseUri
        | SaveKibanaLogin
    

    let init (errorMessageQueue: Interfaces.IErrorMessageQueue) (settingsManager: ISettingsManager) =
        let getSaved setting = 
            match (settingsManager.Load setting) with
            | :? string as s when not (String.IsNullOrWhiteSpace(s)) -> s |> Some
            | _ -> None

        fun () ->
        
            let kibanaBaseUrl = getSaved "KibanaBaseUri"
            let kibanaLogin = getSaved "KibanaLogin"
            let assemblyVer = "Version" + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()
            {
                AssemblyVersion = assemblyVer
                LogFile = LogFile.New

                Input = None
                KibanaInput = None
                SelectedInput = 0

                //Output = None
                Logs = []
                ProcessId = Guid.Empty
                Loading = false

                TraceId = None
                LogCount = 500

                LogsDate = None

                KibanaBaseUri = kibanaBaseUrl
                KibanaLogin = kibanaLogin
                KibanaPassword = None

                ShowMode = ShowMode.All

                PinnedFieldName = None

                ShowInnerHierarchyLogs = true

                ErrorMessageQueue = errorMessageQueue
                SettingsManager = settingsManager
            },
            Cmd.none

    
    let showAll model =
        match model.ShowMode with
        | ShowMode.All -> true
        | _ -> false
    
    
    let showOnlyParsedLogs model =
        match model.ShowMode with
        | ShowMode.OnlyParsedLogs -> true
        | _ -> false
