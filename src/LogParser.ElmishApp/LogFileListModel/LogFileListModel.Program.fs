namespace LogParser.ElmishApp.LogFileListModel

open System
open Elmish
open p1eXu5.FSharp.ElmishExtensions
open LogParser.ElmishApp
open LogParser.ElmishApp.Models
open LogParser.ElmishApp.Models.LogFileListModel

module Program =

    let update msg model =
        match msg with
        | Msg.SelectLogFileId id ->
            { model with SelectedLogFileModelId = id }
            , Cmd.none

        | Msg.LogFileModelMsg (id, smsg) ->
            model
            |> Model.mapCmd 
                _.LogFileModelList
                withLogFileList
                (List.mapFirstCmd (_.Id >> (=) id) (LogFileModel.Program.update smsg))
                Msg.LogFileModelMsg

        | Msg.AddNewLogFile ->
            model |> addLogFileModel
            , Cmd.none
