namespace LogParser.App.MainModel

open System
open System.Windows;

open Elmish

open LogParser.App
open LogParser.Core.Parser
open LogParser.Core.Types
open Microsoft.Win32
open System.IO
open Elmish.Extensions
open System
open LogParser.Core


type LogFile =
    | Existing of string
    | New


type internal MainModel =
    {
        AssemblyVersion: string
        LogFile: LogFile
        Input: string option
        //Output: string option
        Logs: LogModel list
        ProcessId: Guid
        Loading: bool

        TraceId: string option
        LogsDate: DateTime option
        KibanaBaseUri: string option
        KibanaLogin: string option
        KibanaPassword: string option

    }
and
    LogModel =
        | TechnoLog of TechnoLog.Model
        | TextLog of TextLog.Model


type Msg =
    | InputChanged of string option
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
    | SearchKibanaLogs of Operation<(string * string * DateTime option), string seq>
    | SetTraceId of string option
    | SetLogsDate of DateTime option
    | SetKibanaBaseUri of string option
    | SetKibanaLogin of string option
    | SetKibanaPassword of string option
    

module internal Program =

    let init () =

        let assemblyVer = "Version" + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()
        {
            AssemblyVersion = assemblyVer
            LogFile = LogFile.New
            Input = None
            //Output = None
            Logs = []
            ProcessId = Guid.Empty
            Loading = false

            TraceId = None
            LogsDate = None
            KibanaBaseUri = None
            KibanaLogin = None
            KibanaPassword = None
        },
        Cmd.none

    // TODO: move to parser
    //let fixStringData (s: string) =
    //    s.Replace('\u00a0', ' ')


    let update (msg: Msg) (model: MainModel) =
        match msg with
        | NewFile -> init ()

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

        | InputChanged (Some v) when not (String.IsNullOrWhiteSpace(v)) ->
            let ``process`` (v, processId) =
                async {
                    match parse (v) with
                    | Ok xlog ->
                
                        let logs =
                            xlog
                            |> List.map (function
                                | Log.TechnoLog l ->
                                    let technoLogModel = TechnoLog.Program.init (l)
                                    LogModel.TechnoLog technoLogModel
                                | Log.TextLog l ->
                                    let (textLogModel, _) = TextLog.Program.init (l)
                                    LogModel.TextLog textLogModel
                            )
                        //{model with Logs = logs; Input = v |> Some}, Cmd.none
                        return logs, processId

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
            , Cmd.OfAsync.perform ``process`` (v, processId) LogsChanged

        | InputChanged _ ->
            {model with Logs = []; Input = None; ProcessId = Guid.Empty}, Cmd.none

        | LogsChanged (logs, processId) when processId = model.ProcessId ->
            {model with Logs = logs; Loading = false}, Cmd.none

        | PastFromClipboardRequested ->
            let v = Clipboard.GetText() |> Some
            model, Cmd.ofMsg (InputChanged v)

        | CleanInputRequested ->
            {model with Input = None; Logs = []; ProcessId = Guid.Empty; Loading = false}, Cmd.none

        | TechnoLogMsg (id, TechnoFieldMsg (key, msg)) ->
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

        | SearchKibanaLogs (Start (traceId, uri, logsDate)) ->
            model, Cmd.OfTask.perform (Kibana.searchLogs uri logsDate) traceId (Finish >> SearchKibanaLogs)

        | SearchKibanaLogs (Finish logs) when logs |> (not << Seq.isEmpty) ->
            let logMessage = String.Join(Environment.NewLine, logs) |> Some
            model, Cmd.ofMsg (InputChanged logMessage)

        | _ -> model, Cmd.none


    // =====================
    //       Bindings
    // =====================
    open Elmish.WPF

    let bindings () : Binding<MainModel, Msg> list =
        [
            "AssemblyVersion" |> Binding.oneWay (fun m -> m.AssemblyVersion)

            "Input" |> Binding.twoWayOpt ((fun m -> m.Input), Msg.InputChanged)
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

            "Logs" |> Binding.subModelSeq (
                (fun m -> m.Logs),
                (fun (m, l) -> l),
                (function LogModel.TechnoLog l -> l.Id | LogModel.TextLog l -> l.Id),
                (Msg.TechnoLogMsg),
                (fun () -> [
                    "Log" |> Binding.oneWayOpt (fun l ->
                        match l with
                        | LogModel.TechnoLog tl -> tl.Fields |> List.map (fun f -> f.TechnoField) |> LogParser.Core.Types.TechnoFields.toString 1 |> Some
                        | LogModel.TextLog t -> t.Log |> Some
                    )

                    "IsTechnoLog" |> Binding.oneWay (function LogModel.TechnoLog _ -> true | _ -> false)

                    "Header" |> Binding.oneWayOpt (fun l ->
                        match l with
                        | LogModel.TechnoLog tl -> tl.Header |> Some
                        | LogModel.TextLog _ -> None
                    )

                    "CopyCommand" |> Binding.cmd Msg.CopyLogCommand

                    "Fields" |> Binding.subModelSeq (
                        (fun l ->
                             match l with
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
                |> Option.bind (fun uri -> m.TraceId |> Option.map (fun traceId -> (traceId, uri)))
                |> Option.map (fun t -> (fst t, snd t, m.LogsDate) |> Start |> SearchKibanaLogs)
            )
        ]