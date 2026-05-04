namespace LogParser.Types

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

type TechJsonLog =
        {
            Source: TechJsonLogField option
            Fields: TechJsonLogContent
        }
        with
            override this.ToString() =
                this.Fields
                |> List.map (sprintf "%O")
                |> (fun l -> String.Join("\n", l))


type TechLog =
    | TextLog of string
    | JsonLog of TechJsonLog
    with
        override this.ToString() =
            match this with
            | TextLog s -> s
            | JsonLog tl -> tl.ToString()


type LogPosition =
    {
        Start: Position
        End: Position
        Log: TechLog
    }

// ----------------------- modules

module TechJsonLog =

    let tryFindField fieldName techLog =
        techLog.Fields
        |> List.tryFind (fun f ->
            f |> TechJsonLogField.key |> fun k -> k.Equals(fieldName, StringComparison.OrdinalIgnoreCase)
        )


module TechLog =

    let fromTechJson (techJson: TechJsonLogContent) =
        {
            Source = None
            Fields = techJson
        }
        |> TechLog.JsonLog


    let tryFind fieldType log =
        match log with
        | TechLog.TextLog _ -> None
        | TechLog.JsonLog techLog ->
            techLog.Fields
            |> List.tryPick (fun field -> 
                match fieldType, field with
                | TechJsonSpecialFieldType.Timespan, TechJsonLogField.Timespan (Timespan.Value v)
                | TechJsonSpecialFieldType.Message, TechJsonLogField.Message v 
                | TechJsonSpecialFieldType.Method, TechJsonLogField.Method v
                | TechJsonSpecialFieldType.Path, TechJsonLogField.Path v
                | TechJsonSpecialFieldType.Host, TechJsonLogField.Host v
                | TechJsonSpecialFieldType.SourceContext, TechJsonLogField.SourceContext v
                | TechJsonSpecialFieldType.RequestId, TechJsonLogField.RequestId v
                | TechJsonSpecialFieldType.RequestPath, TechJsonLogField.RequestPath v
                | TechJsonSpecialFieldType.SpanId, TechJsonLogField.SpanId v
                | TechJsonSpecialFieldType.TraceId, TechJsonLogField.TraceId v
                | TechJsonSpecialFieldType.EventId, TechJsonLogField.EventId v
                | TechJsonSpecialFieldType.ParentId, TechJsonLogField.ParentId v
                | TechJsonSpecialFieldType.ConnectionId, TechJsonLogField.ConnectionId v
                | TechJsonSpecialFieldType.HierarchicalTraceId, TechJsonLogField.HierarchicalTraceId v 
                    -> 
                        Some v

                | TechJsonSpecialFieldType.StatusCode, TechJsonLogField.StatusCode v -> Some (v.ToString())
                | TechJsonSpecialFieldType.Level, TechJsonLogField.Level v -> Some (v.ToString())
                | TechJsonSpecialFieldType.Port, TechJsonLogField.Port v -> Some (v.ToString())
                | _ -> None
            )

    let hierarchicalTraceId = tryFind TechJsonSpecialFieldType.HierarchicalTraceId
