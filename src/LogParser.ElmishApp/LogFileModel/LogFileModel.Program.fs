namespace LogParser.ElmishApp.LogFileModel

open System

open Elmish
open Elmish.WPF
open p1eXu5.FSharp.ElmishExtensions

open LogParser.ElmishApp
open LogParser.ElmishApp.Models
open LogParser.ElmishApp.Models.LogFileModel
open System.Windows.Input
open System.Windows
open LogParser.App
open LogParser.App.LogRepository
open LogParser.ElmishApp.Interfaces

module Program =

    let update (errorMessageQueue: IErrorMessageQueue) (msg: LogFileModel.Msg) (model: LogFileModel) =
        match msg with
        | Msg.TechLogModelMsg (id, smsg) ->
            model
            |> Model.mapCmd
                _.TechLogModelList
                withTechLogListModel
                (fun l ->
                    l
                    |> List.mapFirstCmd
                        (_.TechLogId >> (=) id)
                        (TechLogModel.Program.update smsg)
                )
                (fun subMsg -> Msg.TechLogModelMsg (id, subMsg))

        | MsgWith.``Start of PastFromClipboardRequested`` model (text, state, cts) ->
            match LogSourceText.create text with
            | Ok t ->
                // Add code here
                { model with ImportLogsState = state }
                , Cmd.OfAsync.perform model.Repo.ParseTextAsync t (fun res ->  AsyncOperation.finishWithin Msg.PastFromClipboardRequested cts res)
            | Error err ->
                model, Cmd.ofMsg (Msg.OnError err)

        | MsgWith.``Finish of PastFromClipboardRequested`` model (state, res) ->
            match res with
            | ParseTextRequestResult.Accepted logFile ->
                { model with ImportLogsState = state }, Cmd.OfAsync.perform model.Repo.GetNextLogBatch () Msg.AppendFirstLogBatch
            | _ ->
                { model with ImportLogsState = state }, Cmd.ofMsg (Msg.OnError $"{res} are not handled yet.")

        | Msg.AppendFirstLogBatch batch ->
            if batch.TechLogIds |> List.isEmpty then
                model, Cmd.none
            else
                {
                    model with
                        TechLogModelList = batch.TechLogIds |> List.map TechLogModel.init
                }
                , Cmd.none

        | Msg.OnError err ->
            errorMessageQueue.EnqueueError(err)
            model, Cmd.none

        | _ -> model, Cmd.none


