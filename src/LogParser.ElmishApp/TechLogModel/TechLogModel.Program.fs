module LogParser.ElmishApp.TechLogModel.Program

open Elmish

let update msg model =
    match msg with
    | _ -> model, Cmd.none

