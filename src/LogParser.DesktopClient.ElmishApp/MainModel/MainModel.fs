namespace rec LogParser.DesktopClient.ElmishApp.Models

open System

open Elmish
open Elmish.Extensions
open LogParser.DesktopClient.ElmishApp
open LogParser.DesktopClient.ElmishApp.Interfaces
open MainModel
open LogParser.Core.Types


type MainModel =
    {
        AssemblyVersion: string
        LogFile: LogFile

        Input: string option
        KibanaInput: string option
        SelectedInput: int
        Loading: bool
        ShowMode: ShowMode
        
        Logs: LogModel list

        /// is set when log parsing is starting
        /// could be replaced with TCS in future
        ProcessId: Guid
        
        PinnedFieldName: string option

        KibanaSearchModel: KibanaSearchModel
        FiltersModel: FiltersModel
        ErrorMessageQueue : IErrorMessageQueue
        TempTitle: string option
    }
and
     LogFile =
        | Existing of string
        | New
and
    LogModel =
            | TechLogModel of TechLogModel
            | TextLogModel of TextLogModel
and
    ShowMode =
        | All
        | OnlyParsedLogs


module LogFile =

    let isCsv = function
        | LogFile.Existing f ->
            System.IO.Path.GetExtension(f).Equals(".csv", StringComparison.OrdinalIgnoreCase)
        | _ -> false

[<RequireQualifiedAccess>]
module LogModel =

    let logId = function
        | LogModel.TechLogModel l -> l.Id 
        | LogModel.TextLogModel l -> l.Id

    let timestamp = function
        | TechLogModel log -> log.Timestamp
        | TextLogModel _ -> None

    let toString = function
        | TechLogModel log ->
            match log.Log.Source with
            | Some ls ->
                sprintf "%s %s"
                    (ls.ToString())
                    (log.Log.Fields |> LogParser.Core.Types.TechField.toString 1)
            | None ->
                sprintf "%s"
                    (log.Log.Fields |> LogParser.Core.Types.TechField.toString 1)
        | TextLogModel tl -> tl.Log

    let serviceName = function
        | TechLogModel log -> log.ServiceName |> Some
        | _ -> None

module MainModel =

    type Msg =
        | SetSelectedInput of int
        | InputChanged of string option
        | PastFromClipboardRequested
        | KibanaInputChanged of string option
        | LogsChanged of (LogModel list * Guid)
        | CleanInputRequested
        | LogParsingRequested of Operation<unit, unit>
        | TechFieldMsg of key: string * TechFieldModel.Msg
        | TechLogMsg of logKey: Guid * Msg
        | KibanaSearchModelMsg of KibanaSearchModel.Msg
        | FiltersModelMsg of FiltersModel.Msg
        | OrderByTimestamp
        | CopyLogCommand
        | OpenFile
        /// Used in `DrugFileCommand`
        | OpenSpecifiedFile of string
        | SaveFileAs
        | SaveFile
        | NewFile
        | ToggleShowAll of bool
        | SetTempTitle of string option
        | OnError of exn


    let init (errorMessageQueue: Interfaces.IErrorMessageQueue) (settingsManager: ISettingsManager) (logFile: string option) =
        fun () ->
            let cmd =
                logFile |> Option.map (fun fileName -> Cmd.ofMsg (Msg.OpenSpecifiedFile fileName)) |> Option.defaultValue Cmd.none

            let assemblyVer = "Version " + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()

            {
                AssemblyVersion = assemblyVer
                LogFile = LogFile.New

                Input = None
                KibanaInput = None
                SelectedInput = 0
                Loading = false
                ShowMode =
                    match logFile with
                    | Some _ -> ShowMode.OnlyParsedLogs
                    | _ -> ShowMode.All

                Logs = []
                ProcessId = Guid.Empty
                PinnedFieldName = None

                KibanaSearchModel = KibanaSearchModel.init settingsManager
                FiltersModel = FiltersModel.init ()
                ErrorMessageQueue = errorMessageQueue
                TempTitle = None
            }
            , cmd


    // --------------------------------- helpers

    let notEmpty s = not (String.IsNullOrWhiteSpace(s))
    let noCmd = fun m -> m, Cmd.none

    let private fileNameWithoutExtension m =
        match m.LogFile with
            | Existing path ->
                System.IO.Path.GetFileNameWithoutExtension(path) |> Some
            | _ ->
                None

    // --------------------------------- accessors
    
    let getKibanaSearchModel (m: MainModel) = m.KibanaSearchModel
    let setKibanaSearchModel kibanaSearchModel (m: MainModel) =
        { m with KibanaSearchModel = kibanaSearchModel }

    let getTempTitle m = 
        match m.TempTitle with
        | Some tempTitle -> tempTitle |> Some
        | None -> fileNameWithoutExtension m

    let setTempTitle v m = { m with TempTitle = v }

    let getDocumentName m =
        match m.LogFile with
            | Existing path ->
                let docName = $"{System.IO.Path.GetFileNameWithoutExtension(path)}        ({path})"
                
                m.TempTitle
                |> Option.map (fun t -> $"{t}    {docName}")
                |> Option.defaultValue docName
                
            | _ -> m.TempTitle |> Option.defaultValue "Untitled"

    let showAll model =
        match model.ShowMode with
        | ShowMode.All -> true
        | _ -> false

    let showOnlyParsedLogs model =
        match model.ShowMode with
        | ShowMode.OnlyParsedLogs -> true
        | _ -> false

    let filtersModel (m: MainModel) =
        m.FiltersModel

    let withFiltersModel filtersModel (m: MainModel) =
        { m with FiltersModel = filtersModel }

    let getFilteredLogModels (m: MainModel) =
        let hierarchyProccessedLogs =
            if m.FiltersModel.ShowInnerHierarchyLogs then // TODO: remove after make log model hierarchy
                m.Logs 
            else 
                m.Logs 
                |> List.choose (fun logModel ->
                    match logModel with
                    | LogModel.TechLogModel l ->
                        if l.IsNestedLog then None
                        else logModel |> Some
                    | LogModel.TextLogModel _ -> logModel |> Some
                )

        if not <| m.FiltersModel.FilterOn then
            hierarchyProccessedLogs
        else
            let traceId = m.FiltersModel.TraceId
            let serviceName = m.FiltersModel.SelectedServiceName
            let startTime = m.FiltersModel.Start |> Option.map (fun dt -> dt.TimeOfDay)
            let endTime = m.FiltersModel.End |> Option.map (fun dt -> dt.TimeOfDay)
            let logLevel = m.FiltersModel.SelectedLogLevel
            hierarchyProccessedLogs
            |> List.filter (fun logModel ->
                match logModel with
                | TechLogModel techLog ->
                    (startTime.IsNone || (startTime.IsSome && techLog.Timestamp |> Option.map (fun dto -> dto.TimeOfDay >= startTime.Value) |> Option.defaultValue false))
                    &&
                    (endTime.IsNone || (endTime.IsSome && techLog.Timestamp |> Option.map (fun dto -> dto.TimeOfDay <= endTime.Value) |> Option.defaultValue false))
                    &&
                    (traceId.IsNone || (traceId.IsSome && techLog.HierarchicalTraceId.Contains(traceId.Value, StringComparison.OrdinalIgnoreCase)))
                    &&
                    (serviceName.IsNone || (serviceName.IsSome && techLog.ServiceName.Equals(serviceName.Value, StringComparison.OrdinalIgnoreCase)))
                    &&
                    (logLevel.IsNone || (logLevel.IsSome && techLog.LogLevel.Equals(logLevel.Value, StringComparison.OrdinalIgnoreCase)))
                | _ -> false
            )



    let setFilteringServiceNamesCmd (logs: LogModel list) =
        let t =
            logs 
            |> List.fold (fun (state: {| ServiceNames: Set<string>; LogLevels: Set<string>; Timestamps: Set<DateTimeOffset option> |}) (l: LogModel) ->
                match l with
                | LogModel.TextLogModel _ -> state
                | LogModel.TechLogModel tlog ->
                    {| state with
                        LogLevels =
                            if state.LogLevels |> Set.contains tlog.LogLevel then state.LogLevels
                            else state.LogLevels |> Set.add tlog.LogLevel
                        ServiceNames =
                            if state.ServiceNames |> Set.contains tlog.ServiceName then state.ServiceNames
                            else state.ServiceNames |> Set.add tlog.ServiceName
                        Timestamps =
                            if state.Timestamps |> Set.contains tlog.Timestamp then state.Timestamps
                            else state.Timestamps |> Set.add tlog.Timestamp
                    |}
            ) (
                {|
                    ServiceNames = Set.empty
                    LogLevels = Set.empty
                    Timestamps = Set.empty
                |}
            )

        let timestamps = t.Timestamps |> Set.toList |> List.choose id |> List.sort

        Cmd.batch [
            t.ServiceNames |> Set.toList |> FiltersModel.Msg.SetServiceNames |> FiltersModelMsg |> Cmd.ofMsg
            t.LogLevels |> Set.toList |> FiltersModel.Msg.SetLogLevels |> FiltersModelMsg |> Cmd.ofMsg
            timestamps |> List.tryHead |> Option.map (fun dto -> dto.DateTime) |> FiltersModel.Msg.SetStartDateNoActivate |> FiltersModelMsg |> Cmd.ofMsg
            timestamps |> List.tryLast |> Option.map (fun dto -> dto.DateTime) |> FiltersModel.Msg.SetEndDateNoActivate |> FiltersModelMsg |> Cmd.ofMsg
        ]

