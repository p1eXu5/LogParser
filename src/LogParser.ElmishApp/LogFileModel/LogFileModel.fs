namespace LogParser.ElmishApp.Models

open LogParser.ElmishApp.Types
open System.IO

type LogFileModel =
    {
        Id: int
        IsSelected: bool // TODO: remove
        State: FileState
        FullPath: string
        TechLogListModel: TechLogListModel
    }

module LogFileModel =

    open System

    type Msg = 
        | SetIsSelected of bool
        | TechLogListModelMsg of TechLogListModel.Msg
        | PastFromClipboardRequested

    [<RequireQualifiedAccess>]
    type Intent =
        | Select of Guid
        | None

    let initNew (id: int, isSelected: bool) =
        let tmpFilePath = Path.GetTempFileName()
        {
            Id = id
            IsSelected = isSelected
            State = FileState.NewTemp
            FullPath = tmpFilePath
            TechLogListModel = TechLogListModel.init []
        }

    let inline fileNameWithoutExtension (m: LogFileModel) =
        System.IO.Path.GetFileNameWithoutExtension(m.FullPath)

    let fileName (m: LogFileModel) =
        match m.State with
        | FileState.NewTemp -> "New"
        | FileState.Existing -> m |> fileNameWithoutExtension

    let inline withTechLogListModel techLogListModel (m: LogFileModel) =
        { m with TechLogListModel = techLogListModel }


namespace LogParser.ElmishApp.LogFileModel

open Elmish
open Elmish.WPF
open p1eXu5.FSharp.ElmishExtensions

open LogParser.ElmishApp
open LogParser.ElmishApp.Models
open LogParser.ElmishApp.Models.LogFileModel
open System.Windows.Input
open System.Windows

module Program =

    // return value can be changed on LogFileModel * Cmd<LogFileModel.Msg> * Intent
    let update (msg: LogFileModel.Msg) (model: LogFileModel) =
        match msg with
        | Msg.SetIsSelected false ->
            { model with IsSelected = false }, Intent.None
        | Msg.SetIsSelected true ->
            { model with IsSelected = true }, Intent.Select model.Id
        | Msg.TechLogListModelMsg smsg ->
            model
            |> Model.map _.TechLogListModel withTechLogListModel (TechLogListModel.Program.update smsg)
            , Intent.None
        | Msg.PastFromClipboardRequested ->
            // Add code here
            model, Intent.None


type ISelectedFileNameBindings =
    interface
        abstract FileName: string with get
        abstract IsSelected: bool with get
    end

module SelectedFileNameBindings =

    let __ = Unchecked.defaultof<ISelectedFileNameBindings>

    let bindings () : Binding<LogFileModel, LogFileModel.Msg> list =
        [
            nameof __.FileName
                |> Binding.oneWay LogFileModel.fileName

            nameof __.IsSelected
                |> Binding.twoWay (_.IsSelected, Msg.SetIsSelected)
        ]


type IBindings =
    interface
        abstract FileName: string with get
        abstract LogCount: int with get
        abstract TechLogList: TechLogListModel.IBindings seq with get
        abstract PasteFromClipboardCommand: ICommand
    end

module Bindings =

    let __ = Unchecked.defaultof<IBindings>

    let bindings () : Binding<LogFileModel, LogFileModel.Msg> list =
        [
            nameof __.FileName
                |> Binding.oneWay LogFileModel.fileName

            nameof __.LogCount
                |> Binding.oneWay (fun m -> m.TechLogListModel |> TechLogListModel.length)

            nameof __.TechLogList
                |> Binding.SubModel.required TechLogListModel.Bindings.bindings
                |> Binding.mapModel _.TechLogListModel
                |> Binding.mapMsg Msg.TechLogListModelMsg

            nameof __.PasteFromClipboardCommand |> Binding.cmdIf (fun _ ->
                if Clipboard.ContainsText() then
                    Msg.PastFromClipboardRequested |> Some
                else
                    None
            )
        ]