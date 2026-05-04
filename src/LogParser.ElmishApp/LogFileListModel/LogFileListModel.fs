namespace LogParser.ElmishApp.Models

open System

type LogFileListModel =
    {
        LogFileList: LogFileModel list
        SelectedLogFileId: Guid
    }

module LogFileListModel =

    open System
    open LogParser.ElmishApp.Types

    type Msg =
        | LogFileModelMsg of Guid * LogFileModel.Msg

    let init () =
        let logFileModel = LogFileModel.initNew (true)
        {
            LogFileList =
                [
                    logFileModel
                ]
            SelectedLogFileId = logFileModel.Id
        }

    let selecteLogFileName (m: LogFileListModel) =
        m.LogFileList
        |> List.tryFind (fun f -> f.Id = m.SelectedLogFileId)
        |> Option.map (fun f -> f |> LogFileModel.fileName)

    let selecteLogFileNameAndPath (m: LogFileListModel) =
        m.LogFileList
        |> List.tryFind (fun f -> f.Id = m.SelectedLogFileId)
        |> Option.bind (fun f ->
            match f.State with
            | FileState.Existing ->
                (f |> LogFileModel.fileName, f.FullPath)
                |> Some
            | _ -> None
        )

    let inline withLogFileList logFileList (m: LogFileListModel) =
        { m with LogFileList = logFileList }

namespace LogParser.ElmishApp.LogFileListModel

open System
open Elmish.WPF
open p1eXu5.FSharp.ElmishExtensions
open LogParser.ElmishApp
open LogParser.ElmishApp.Models
open LogParser.ElmishApp.Models.LogFileListModel

module Program =

    let update msg model =
        match msg with
        | Msg.LogFileModelMsg (id, smsg) ->
            model
            |> Model.mapHandleIntent _.LogFileList withLogFileList
                (List.mapFirstIntent (_.Id >> (=) id) (LogFileModel.Program.update smsg) LogFileModel.Intent.None)
                (fun intent m ->
                    match intent with
                    | LogFileModel.Intent.Select id -> { m with SelectedLogFileId = id }
                    | _ -> m
                )

type IBindings =
    interface
        abstract LogFiles: obj
        abstract SelectedLogFile: LogFileModel.IBindings
    end

module Bindings =


    let private __ = Unchecked.defaultof<IBindings>

    let bindings () =
        [
            nameof __.LogFiles
                |> Binding.subModelSeq (
                    (fun m -> m.LogFileList),
                    (fun (_, sm) -> sm),
                    (fun sm -> sm.Id),
                    Msg.LogFileModelMsg,
                    LogFileModel.SelectedFileNameBindings.bindings
                )

            nameof __.SelectedLogFile
                |> Binding.SubModel.required (LogFileModel.Bindings.bindings)
                |> Binding.mapModel (fun m -> m.LogFileList |> List.find (_.Id >> (=) m.SelectedLogFileId))
                |> Binding.mapMsgWithModel (fun smsg model -> Msg.LogFileModelMsg (model.SelectedLogFileId, smsg))
        ]