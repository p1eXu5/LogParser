module LogParser.DesktopClient.ElmishApp.MainModel.Bindings

open System
open System.Windows
open Elmish.WPF
open LogParser.DesktopClient.ElmishApp
open LogParser.DesktopClient.ElmishApp.Models
open LogParser.DesktopClient.ElmishApp.Models.MainModel


let logBindings () =
    [
        "Log" |> Binding.oneWayOpt (fun (l: {| LogModel: LogModel; PinnedFieldName: string option |}) ->
            match l.LogModel with
            | LogModel.TechLogModel tl -> tl.Fields |> List.map (fun f -> f.TechField) |> LogParser.Core.Types.TechField.toString 1 |> Some
            | LogModel.TextLogModel t -> t.Log |> Some
        )

        "IsTechLog" |> Binding.oneWay (fun l -> l.LogModel |> function LogModel.TechLogModel _ -> true | _ -> false)

        "LogLevel" |> Binding.oneWayOpt (fun (l: {| LogModel: LogModel; PinnedFieldName: string option |}) ->
            match l.LogModel with
            | LogModel.TechLogModel tl -> tl.LogLevel |> Some
            | LogModel.TextLogModel _ -> None
        )

        "Timestamp" |> Binding.oneWayOpt (fun (l: {| LogModel: LogModel; PinnedFieldName: string option |}) ->
            match l.LogModel with
            | LogModel.TechLogModel tl -> tl.Timestamp
            | LogModel.TextLogModel _ -> None
        )

        "Message" |> Binding.oneWayOpt (fun (l: {| LogModel: LogModel; PinnedFieldName: string option |}) ->
            match l.LogModel with
            | LogModel.TechLogModel tl -> tl.Message |> Some
            | LogModel.TextLogModel _ -> None
        )

        "PinnedValue" |> Binding.oneWayOpt (fun (l: {| LogModel: LogModel; PinnedFieldName: string option |}) ->
            l.PinnedFieldName
            |> Option.bind (fun fn ->
                match l.LogModel with
                | LogModel.TechLogModel tl -> 
                    tl.Fields
                    |> List.tryFind(fun f -> f.Key = fn)
                    |> Option.bind (fun f -> f.Text )
                | LogModel.TextLogModel _ -> None
            )
        ) 

        "HierarchyLevel" |> Binding.oneWay (fun (l: {| LogModel: LogModel; PinnedFieldName: string option |}) ->
            match l.LogModel with
            | LogModel.TechLogModel tl -> tl.HierarchyLevel
            | LogModel.TextLogModel _ -> 0
        )

        "CopyCommand" |> Binding.cmd Msg.CopyLogCommand


        "Fields" |> Binding.subModelSeq (
            (fun l ->
                    match l.LogModel with
                    | LogModel.TextLogModel _ -> []
                    | LogModel.TechLogModel t -> t.Fields),
            (fun (_, f: TechFieldModel) -> f),
            (fun f -> f.Key),
            (Msg.TechFieldMsg),
            TechFieldModel.Bindings.bindings
            )
    ]


let bindings () : Binding<MainModel, MainModel.Msg> list =
    [
        "AssemblyVersion" |> Binding.oneWay (fun m -> m.AssemblyVersion)

        "KibanaSearchModel"
            |> Binding.SubModel.required KibanaSearchModel.Bindings.bindings
            |> Binding.mapModel getKibanaSearchModel
            |> Binding.mapMsg KibanaSearchModelMsg

        "FiltersModel"
            |> Binding.SubModel.required FiltersModel.Bindings.bindings
            |> Binding.mapModel filtersModel
            |> Binding.mapMsg FiltersModelMsg

        "DockerInput" |> Binding.twoWayOpt ((fun m -> m.Input), Msg.InputChanged)
        "KibanaInput" |> Binding.twoWayOpt ((fun m -> m.KibanaInput), Msg.KibanaInputChanged)
        "SelectedInput" |> Binding.twoWay ((fun m -> m.SelectedInput), Msg.SetSelectedInput)

        //"Output" |> Binding.oneWayOpt (fun m -> m.Output)

        "Loading" |> Binding.oneWay (fun m -> m.Loading)

        "PasteFromClipboardCommand" |> Binding.cmdIf (fun _ ->
            if Clipboard.ContainsText() then
                PastFromClipboardRequested |> Some
            else
                None
        )

        "ClearInputCommand" |> Binding.cmdIf (fun m -> m.Input |> Option.map (fun _ -> CleanInputRequested))
        "OrderByTimestampCommand" |> Binding.cmdIf (fun m -> if m.Loading then None else m.Input |> Option.map (fun _ -> Msg.OrderByTimestamp))

        "DrugFileCommand" |> Binding.cmdParamIf (fun s ->
            match s with
            | :? string as fileName -> fileName |> OpenSpecifiedFile |> Some
            | _ -> None
        )

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

        "TempTitle" |> Binding.twoWayOpt (getTempTitle, Msg.SetTempTitle)

        "DocumentName" |> Binding.oneWay getDocumentName

        "Logs" |> Binding.subModelSeq (
            getFilteredLogModels,
            (fun (m, l) -> {| LogModel = l; PinnedFieldName = m.PinnedFieldName |} ),
            (fun bm -> bm.LogModel |> LogModel.logId),
            Msg.TechLogMsg,
            logBindings
        )

        "LogCount" |> Binding.oneWay (fun m -> m.Logs.Length)

        "ShowAll" |> Binding.twoWay ((fun m -> MainModel.showAll m), ToggleShowAll)
        "ShowOnlyParsedLogs" |> Binding.oneWay (fun m -> MainModel.showOnlyParsedLogs m)

        "ErrorMessageQueue" |> Binding.oneWay (fun m -> m.ErrorMessageQueue)
    ]