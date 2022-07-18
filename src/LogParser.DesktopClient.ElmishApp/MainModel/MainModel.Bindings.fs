module LogParser.DesktopClient.ElmishApp.MainModel.Bindings

open System.Windows

open Elmish.Extensions
open Elmish.WPF

open LogParser.DesktopClient.ElmishApp
open LogParser.DesktopClient.ElmishApp.Models
open LogParser.DesktopClient.ElmishApp.Models.MainModel






let bindings () : Binding<MainModel, MainModel.Msg> list =
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
                    (fun (_, f: TechnoField) -> f),
                    (fun f -> f.Key),
                    (Msg.TechnoFieldMsg),
                    TechnoField.Bindings.bindings
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

        "ShowAll" |> Binding.twoWay ((fun m -> MainModel.showAll m), ToggleShowAll)
        "ShowOnlyParsedLogs" |> Binding.oneWay (fun m -> MainModel.showOnlyParsedLogs m)

        "ErrorMessageQueue" |> Binding.oneWay (fun m -> m.ErrorMessageQueue)
    ]