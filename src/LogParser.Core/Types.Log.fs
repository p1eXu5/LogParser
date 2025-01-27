namespace LogParser.Core.Types

open System

type TechLog =
        {
            Source: TechField option
            Fields: TechField list
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
                | TechFieldType.Timespan, TechField.Timespan (Timespan.Value v)
                | TechFieldType.Message, TechField.Message v 
                | TechFieldType.Method, TechField.Method v
                | TechFieldType.Path, TechField.Path v
                | TechFieldType.Host, TechField.Host v
                | TechFieldType.SourceContext, TechField.SourceContext v
                | TechFieldType.RequestId, TechField.RequestId v
                | TechFieldType.RequestPath, TechField.RequestPath v
                | TechFieldType.SpanId, TechField.SpanId v
                | TechFieldType.TraceId, TechField.TraceId v
                | TechFieldType.EventId, TechField.EventId v
                | TechFieldType.ParentId, TechField.ParentId v
                | TechFieldType.ConnectionId, TechField.ConnectionId v
                | TechFieldType.HierarchicalTraceId, TechField.HierarchicalTraceId v 
                    -> 
                        Some v

                | TechFieldType.StatusCode, TechField.StatusCode v -> Some (v.ToString())
                | TechFieldType.Level, TechField.Level v -> Some (v.ToString())
                | TechFieldType.Port, TechField.Port v -> Some (v.ToString())
                | _ -> None
            )

    let hierarchicalTraceId = tryFind TechFieldType.HierarchicalTraceId
