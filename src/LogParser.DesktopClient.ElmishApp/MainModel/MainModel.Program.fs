module LogParser.DesktopClient.ElmishApp.MainModel.Program

open System
open System.IO
open Microsoft.Extensions.Logging
open Microsoft.Win32

open Elmish
open Elmish.Extensions
open FsToolkit.ErrorHandling

open LogParser.Core

open LogParser.DesktopClient.ElmishApp
open LogParser.DesktopClient.ElmishApp.Models
open LogParser.DesktopClient.ElmishApp.Models.MainModel
open System.Windows
open LogParser.Core.Types


let private toLogModels logs =
    logs
    |> List.map (fun l -> 
        match l with
        | Log.TechnoLog l ->
            let technoLogModel = TechnoLog.init (l)
            LogModel.TechnoLog technoLogModel
        | Log.TextLog l ->
            let (textLogModel, _) = TextLog.init (l)
            LogModel.TextLog textLogModel
    )


let update (logger: ILogger) (msg: Msg) (model: MainModel) =
    match msg with
    | NewFile -> MainModel.init model.ErrorMessageQueue model.SettingsManager ()

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
                match Parser.parse (v) with
                | Ok xlog ->
                    return (toLogModels xlog), processId

                | Error err ->
                    let (textLogModel, _) = TextLog.init (err)
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
    | SetKibanaBaseUri uri -> { model with KibanaBaseUri = uri }, Cmd.ofMsg Msg.SaveKibanaBaseUri
    | SetKibanaLogin login -> { model with KibanaLogin = login }, Cmd.ofMsg Msg.SaveKibanaLogin
    | SetKibanaPassword pass -> { model with KibanaPassword = pass }, Cmd.none

    | SearchKibanaLogs (Start ()) ->
        { model with Loading = true }
        , Cmd.OfTask.either 
            (
                Kibana.searchLogs logger
                    (model.KibanaBaseUri |> Option.get) 
                    model.LogsDate 
                    (model.KibanaLogin |> Option.get) 
                    (model.KibanaPassword |> Option.get)
            ) 
            (model.TraceId |> Option.get)
            (Finish >> SearchKibanaLogs)
            Msg.OnError

    | SearchKibanaLogs (Finish logs) when logs |> (not << Seq.isEmpty) ->
        let logMessage = String.Join(Environment.NewLine, logs) |> Some
        { model with Loading = false }, Cmd.ofMsg (InputChanged logMessage)

    | ToggleShowAll v ->
        if v then
            { model with ShowMode = ShowMode.All }, Cmd.none
        else
            { model with ShowMode = ShowMode.OnlyParsedLogs }, Cmd.none

    | ToggleShowInnerHierarchyLogs v ->
        { model with ShowInnerHierarchyLogs = v }, Cmd.none

    | SaveKibanaBaseUri when model.KibanaBaseUri |> Option.isSome ->
        model.KibanaBaseUri 
        |> Option.iter (fun s -> model.SettingsManager.Save "KibanaBaseUri" s)
        model, Cmd.none

    | SaveKibanaLogin when model.KibanaBaseUri |> Option.isSome ->
        model.KibanaLogin 
        |> Option.iter (fun s -> model.SettingsManager.Save "KibanaLogin" s)
        model, Cmd.none

    | Msg.OnError ex ->
        model.ErrorMessageQueue.EnqueuError(ex.Message)
        {model with Loading = false}, Cmd.none

    | _ -> model, Cmd.none