namespace LogParser.DesktopClient.ElmishApp.Models

open System

type TextLogModel =
    {
        Id: Guid
        Log: string
    }


module TextLogModel =

    type Msg =
        | CopyLog

    open Elmish


    let init (log: string) =
        {
            Id = Guid.NewGuid()
            Log = log
        }
        , Cmd.none


// -----------------------------------------------------

namespace LogParser.DesktopClient.ElmishApp.TextLogModel

open LogParser.DesktopClient.ElmishApp.Models
open LogParser.DesktopClient.ElmishApp.Models.TextLogModel

module Program =

    open System.Windows

    let update msg model =
        match msg with
        | CopyLog ->
            Clipboard.SetText(model.Log)

module Bindings =

    open Elmish.WPF

    let bindings : Binding<TextLogModel, TextLogModel.Msg> list =
        [
            "Log" |> Binding.oneWay (fun m -> m.Log)
            "CopyCommand" |> Binding.cmd TextLogModel.Msg.CopyLog
        ]