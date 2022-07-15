namespace LogParser.App.MainModel

open System
open System.Windows;

open Elmish
open FsToolkit.ErrorHandling

open LogParser.App
open LogParser.Core.Parser
open LogParser.Core.Types
open Microsoft.Win32
open System.IO
open Elmish.Extensions
open System
open LogParser.Core
open LogParser.DesktopClient.ElmishApp
open LogParser.DesktopClient.ElmishApp.Interfaces


type LogFile =
    | Existing of string
    | New


type internal MainModel =
    {
        AssemblyVersion: string
        LogFile: LogFile

        Input: string option
        KibanaInput: string option
        SelectedInput: int

        //Output: string option
        Logs: LogModel list
        ProcessId: Guid
        Loading: bool

        TraceId: string option
        LogCount: int
        LogsDate: DateTime option

        KibanaBaseUri: string option
        KibanaLogin: string option
        KibanaPassword: string option

        ShowMode: ShowMode

        PinnedFieldName: string option

        ShowInnerHierarchyLogs: bool

        ErrorMessageQueue : IErrorMessageQueue
    }
and
    LogModel =
        | TechnoLog of TechnoLog.Model
        | TextLog of TextLog.Model
and
    ShowMode =
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
    

module internal Program =

    let init (errorMessageQueue: Interfaces.IErrorMessageQueue) (_: unit) =

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

            KibanaBaseUri = None
            KibanaLogin = None
            KibanaPassword = None

            ShowMode = ShowMode.All

            PinnedFieldName = None

            ShowInnerHierarchyLogs = true

            ErrorMessageQueue = errorMessageQueue
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


    let private toLogModels logs =
        logs
        |> List.map (fun l -> 
            match l with
            | Log.TechnoLog l ->
                let technoLogModel = TechnoLog.Program.init (l)
                LogModel.TechnoLog technoLogModel
            | Log.TextLog l ->
                let (textLogModel, _) = TextLog.Program.init (l)
                LogModel.TextLog textLogModel
        )


    let update (msg: Msg) (model: MainModel) =
        match msg with
        | NewFile -> init model.ErrorMessageQueue ()

        | OpenFile ->
            let fd = OpenFileDialog()
            fd.Filter <- "text files (*.txt)|*.txt|All files (*.*)|*.*"
            let result = fd.ShowDialog()
            if result.HasValue && result.Value then
                let ``process`` () =
                    task {
                        let! content = File.ReadAllTextAsync(fd.FileName)
                        return content |> Some
                    }
                {
                    model with 
                        LogFile = LogFile.Existing fd.FileName
                        SelectedInput = 1
                        Loading = true
                }, Cmd.OfTask.perform ``process`` () InputChanged
            else 
                (model, Cmd.none)

        | SaveFileAs ->
            let fd = SaveFileDialog()
            fd.Filter <- "text files (*.txt)|*.txt|All files (*.*)|*.*"
            let result = fd.ShowDialog()
            if result.HasValue && result.Value then
                let content = model.Input |> Option.defaultValue ""
                use sw = File.CreateText(fd.FileName)
                sw.Write(content)
                sw.Flush()
                { model with LogFile = LogFile.Existing fd.FileName }, Cmd.none
            else
                (model, Cmd.none)

        | SaveFile ->
            match model.LogFile with
            | LogFile.New _ -> ()
            | LogFile.Existing path ->
                if File.Exists(path) then
                    let content = model.Input |> Option.defaultValue ""
                    use sw = File.CreateText(path)
                    sw.Write(content)
                    sw.Flush()

            (model, Cmd.none)

        | SetSelectedInput ind -> { model with SelectedInput = ind }, Cmd.none

        | KibanaInputChanged (Some v) when not (String.IsNullOrWhiteSpace(v)) ->
            Kibana.parse2 v
            |> Result.map (fun l -> String.Join(Environment.NewLine, l))
            |> Result.map (fun s ->
                { model with KibanaInput = v |> Some; Loading = true }, Cmd.ofMsg (InputChanged (Some s))
            )
            |> Result.defaultValue (model, Cmd.none)

        | InputChanged (Some v) when not (String.IsNullOrWhiteSpace(v)) ->
            let parseAsync (v, processId) =
                async {
                    match parse (v) with
                    | Ok xlog ->
                        return (toLogModels xlog), processId

                    | Error err ->
                        let (textLogModel, _) = TextLog.Program.init (err)
                        //{model with Logs = textLogModel |> LogModel.TextLog |> List.singleton; Input = v |> Some}, Cmd.none
                        return textLogModel |> LogModel.TextLog |> List.singleton, processId
                }

            let processId = Guid.NewGuid()
            {
                model with 
                    Input = v |> Some; 
                    ProcessId = processId
                    Loading = true
            }
            , Cmd.OfAsync.either parseAsync (v, processId) LogsChanged Msg.OnError

        | InputChanged _ ->
            {model with Logs = []; Input = None; ProcessId = Guid.Empty}, Cmd.none

        | LogsChanged (logs, processId) when processId = model.ProcessId ->
            {model with Logs = logs; Loading = false}, Cmd.none

        | PastFromClipboardRequested ->
            let v = Clipboard.GetText() |> Some
            match model.SelectedInput with
            | 0 -> model, Cmd.ofMsg (InputChanged v)
            | 1 -> model, Cmd.ofMsg (KibanaInputChanged v)
            | _ -> model, Cmd.none

        | CopyKibanaRequestToClipboard ->
            do Clipboard.SetText(Kibana.searchRequest model.LogCount (model.TraceId |> Option.get))
            model, Cmd.none

        | CleanInputRequested ->
            {model with Input = None; KibanaInput = None; Logs = []; ProcessId = Guid.Empty; Loading = false}, Cmd.none

        | TechnoLogMsg (id, TechnoFieldMsg (key, msg)) ->
            match msg with
            | TechnoField.Msg.PinFieldValueInHeader key ->
                { model with PinnedFieldName = key |> Some }, Cmd.none
            | _ ->    
                let log =
                    model.Logs
                    |> List.choose (function LogModel.TechnoLog tl -> tl |> Some | _ -> None)
                    |> List.find (fun l -> l.Id = id)
                
                let field =
                    log.Fields
                    |> List.find (fun f -> f.Key = key)

                let newField = TechnoField.Program.update msg field // TODO: update list when field changing

                model, Cmd.none

        | TechnoLogMsg (id, Msg.CopyLogCommand) ->
            let log =
                model.Logs
                |> List.choose (function LogModel.TechnoLog tl -> tl |> Some | _ -> None)
                |> List.find (fun l -> l.Id = id)

            Clipboard.SetText(log.Log.Fields |> TechnoFields.toString 1)
            model, Cmd.none

        | SetTraceId traceId -> { model with TraceId = traceId }, Cmd.none
        | SetLogsDate logsDate -> { model with LogsDate = logsDate }, Cmd.none
        | SetKibanaBaseUri uri -> { model with KibanaBaseUri = uri }, Cmd.none
        | SetKibanaLogin login -> { model with KibanaLogin = login }, Cmd.none
        | SetKibanaPassword pass -> { model with KibanaPassword = pass }, Cmd.none

        | SearchKibanaLogs (Start ()) ->
            model
            , Cmd.OfTask.perform 
                (
                    Kibana.searchLogs 
                        (model.KibanaBaseUri |> Option.get) 
                        model.LogsDate 
                        (model.KibanaLogin |> Option.get) 
                        (model.KibanaPassword |> Option.get)
                ) 
                (model.TraceId |> Option.get) 
                (Finish >> SearchKibanaLogs)

        | SearchKibanaLogs (Finish logs) when logs |> (not << Seq.isEmpty) ->
            let logMessage = String.Join(Environment.NewLine, logs) |> Some
            model, Cmd.ofMsg (InputChanged logMessage)

        | ToggleShowAll v ->
            if v then
                { model with ShowMode = ShowMode.All }, Cmd.none
            else
                { model with ShowMode = ShowMode.OnlyParsedLogs }, Cmd.none

        | ToggleShowInnerHierarchyLogs v ->
            { model with ShowInnerHierarchyLogs = v }, Cmd.none

        | Msg.OnError ex ->
            model.ErrorMessageQueue.EnqueuError(ex.Message)
            model, Cmd.none

        | _ -> model, Cmd.none


    // =====================
    //       Bindings
    // =====================
    open Elmish.WPF

    let bindings () : Binding<MainModel, Msg> list =
        [
            "AssemblyVersion" |> Binding.oneWay (fun m -> m.AssemblyVersion)

            "DockerInput" |> Binding.twoWayOpt ((fun m -> m.Input), Msg.InputChanged)
            "KibanaInput" |> Binding.twoWayOpt ((fun m -> m.KibanaInput), Msg.KibanaInputChanged)
            "SelectedInput" |> Binding.twoWay ((fun m -> m.SelectedInput), Msg.SetSelectedInput)
            
            "CopyKibanaRequestToClipboardCommand" 
            |> Binding.cmdIf (fun m ->
                m.TraceId
                |> Option.map (fun _ -> Msg.CopyKibanaRequestToClipboard)
            )

            //"Output" |> Binding.oneWayOpt (fun m -> m.Output)

            "Loading" |> Binding.oneWay (fun m -> m.Loading)

            "PasteFromClipboardCommand" |> Binding.cmdIf (fun _ ->
                if Clipboard.ContainsText() then
                    PastFromClipboardRequested |> Some
                else
                    None
            )

            "ClearInputCommand" |> Binding.cmdIf (fun m -> m.Input |> Option.map (fun _ -> CleanInputRequested))

            "OpenLogsFileCommand" |> Binding.cmd OpenFile
            "SaveLogsFileAsCommand" |> Binding.cmdIf (fun m -> m.Input |> Option.map (fun _ -> SaveFileAs))
            "SaveLogsFileCommand" |> Binding.cmdIf (fun m -> 
                match m.LogFile with
                | Existing _ -> Msg.SaveFile |> Some
                | _ -> None
            )

            "NewFileCommand" |> Binding.cmdIf (fun m -> 
                match m.LogFile with
                | Existing _ -> Msg.NewFile |> Some
                | _ -> None
            ) 

            "DocumentName" |> Binding.oneWay (fun m ->
                match m.LogFile with
                | Existing path -> path
                | _ -> "Untitled"
            )


            "ShowInnerHierarchyLogs" |> Binding.twoWay ((fun m -> m.ShowInnerHierarchyLogs), Msg.ToggleShowInnerHierarchyLogs) // TODO: remove after make log model hierarchy


            "Logs" |> Binding.subModelSeq (
                (fun m -> 
                    if m.ShowInnerHierarchyLogs then // TODO: remove after make log model hierarchy
                        m.Logs 
                    else 
                        m.Logs 
                        |> List.choose (fun logModel ->
                            match logModel with
                            | LogModel.TechnoLog l ->
                                if l.IsNestedLog then None
                                else logModel |> Some
                            | LogModel.TextLog _ -> logModel |> Some
                        )
                ),
                (fun (m, l) -> {| LogModel = l; PinnedFieldName = m.PinnedFieldName |} ),
                (fun bm ->
                    match bm.LogModel with
                    | LogModel.TechnoLog l -> l.Id 
                    | LogModel.TextLog l -> l.Id
                ),
                (Msg.TechnoLogMsg),
                (fun () -> [
                    "Log" |> Binding.oneWayOpt (fun (l: {| LogModel: LogModel; PinnedFieldName: string option |}) ->
                        match l.LogModel with
                        | LogModel.TechnoLog tl -> tl.Fields |> List.map (fun f -> f.TechnoField) |> LogParser.Core.Types.TechnoFields.toString 1 |> Some
                        | LogModel.TextLog t -> t.Log |> Some
                    )

                    "IsTechnoLog" |> Binding.oneWay (fun l -> l.LogModel |> function LogModel.TechnoLog _ -> true | _ -> false)

                    "LogLevel" |> Binding.oneWayOpt (fun (l: {| LogModel: LogModel; PinnedFieldName: string option |}) ->
                        match l.LogModel with
                        | LogModel.TechnoLog tl -> tl.LogLevel |> Some
                        | LogModel.TextLog _ -> None
                    )

                    "Timestamp" |> Binding.oneWayOpt (fun (l: {| LogModel: LogModel; PinnedFieldName: string option |}) ->
                        match l.LogModel with
                        | LogModel.TechnoLog tl -> tl.Timestamp |> Some
                        | LogModel.TextLog _ -> None
                    )

                    "Message" |> Binding.oneWayOpt (fun (l: {| LogModel: LogModel; PinnedFieldName: string option |}) ->
                        match l.LogModel with
                        | LogModel.TechnoLog tl -> tl.Message |> Some
                        | LogModel.TextLog _ -> None
                    )

                    "PinnedValue" |> Binding.oneWayOpt (fun (l: {| LogModel: LogModel; PinnedFieldName: string option |}) ->
                        l.PinnedFieldName
                        |> Option.bind (fun fn ->
                            match l.LogModel with
                            | LogModel.TechnoLog tl -> 
                                tl.Fields
                                |> List.tryFind(fun f -> f.Key = fn)
                                |> Option.bind (fun f -> f.Text )
                            | LogModel.TextLog _ -> None
                        )
                    ) 

                    "HierarchyLevel" |> Binding.oneWay (fun (l: {| LogModel: LogModel; PinnedFieldName: string option |}) ->
                        match l.LogModel with
                        | LogModel.TechnoLog tl -> tl.HierarchyLevel
                        | LogModel.TextLog _ -> 0
                    )

                    "CopyCommand" |> Binding.cmd Msg.CopyLogCommand


                    "Fields" |> Binding.subModelSeq (
                        (fun l ->
                             match l.LogModel with
                             | LogModel.TextLog _ -> []
                             | LogModel.TechnoLog t -> t.Fields),
                        (fun (_, f: TechnoField.Model) -> f),
                        (fun f -> f.Key),
                        (Msg.TechnoFieldMsg),
                        TechnoField.Program.bindings
                     )

                ])
            )

            "TraceId" |> Binding.twoWayOpt ((fun m -> m.TraceId), SetTraceId)
            "LogsDate" |> Binding.twoWayOpt ((fun m -> m.LogsDate), SetLogsDate)
            "KibanaBaseUri" |> Binding.twoWayOpt ((fun m -> m.KibanaBaseUri), SetKibanaBaseUri)
            "KibanaLogin" |> Binding.twoWayOpt ((fun m -> m.KibanaLogin), SetKibanaLogin)
            "KibanaPassword" |> Binding.oneWayToSourceOpt (SetKibanaPassword)

            "SearchKibanaLogsCommand" 
            |> Binding.cmdIf (fun m ->
                m.KibanaPassword
                |> Option.bind (fun _ -> m.KibanaLogin)
                |> Option.bind (fun _ -> m.KibanaBaseUri)
                |> Option.bind (fun _ -> m.TraceId)
                |> Option.map (fun _ -> Start () |> SearchKibanaLogs)
            )

            "ShowAll" |> Binding.twoWay ((fun m -> showAll m), ToggleShowAll)
            "ShowOnlyParsedLogs" |> Binding.oneWay (fun m -> showOnlyParsedLogs m)

            "ErrorMessageQueue" |> Binding.oneWay (fun m -> m.ErrorMessageQueue)
        ]