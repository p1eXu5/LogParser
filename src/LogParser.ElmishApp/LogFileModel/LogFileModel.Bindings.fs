namespace LogParser.ElmishApp.LogFileModel

open System.Windows
open System.Windows.Input

open Elmish
open Elmish.WPF
open p1eXu5.FSharp.ElmishExtensions
open Nest

open LogParser.App
open LogParser.App.LogRepository
open LogParser.ElmishApp
open LogParser.ElmishApp.Models
open LogParser.ElmishApp.Models.LogFileModel
open LogParser.ElmishApp.Helpers

type IBindings =
    interface
        abstract Id: int with get
        abstract FileName: string option with get
        abstract Title: string with get
        abstract LogCount: int with get
        abstract TechLogList: TechLogListModel.IBindings seq with get
        abstract SelectedTechLogId: TechLogId voption with get, set
        abstract IsLoading: bool with get
        abstract LoadNextLogBatchCommand: ICommand
        abstract PasteFromClipboardCommand: ICommand
        abstract ParseCommand: ICommand
        abstract OrderByTimestampCommand: ICommand
    end

module Bindings =

    let __ = Unchecked.defaultof<IBindings>

    let bindings () : Binding<LogFileModel, LogFileModel.Msg> list =
        [
            nameof __.Id
                |> Binding.oneWay _.Id

            nameof __.FileName
                |> Binding.oneWay LogFileModel.fileNameWithoutExtension

            nameof __.Title
                |> Binding.oneWay LogFileModel.title

            nameof __.LogCount
                |> Binding.oneWay (fun m -> m.TechLogModelList |> List.length)

            nameof __.TechLogList
                |> Binding.subModelSeq (
                    (fun m -> m.TechLogModelList),
                    (fun (m, sm) ->
                        let repo = m.Repo
                        let logs = repo.GetTechLogs (m.TechLogModelList |> List.map _.TechLogId) m.CacheKey
                        (sm, logs[sm.TechLogId])
                    ),
                    (fst >> _.TechLogId),
                    Msg.TechLogModelMsg,
                    TechLogModel.Bindings.bindings
                )

            nameof __.SelectedTechLogId
                |> Binding.subModelSelectedItem (
                    nameof __.TechLogList,
                    _.SelectedTechLogId,
                    Msg.SetSelectedTechLogId
                )

            nameof __.LoadNextLogBatchCommand
                |> Binding.cmdIf (fun m -> None)

            nameof __.IsLoading
                |> Binding.oneWay (fun m -> m.ImportLogsState |> function AsyncDeferredState.InProgress _ -> true | _ -> true)

            nameof __.PasteFromClipboardCommand |> Binding.cmdIf (fun (m) ->
                match m.ImportLogsState, Clipboard.ContainsText() with
                | AsyncDeferredState.Retrieved, true
                | AsyncDeferredState.NotRequested, true ->
                    Msg.PastFromClipboardRequested |> AsyncOperation.startWith (Clipboard.GetText()) |> Some
                | _ ->
                    None
            )

            nameof __.ParseCommand |> Binding.cmdParamIf (fun str (m) ->
                let s = str :?> string
                if s |> notEmpty then
                    match m.ImportLogsState with
                    | AsyncDeferredState.Retrieved
                    | AsyncDeferredState.NotRequested ->
                        Msg.PastFromClipboardRequested |> AsyncOperation.startWith s |> Some
                    | _ ->
                        None
                else
                    None
            )

            // Temporary disabled:
            nameof __.OrderByTimestampCommand |> Binding.cmdIf (fun m ->
                None |> Option.map (fun _ -> Msg.OrderByTimestamp))

            (*

            "ClearInputCommand" |> Binding.cmdIf (fun m -> m.Input |> Option.map (fun _ -> CleanInputRequested))
*)
        ]