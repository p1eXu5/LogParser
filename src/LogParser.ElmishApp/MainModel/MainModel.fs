namespace rec LogParser.ElmishApp.Models

open System

open Elmish
open p1eXu5.FSharp.ElmishExtensions

open LogParser.ElmishApp
open LogParser.ElmishApp.Interfaces
open LogParser.ElmishApp.Types
open LogParser.App
open LogParser.App.LogRepository


type MainModel =
    {
        LogFileListModel: LogFileListModel

        TempTitle: string option
        ShowMode: ShowMode

        FiltersModel: FiltersModel option
        KibanaSearchModel: KibanaSearchModel option


        //Input: string option
        //KibanaInput: string option
        //SelectedInput: int
        //Loading: bool

        ///// is set when log parsing is starting
        ///// could be replaced with TCS in future
        //ProcessId: Guid
        
        //PinnedFieldName: string option

    }

module LogFile =

    let isCsv = function
        | LogFile.Existing f ->
            System.IO.Path.GetExtension(f).Equals(".csv", StringComparison.OrdinalIgnoreCase)
        | _ -> false

module MainModel =

    type Msg =
        | LoadKibanaSearchModel
        | UnloadKibanaSearchModel
        | LoadFiltersModel
        | UnloadFiltersModel
        | LogFileListModelMsg of LogFileListModel.Msg

        | SetSelectedInput of int
        | InputChanged of string option
        
        | KibanaInputChanged of string option
        | LogsChanged of (TechLogModel list * Guid)
        | CleanInputRequested
        | LogParsingRequested of Operation<unit, unit>
        | TextLogMsg of TextLogModel.Msg
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


    let init (settingsManager: ISettingsManager) (initLogRepository: LogSourceId -> LogRepository) (logFile: string option) =
        fun () ->

            let logFileListModel = LogFileListModel.init initLogRepository

            let cmds =
                [
                    if logFile.IsSome then
                        Cmd.ofMsg (Msg.OpenSpecifiedFile logFile.Value)
                ]

            {
                LogFileListModel = logFileListModel
                TempTitle = None
                ShowMode = ShowMode.All
                FiltersModel = None
                KibanaSearchModel = None

                (*
                TechLogListModel = techLogListModel

                LogFile = LogFile.New

                Input = None
                KibanaInput = None
                SelectedInput = 0
                Loading = false
                ShowMode =
                    match logFile with
                    | Some _ -> ShowMode.OnlyParsedLogs
                    | _ -> ShowMode.All

                ProcessId = Guid.Empty
                PinnedFieldName = None

                KibanaSearchModel = KibanaSearchModel.init settingsManager
                FiltersModel = FiltersModel.init ()
                ErrorMessageQueue = errorMessageQueue
                *)
            }
            ,
            // Cmd.batch cmds
            Cmd.none

    // --------------------------------- accessors
    
    let getKibanaSearchModel (m: MainModel) = m.KibanaSearchModel
    let setKibanaSearchModel kibanaSearchModel (m: MainModel) =
        { m with KibanaSearchModel = kibanaSearchModel }

    let inline setTempTitle v m = { m with TempTitle = v }

    let documentNameTitle defaultTitle m =
        match m.LogFileListModel |> LogFileListModel.selecteLogFileTitle with
            | Some (title)->
                title
            | _ -> m.TempTitle |> Option.defaultValue $"{defaultTitle} - New"

    let inline  showAll model =
        match model.ShowMode with
        | ShowMode.All -> true
        | _ -> false

    let inline showOnlyParsedLogs model =
        match model.ShowMode with
        | ShowMode.OnlyParsedLogs -> true
        | _ -> false

    let inline toggleShowMode model =
        match model.ShowMode with
        | ShowMode.OnlyParsedLogs -> { model with ShowMode = ShowMode.All }
        | ShowMode.All -> { model with ShowMode = ShowMode.OnlyParsedLogs }

    let inline filtersModel (m: MainModel) =
        m.FiltersModel

    let inline withFiltersModel filtersModel (m: MainModel) =
        { m with FiltersModel = filtersModel }

    let inline withLogFileListMoodel logFileListModel (m: MainModel) =
        { m with LogFileListModel = logFileListModel }
    (*
    let getFilteredLogModels (m: MainModel) =
        let hierarchyProccessedLogs =
            if m.FiltersModel.ShowInnerHierarchyLogs then // TODO: remove after make log model hierarchy
                m.Logs 
            else 
                m.Logs 
                |> List.choose (fun logModel ->
                    match logModel with
                    | TechLogModel.JsonLogModel l ->
                        if l.IsNestedLog then None
                        else logModel |> Some
                    | TechLogModel.TextLogModel _ -> logModel |> Some
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
                | TechLogModel.JsonLogModel techLog ->
                    (
                        startTime.IsNone
                        || (
                            startTime.IsSome
                            &&
                                techLog.Timestamp
                                |> Option.map (fun dto -> dto.TimeOfDay >= startTime.Value)
                                |> Option.defaultValue false
                        )
                    )
                    && (
                        endTime.IsNone
                        || (
                            endTime.IsSome
                            &&
                                techLog.Timestamp
                                |> Option.map (fun dto -> dto.TimeOfDay <= endTime.Value)
                                |> Option.defaultValue false
                        )
                    )
                    && (
                        traceId.IsNone
                        || (
                            traceId.IsSome
                            && techLog.HierarchicalTraceId.Contains(traceId.Value, StringComparison.OrdinalIgnoreCase)
                        )
                    )
                    && (
                        serviceName.IsNone
                        || (
                            serviceName.IsSome
                            && techLog.ServiceName.Equals(serviceName.Value, StringComparison.OrdinalIgnoreCase)
                        )
                    )
                    &&
                    (
                        logLevel.IsNone
                        || (
                            logLevel.IsSome
                            && techLog.LogLevel.Equals(logLevel.Value, StringComparison.OrdinalIgnoreCase)
                        )
                    )
                | _ -> false
            )

    let setFilteringServiceNamesCmd (logs: TechLogModel list) =
        let t =
            logs 
            |> List.fold (
                fun (state: {| ServiceNames: Set<string>; LogLevels: Set<string>; Timestamps: Set<DateTimeOffset option> |})
                    (l: TechLogModel)
                    ->
                    match l with
                    | TechLogModel.TextLogModel _ ->
                        state
                    | TechLogModel.JsonLogModel tlog ->
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
    *)

