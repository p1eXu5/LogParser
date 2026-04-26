namespace LogParser.Core.Types

open System
open FParsec

type TechLog =
        {
            Source: TechJsonField option
            Fields: TechJsonField list
        }
        with
            override this.ToString() =
                this.Fields
                |> List.map (sprintf "%O")
                |> (fun l -> String.Join("\n", l))


type Log =
    | TextLog of string
    | TechLog of TechLog
    with
        override this.ToString() =
            match this with
            | TextLog s -> s
            | TechLog tl -> tl.ToString()


type LogPosition =
    {
        Start: Position
        End: Position
        Log: Log
    }

// ----------------------- modules

module TechLog =

    let tryFindField fieldName techLog =
        techLog.Fields
        |> List.tryFind (fun f ->
            f |> TechField.key |> fun k -> k.Equals(fieldName, StringComparison.OrdinalIgnoreCase)
        )


module Log =

    let fromTechJson (techJson: TechJson) =
        {
            Source = None
            Fields = techJson
        }
        |> Log.TechLog


    let tryFind fieldType log =
        match log with
        | Log.TextLog _ -> None
        | Log.TechLog techLog ->
            techLog.Fields
            |> List.tryPick (fun field -> 
                match fieldType, field with
                | TechFieldType.Timespan, TechJsonField.Timespan (Timespan.Value v)
                | TechFieldType.Message, TechJsonField.Message v 
                | TechFieldType.Method, TechJsonField.Method v
                | TechFieldType.Path, TechJsonField.Path v
                | TechFieldType.Host, TechJsonField.Host v
                | TechFieldType.SourceContext, TechJsonField.SourceContext v
                | TechFieldType.RequestId, TechJsonField.RequestId v
                | TechFieldType.RequestPath, TechJsonField.RequestPath v
                | TechFieldType.SpanId, TechJsonField.SpanId v
                | TechFieldType.TraceId, TechJsonField.TraceId v
                | TechFieldType.EventId, TechJsonField.EventId v
                | TechFieldType.ParentId, TechJsonField.ParentId v
                | TechFieldType.ConnectionId, TechJsonField.ConnectionId v
                | TechFieldType.HierarchicalTraceId, TechJsonField.HierarchicalTraceId v 
                    -> 
                        Some v

                | TechFieldType.StatusCode, TechJsonField.StatusCode v -> Some (v.ToString())
                | TechFieldType.Level, TechJsonField.Level v -> Some (v.ToString())
                | TechFieldType.Port, TechJsonField.Port v -> Some (v.ToString())
                | _ -> None
            )

    let hierarchicalTraceId = tryFind TechFieldType.HierarchicalTraceId
