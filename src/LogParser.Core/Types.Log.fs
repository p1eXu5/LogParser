namespace LogParser.Core.Types

open System
open FParsec

/// Used in Log.tryFind
[<Struct>]
[<RequireQualifiedAccess>]
type TechJsonSpecialFieldType =
    | Timespan
    | Message
    | Level
    | Method
    | StatusCode
    | Path
    | Host
    | Port
    | Body
    | SourceContext
    | RequestId
    | RequestPath
    | SpanId
    | TraceId
    | EventId
    | ParentId
    | ConnectionId
    | HierarchicalTraceId

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
                | TechJsonSpecialFieldType.Timespan, TechJsonField.Timespan (Timespan.Value v)
                | TechJsonSpecialFieldType.Message, TechJsonField.Message v 
                | TechJsonSpecialFieldType.Method, TechJsonField.Method v
                | TechJsonSpecialFieldType.Path, TechJsonField.Path v
                | TechJsonSpecialFieldType.Host, TechJsonField.Host v
                | TechJsonSpecialFieldType.SourceContext, TechJsonField.SourceContext v
                | TechJsonSpecialFieldType.RequestId, TechJsonField.RequestId v
                | TechJsonSpecialFieldType.RequestPath, TechJsonField.RequestPath v
                | TechJsonSpecialFieldType.SpanId, TechJsonField.SpanId v
                | TechJsonSpecialFieldType.TraceId, TechJsonField.TraceId v
                | TechJsonSpecialFieldType.EventId, TechJsonField.EventId v
                | TechJsonSpecialFieldType.ParentId, TechJsonField.ParentId v
                | TechJsonSpecialFieldType.ConnectionId, TechJsonField.ConnectionId v
                | TechJsonSpecialFieldType.HierarchicalTraceId, TechJsonField.HierarchicalTraceId v 
                    -> 
                        Some v

                | TechJsonSpecialFieldType.StatusCode, TechJsonField.StatusCode v -> Some (v.ToString())
                | TechJsonSpecialFieldType.Level, TechJsonField.Level v -> Some (v.ToString())
                | TechJsonSpecialFieldType.Port, TechJsonField.Port v -> Some (v.ToString())
                | _ -> None
            )

    let hierarchicalTraceId = tryFind TechJsonSpecialFieldType.HierarchicalTraceId
