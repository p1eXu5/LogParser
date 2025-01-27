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
open LogParser.DesktopClient.ElmishApp.Interfaces
open LogParser.DesktopClient.ElmishApp.Models.LogFile


let private toLogModels logs =
    logs
    |> List.map (fun l -> 
        match l with
        | Log.TechLog l ->
            let techLogModel = TechLogModel.init (l)
            LogModel.TechLogModel techLogModel
        | Log.TextLog l ->
            let (textLogModel, _) = TextLogModel.init (l)
            LogModel.TextLogModel textLogModel
    )



let update (settingsManager: ISettingsManager) (logger: ILogger) (msg: Msg) (model: MainModel) =
    match msg with
    | NewFile -> MainModel.init model.ErrorMessageQueue settingsManager None ()

    | OpenFile ->
        let fd = OpenFileDialog()
        fd.Filter <- "text files (*.txt)|*.txt|csv files (*.csv)|*.csv|All files (*.*)|*.*"
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
            },
            Cmd.OfTask.perform ``process`` () InputChanged
        else 
            (model, Cmd.none)

    | OpenSpecifiedFile fileName ->
        let ``process`` () =
                task {
                    let! content = File.ReadAllTextAsync(fileName)
                    return content |> Some
                }

        {
            model with 
                LogFile = LogFile.Existing fileName
                SelectedInput = 1
                Loading = true
        }, Cmd.OfTask.perform ``process`` () InputChanged

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

    // optionally emits InputChanged msg
    | KibanaInputChanged (Some v) when not (String.IsNullOrWhiteSpace(v)) ->
        Kibana.parse2 v
        |> Result.map (fun l -> String.Join(Environment.NewLine, l))
        |> Result.map (fun s ->
            { model with KibanaInput = v |> Some; Loading = true }, Cmd.ofMsg (InputChanged (Some s))
        )
        |> Result.defaultValue (model, Cmd.none)

    // initializes parsing process
    | InputChanged (Some v) when not (String.IsNullOrWhiteSpace(v)) ->
        let parseAsync (v, processId, isCsv) =
            async {
                let v =
                    if isCsv then Csv.parse v
                    else v
                match Parser.parse (v) with
                | Ok xlog ->
                    return (toLogModels xlog), processId
                | Error err ->
                    let (textLogModel, _) = TextLogModel.init (err)
                    //{model with Logs = textLogModel |> LogModel.TextLog |> List.singleton; Input = v |> Some}, Cmd.none
                    return textLogModel |> LogModel.TextLogModel |> List.singleton, processId
            }

        let processId = Guid.NewGuid()
        {
            model with 
                Input = v |> Some; 
                ProcessId = processId
                Loading = true
        }
        , Cmd.OfAsync.either parseAsync (v, processId, model.LogFile |> LogFile.isCsv) LogsChanged Msg.OnError

    | InputChanged _ ->
        {model with Logs = []; Input = None; ProcessId = Guid.Empty}, Cmd.none

    | LogsChanged (logs, processId) when processId = model.ProcessId ->
        {model with Logs = logs; Loading = false}, setFilteringServiceNamesCmd logs

    | PastFromClipboardRequested ->
        let v = Clipboard.GetText() |> Some
        match model.SelectedInput with
        | 0 -> model, Cmd.ofMsg (InputChanged v)
        | 1 -> model, Cmd.ofMsg (KibanaInputChanged v)
        | _ -> model, Cmd.none

    

    | CleanInputRequested ->
        {model with Input = None; KibanaInput = None; Logs = []; ProcessId = Guid.Empty; Loading = false}, Cmd.none

    | OrderByTimestamp ->
        let logs' = model.Logs |> List.sortBy LogModel.timestamp
        let input = logs' |> List.map LogModel.toString |> String.concat "\n"
        {
            model with
                Logs = model.Logs |> List.sortBy (fun l -> l |> LogModel.timestamp)
                Input = input |> Some
        }
        , Cmd.none

    | TechLogMsg (id, TechFieldMsg (key, msg)) ->
        match msg with
        | TechFieldModel.Msg.PinFieldValueInHeader key ->
            { model with PinnedFieldName = key |> Some }, Cmd.none
        | _ ->    
            let log =
                model.Logs
                |> List.choose (function LogModel.TechLogModel tl -> tl |> Some | _ -> None)
                |> List.find (fun l -> l.Id = id)
            
            let field =
                log.Fields
                |> List.find (fun f -> f.Key = key)

            let newField = TechFieldModel.Program.update msg field // TODO: update list when field changing

            model, Cmd.none

    | TechLogMsg (id, Msg.CopyLogCommand) ->
        let log =
            model.Logs
            |> List.choose (function LogModel.TechLogModel tl -> tl |> Some | _ -> None)
            |> List.find (fun l -> l.Id = id)

        Clipboard.SetText(log.Log.Fields |> TechField.toString 1)
        model, Cmd.none

    | KibanaSearchModelMsg kaMsg ->
        let (kibanaSearchModel, kaCmd, intent) = model.KibanaSearchModel |> KibanaSearchModel.Program.update settingsManager logger kaMsg
        let (model, cmd) = model |> setKibanaSearchModel kibanaSearchModel, Cmd.map KibanaSearchModelMsg kaCmd
        match intent with
        | KibanaSearchModel.Intent.NoIntent -> model, cmd
        | KibanaSearchModel.Intent.FilterOff -> model, Cmd.batch [ cmd; Cmd.ofMsg (FiltersModel.Msg.ToggleFilterOn false |> FiltersModelMsg ) ]
        | KibanaSearchModel.Intent.LoadingOn -> { model with Loading = true }, cmd
        | KibanaSearchModel.Intent.ProcessLoadedLogs logs ->
            { model with Loading = false }
            , Cmd.batch [ cmd; Cmd.ofMsg (InputChanged logs) ]
        | KibanaSearchModel.Intent.OnError exn ->
            { model with Loading = false }
            , Cmd.batch [ cmd; Cmd.ofMsg (OnError exn) ]

    | FiltersModelMsg fmsg ->
        let filtersModel = FiltersModel.Program.update fmsg model.FiltersModel
        model |> withFiltersModel filtersModel, Cmd.none

    | ToggleShowAll v ->
        if v then
            { model with ShowMode = ShowMode.All }, Cmd.none
        else
            { model with ShowMode = ShowMode.OnlyParsedLogs }, Cmd.none

    | Msg.OnError ex ->
        model.ErrorMessageQueue.EnqueuError(ex.Message)
        {model with Loading = false}, Cmd.none

    | SetTempTitle v ->
        model |> setTempTitle v, Cmd.none

    | _ -> model, Cmd.none