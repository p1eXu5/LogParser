namespace LogParser.ElmishApp.MainModel

open System
open System.Windows
open System.Windows.Input
open Elmish.WPF
open LogParser.ElmishApp
open LogParser.ElmishApp.Models
open LogParser.ElmishApp.Models.MainModel
open LogParser.ElmishApp.Interfaces
open LogParser.ElmishApp.Types

/// Design time bindings
type IBindings =
    interface
        abstract Title: string
        abstract AssemblyVersion: string
        abstract ErrorMessageQueue: IErrorMessageQueue
        //abstract TempTitle: string option with get, set // TODO: move to LogFileModel

        abstract ShowAll: bool with get, set
        abstract ShowOnlyParsedLogs: bool

        abstract LogFileList: LogFileListModel.IBindings

        // ToolBars
        abstract KibanaSearchModel: obj with get
        abstract IsKibanaSearchModelLoaded: bool with get, set

        abstract FiltersModel: obj with get
        abstract IsFiltersModelLoaded: bool with get, set

        // Menu
        abstract OpenLogsFileCommand: ICommand with get
        abstract SaveLogsFileAsCommand: ICommand with get
        abstract SaveLogsFileCommand: ICommand with get
        abstract NewFileCommand: ICommand with get

        abstract DrugFileCommand: ICommand with get
    end

module Bindings =

    let private __ = Unchecked.defaultof<IBindings>

    let bindings
        (title: string)
        (assemblyVersion: string)
        (mainErrorMessageQueue: IErrorMessageQueue)
        (dialogErrorMessageQueue: IErrorMessageQueue)
        : Binding<MainModel, MainModel.Msg> list
        =
        [
            nameof __.AssemblyVersion
                |> Binding.oneWay (fun _ -> assemblyVersion)

            nameof __.ErrorMessageQueue |> Binding.oneWay (fun _ -> mainErrorMessageQueue)
            
            nameof __.Title
                |> Binding.oneWay (fun m ->
                    m |> MainModel.documentNameTitle title)

            // TODO: move to LogFileModel
            // nameof __.TempTitle
            //     |> Binding.twoWayOpt (_.TempTitle, Msg.SetTempTitle)

            nameof __.KibanaSearchModel
                |> Binding.SubModel.opt KibanaSearchModel.Bindings.bindings
                |> Binding.mapModel getKibanaSearchModel
                |> Binding.mapMsg KibanaSearchModelMsg

            nameof __.IsKibanaSearchModelLoaded
                |> Binding.twoWay (
                    (fun m -> m.KibanaSearchModel.IsSome),
                    (fun v ->
                        if v then Msg.LoadKibanaSearchModel
                        else  Msg.UnloadKibanaSearchModel
                    )
                )

            // TODO: move to LogFileModel
            nameof __.FiltersModel
                |> Binding.SubModel.opt FiltersModel.Bindings.bindings
                |> Binding.mapModel filtersModel
                |> Binding.mapMsg FiltersModelMsg

            nameof __.IsFiltersModelLoaded
                |> Binding.twoWay (
                    (fun m -> m.FiltersModel.IsSome),
                    (fun v ->
                        if v then Msg.LoadFiltersModel
                        else  Msg.UnloadFiltersModel
                    )
                )

            nameof __.ShowAll
                |> Binding.twoWay ((fun m -> MainModel.showAll m), ToggleShowAll)

            nameof __.ShowOnlyParsedLogs
                |> Binding.oneWay (fun m -> MainModel.showOnlyParsedLogs m)

            nameof __.LogFileList
                |> Binding.SubModel.required LogFileListModel.Bindings.bindings
                |> Binding.mapModel _.LogFileListModel
                |> Binding.mapMsg Msg.LogFileListModelMsg

            nameof __.OpenLogsFileCommand |> Binding.cmd Msg.OpenFile
            nameof __.SaveLogsFileAsCommand |> Binding.cmdIf (fun m -> None |> Option.map (fun _ -> Msg.SaveFileAs))
            nameof __.SaveLogsFileCommand |> Binding.cmdIf (fun m -> None |> Option.map (fun _ -> Msg.SaveFile))
            nameof __.NewFileCommand |> Binding.cmdIf (fun m -> None |> Option.map (fun _ -> Msg.NewFile))

            nameof __.DrugFileCommand |> Binding.cmdParamIf (fun s ->
                match s with
                | :? string as fileName -> fileName |> Msg.OpenSpecifiedFile |> Some
                | _ -> None
            )

            (*
            "DockerInput" |> Binding.twoWayOpt ((fun m -> m.Input), Msg.InputChanged)
            "KibanaInput" |> Binding.twoWayOpt ((fun m -> m.KibanaInput), Msg.KibanaInputChanged)
            "SelectedInput" |> Binding.twoWay ((fun m -> m.SelectedInput), Msg.SetSelectedInput)

            //"Output" |> Binding.oneWayOpt (fun m -> m.Output)

            "Loading" |> Binding.oneWay (fun m -> m.Loading)

            
            "DocumentName" |> Binding.oneWay getDocumentName

            "Logs" |> Binding.subModelSeq (
                getFilteredLogModels,
                (fun (m, l) -> {| LogModel = l; PinnedFieldName = m.PinnedFieldName |} ),
                (fun bm -> bm.LogModel |> TechLogModel.logId),
                Msg.TechLogMsg,
                logBindings
            )
            
            *)
        ]