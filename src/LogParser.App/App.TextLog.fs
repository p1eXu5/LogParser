namespace LogParser.App.TextLog

open System

type Model =
    {
        Id: Guid
        Log: string
    }

type Msg = Msg

module Program =

    open Elmish

    let init (log: string) =
        {
            Id = Guid.NewGuid()
            Log = log
        }
        , Cmd.none


    open Elmish.WPF

    let bindings : Binding<Model, Msg> list =
        [
            "Log" |> Binding.oneWay (fun m -> m.Log)
        ]