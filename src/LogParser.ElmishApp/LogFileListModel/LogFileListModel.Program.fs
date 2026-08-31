namespace LogParser.ElmishApp.LogFileListModel

open System
open Elmish
open p1eXu5.FSharp.ElmishExtensions
open LogParser.ElmishApp
open LogParser.ElmishApp.Models
open LogParser.ElmishApp.Models.LogFileListModel

module Program =

    let update
        (updateLogFileModel)
        msg 
        model =
        match msg with
        | Msg.SelectLogFileId id ->
            { model with SelectedLogFileModelId = id }
            , Cmd.none

        | Msg.LogFileModelMsg (id, smsg) ->
            model
            |> Model.mapCmd 
                _.LogFileModelList
                withLogFileList
                (List.mapFirstCmd (_.Id >> (=) id) (updateLogFileModel smsg))
                (fun subMsg -> Msg.LogFileModelMsg (id, subMsg))

        | Msg.AddNewLogFile ->
            model |> addLogFileModel
            , Cmd.none
