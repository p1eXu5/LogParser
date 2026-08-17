namespace LogParser.ElmishApp.LogFileModel

open Elmish
open Elmish.WPF
open p1eXu5.FSharp.ElmishExtensions

open LogParser.ElmishApp
open LogParser.ElmishApp.Models
open LogParser.ElmishApp.Models.LogFileModel
open System.Windows.Input
open System.Windows
open LogParser.ElmishApp.Helpers

type IBindings =
    interface
        abstract Id: int with get
        abstract FileName: string option with get
        abstract Title: string with get
        abstract LogCount: int with get
        abstract TechLogList: TechLogListModel.IBindings seq with get
        abstract PasteFromClipboardCommand: ICommand
        abstract PasteCommand: ICommand
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
                |> Binding.oneWay (fun m -> m.TechLogListModel |> TechLogListModel.length)

            nameof __.TechLogList
                |> Binding.SubModel.required TechLogListModel.Bindings.bindings
                |> Binding.mapModel _.TechLogListModel
                |> Binding.mapMsg Msg.TechLogListModelMsg

            nameof __.PasteFromClipboardCommand |> Binding.cmdIf (fun (m: LogFileModel) ->
                match m.ImportLogsState, Clipboard.ContainsText() with
                | AsyncDeferredState.Retrieved, true
                | AsyncDeferredState.NotRequested, true ->
                    Msg.PastFromClipboardRequested |> AsyncOperation.startWith (Clipboard.GetText()) |> Some
                | _ ->
                    None
            )

            nameof __.PasteCommand |> Binding.cmdParamIf (fun str (m: LogFileModel) ->
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
        ]