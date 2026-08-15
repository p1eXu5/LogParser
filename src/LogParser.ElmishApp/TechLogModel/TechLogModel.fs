namespace LogParser.ElmishApp.Models

open LogParser.Types
open LogParser.App

/// Respond to change log in log repository
///
/// Provides on time set bindings
///
/// Sorting, ordering and logContext separation are moved to the Wpf-scope
type TechLogModel =
    | TextLogModel of UIProps: TechLogUIProps * Model: TechLogId
    | JsonLogModel of UIProps: TechLogUIProps * Model: TechLogId
and
    TechLogUIProps =
        {
            IsExpanded: bool
        }

module TechLogModel =

    type Msg = Msg

    let logId = function
        | TechLogModel.TextLogModel (_, l) -> l.Ind
        | TechLogModel.JsonLogModel (_, l) -> l.Ind 

    (*
    let timestamp = function
        | TechLogModel.TextLogModel _ -> None
        | TechLogModel.JsonLogModel (_, log) -> log.Timestamp

    let toString = function
        | TechLogModel.TextLogModel (_, log) -> log.Log
        | TechLogModel.JsonLogModel (_, log) ->
            match log.Log.Source with
            | Some ls ->
                sprintf "%s %s"
                    (ls.ToString())
                    (log.Log.Fields |> LogParser.Types.TechJsonLogField.toString 1)
            | None ->
                sprintf "%s"
                    (log.Log.Fields |> LogParser.Types.TechJsonLogField.toString 1)

    let serviceName = function
        | TechLogModel.JsonLogModel (_, log) -> log.ServiceName |> Some
        | _ -> None
    *)

    