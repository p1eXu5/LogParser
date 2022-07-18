namespace LogParser.DesktopClient.ElmishApp.Models

open System

type TextLog =
    {
        Id: Guid
        Log: string
    }


module TextLog =

    type Msg = Msg

    open Elmish


    let init (log: string) =
        {
            Id = Guid.NewGuid()
            Log = log
        }
        , Cmd.none



namespace LogParser.DesktopClient.ElmishApp.TextLog

open LogParser.DesktopClient.ElmishApp.Models

module Program =


    open Elmish.WPF

    let bindings : Binding<TextLog, TextLog.Msg> list =
        [
            "Log" |> Binding.oneWay (fun m -> m.Log)
        ]