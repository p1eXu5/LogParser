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
        | Msg.TechLogListModelMsg smsg ->
            model
            |> Model.map _.TechLogListModel withTechLogListModel (TechLogListModel.Program.update smsg)
            , Cmd.none
        | Msg.PastFromClipboardRequested text ->
            // Add code here
            model, Cmd.none

