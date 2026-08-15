namespace LogParser.ElmishApp.LogFileListModel

open System.Windows.Input

open Elmish.WPF

open LogParser.ElmishApp
open LogParser.ElmishApp.Models
open LogParser.ElmishApp.Models.LogFileListModel

type IBindings =
    interface
        abstract SelectedLogFileId: int
        abstract LogFiles: LogFileModel.IBindings seq
        abstract SelectedLogFile: LogFileModel.IBindings
        abstract AddNewLogFileCommand: ICommand
        abstract SelectLogFileIdCommand: ICommand
    end

module Bindings =

    let private __ = Unchecked.defaultof<IBindings>

    let bindings () =
        [
            nameof __.SelectedLogFileId
                |> Binding.oneWay _.SelectedLogFileModelId

            nameof __.LogFiles
                |> Binding.subModelSeq (
                    (fun m -> m.LogFileModelList),
                    (fun (_, sm) -> sm),
                    (fun sm -> sm.Id),
                    Msg.LogFileModelMsg,
                    LogFileModel.Bindings.bindings
                )

            nameof __.SelectedLogFile
                |> Binding.SubModel.required (LogFileModel.Bindings.bindings)
                |> Binding.mapModel (fun m -> m.LogFileModelList |> List.find (_.Id >> (=) m.SelectedLogFileModelId))
                |> Binding.mapMsgWithModel (fun smsg model -> Msg.LogFileModelMsg (model.SelectedLogFileModelId, smsg))

            nameof __.AddNewLogFileCommand
                |> Binding.cmd Msg.AddNewLogFile

            nameof __.SelectLogFileIdCommand
                |> Binding.cmdParam (fun o -> Msg.SelectLogFileId (o :?> int))
        ]

