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
        let (mainFields, otherFields) =
            log.Fields
            |> List.sortBy TechnoFields.order
            |> List.map TechnoField.Program.init
            |> List.partition (fun f -> 
                match f.TechnoField with
                | TechnoField.Timespan _
                | TechnoField.Level _
                | TechnoField.Message _
                | TechnoField.MessageBoddied _
                | TechnoField.MessageParameterized _ -> true
                | _ -> false
            )

        let valueOf k =
            mainFields |> List.tryFind (fun f -> f.TechnoField |> TechnoFields.order |> (=) k) |> Option.bind (fun f -> f.Text) |> Option.defaultValue "___"

        let timespan = valueOf "0"
        let level = valueOf "1"
        let message = valueOf "2"

        {
            Id = Guid.NewGuid()
            IsExpanded = false
            Header = $"{level}:    {timespan} - {message}"
            Log = log
            Fields = mainFields @ otherFields
        }
