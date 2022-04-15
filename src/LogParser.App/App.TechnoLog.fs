namespace LogParser.App.TechnoLog

open System

open LogParser.Core.Types
open LogParser.App

type Model =
    {
        Id: Guid
        IsExpanded: bool
        Header: string
        Log: TechnoLog
        Fields: TechnoField.Model list
    }


module Program =

    let init (log: TechnoLog) =
        let fields =
            log.Fields
            |> List.map TechnoField.Program.init

        let timespan = log.Fields |> List.tryPick (function | TechnoField.Timespan (Timespan.Value ts) -> ts |> Some | _ -> None ) |> Option.defaultValue "___"
        let level = log.Fields |> List.tryPick (function | TechnoField.Level l -> l.ToString().ToLowerInvariant() |> Some | _ -> None) |> Option.defaultValue "______"
        let message = log.Fields |> List.tryPick (function | TechnoField.Message m | TechnoField.MessageBoddied (m, _) -> m |> Some | _ -> None) |> Option.defaultValue "______"

        {
            Id = Guid.NewGuid()
            IsExpanded = false
            Header = $"{level}:    {timespan} - {message}"
            Log = log
            Fields = fields
        }
