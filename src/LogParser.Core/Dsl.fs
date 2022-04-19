module LogParser.Core.Dsl

open LogParser.Core.Types
open System
open Microsoft.Extensions.Logging
open System.Net


[<RequireQualifiedAccess>]
module Log =

    let fieldList (log: TechnoLog) = log.Fields


type TechnoLogBuilder () =
    member t.Yield(_) =
        {
            Source = None
            Fields = []
        }

    [<CustomOperation("timestamp")>]
    member _.Timestamp(log: TechnoLog, value: string) = { log with Fields = log.Fields @ [(value |> (Timespan.Value >> TechnoField.Timespan))] }

    [<CustomOperation("message")>]
    member _.Message(log: TechnoLog, value: string) = { log with Fields = log.Fields @ [(value |> TechnoField.Message)] }

    [<CustomOperation("message")>]
    member _.Message(log: TechnoLog, value: string, body: TechnoLog) = { log with Fields = log.Fields @ [((value, body.Fields) |> TechnoField.MessageBoddied)] }

    [<CustomOperation("message")>]
    member _.Message(log: TechnoLog, value: string, parameters: TypeJson) = { log with Fields = log.Fields @ [((value, [parameters  |> MessageParameter.TypeJson]) |> TechnoField.MessageParameterized)] }

    [<CustomOperation("message")>]
    member _.Message(log: TechnoLog, value: string, parameters: MessageParameter list) = { log with Fields = log.Fields @ [((value, parameters) |> TechnoField.MessageParameterized)] }

    [<CustomOperation("level")>]
    member _.Level(log: TechnoLog, value: string) = { log with Fields = log.Fields @ [(Enum.Parse(typeof<LogLevel>, value) |> unbox |> TechnoField.Level)] }

    [<CustomOperation("level")>]
    member _.Level(log: TechnoLog, value: LogLevel) = { log with Fields = log.Fields @ [value |> TechnoField.Level] }

    [<CustomOperation("host")>]
    member _.Host(log: TechnoLog, value: string) = { log with Fields = log.Fields @ [(value |> TechnoField.Host)] }

    [<CustomOperation("port")>]
    member _.Port(log: TechnoLog, value: int) = { log with Fields = log.Fields @ [(value |> TechnoField.Port)] }

    [<CustomOperation("sourceContext")>]
    member _.SourceContext(log: TechnoLog, value: string) = { log with Fields = log.Fields @ [(value |> TechnoField.SourceContext)] }

    [<CustomOperation("method")>]
    member _.Method(log: TechnoLog, value: string) = { log with Fields = log.Fields @ [value |> TechnoField.Method] }

    [<CustomOperation("path")>]
    member _.Path(log: TechnoLog, value: string) = { log with Fields = log.Fields @ [(value |> TechnoField.Path)] }

    [<CustomOperation("statusCode")>]
    member _.StatusCode(log: TechnoLog, value: string) = { log with Fields = log.Fields @ [(Enum.Parse(typeof<HttpStatusCode>, value) |> unbox |> TechnoField.StatusCode)] }

    [<CustomOperation("statusCode")>]
    member _.StatusCode(log: TechnoLog, value: HttpStatusCode) = { log with Fields = log.Fields @ [value |> TechnoField.StatusCode] }

    [<CustomOperation("body")>]
    member _.Body(log: TechnoLog, value: TechnoLog) = { log with Fields = log.Fields @ [value.Fields |> TechnoField.Body] }

    [<CustomOperation("requestId")>]
    member _.RequestId(log: TechnoLog, value: string) = { log with Fields = log.Fields @ [(value |> TechnoField.RequestId)] }

    [<CustomOperation("requestPath")>]
    member _.RequestPath(log: TechnoLog, value: string) = { log with Fields = log.Fields @ [(value |> TechnoField.RequestPath)] }

    [<CustomOperation("spanId")>]
    member _.SpanId(log: TechnoLog, value: string) = { log with Fields = log.Fields @ [(value |> TechnoField.SpanId)] }

    [<CustomOperation("traceId")>]
    member _.TraceId(log: TechnoLog, value: string) = { log with Fields = log.Fields @ [(value |> TechnoField.TraceId)] }

    [<CustomOperation("parentId")>]
    member _.ParentId(log: TechnoLog, value: string) = { log with Fields = log.Fields @ [(value |> TechnoField.ParentId)] }

    [<CustomOperation("connectionId")>]
    member _.ConnectionId(log: TechnoLog, value: string) = { log with Fields = log.Fields @ [(value |> TechnoField.ConnectionId)] }

    [<CustomOperation("hierarchicalTraceId")>]
    member _.HierarchicalTraceId(log: TechnoLog, value: string) = { log with Fields = log.Fields @ [(value |> TechnoField.HierarchicalTraceId)] }

    [<CustomOperation("eventId")>]
    member _.EventId(log: TechnoLog, value: string) = { log with Fields = log.Fields @ [(value |> TechnoField.EventId)] }

    [<CustomOperation("Field")>]
    member _.Field(log: TechnoLog, key: string, value: string) = { log with Fields = log.Fields @ [((key, value) |> TechnoField.String)] }
    
    [<CustomOperation("Field")>]
    member _.Field(log: TechnoLog, key: string, value: int) = { log with Fields = log.Fields @ [((key, value) |> TechnoField.Int)] }

    [<CustomOperation("Field")>]
    member _.Field(log: TechnoLog, key: string, value: bool) = { log with Fields = log.Fields @ [((key, value) |> TechnoField.Bool)] }

    [<CustomOperation("Field")>]
    member _.Field(log: TechnoLog, key: string, value: TechnoLog) = { log with Fields = log.Fields @ [((key, value.Fields) |> TechnoField.Json)] }

    [<CustomOperation("Field")>]
    member _.Field(log: TechnoLog, key: string, value: string list) = { log with Fields = log.Fields @ [((key, value) |> TechnoField.Array)] }

    [<CustomOperation("Field")>]
    member _.Field(log: TechnoLog, key: string, value: int list) = { log with Fields = log.Fields @ [((key, value) |> TechnoField.ArrayInt)] }

    [<CustomOperation("Field")>]
    member _.Field(log: TechnoLog, key: string, typeName: string, value: TechnoLog) = 
        { log with Fields = log.Fields @ [({Key = key; TypeName = typeName; Body = value.Fields} |> TechnoField.TypeJson)] }

    [<CustomOperation("Null")>]
    member _.NullField(log: TechnoLog, key: string) = 
        { log with Fields = log.Fields @ [(key |> TechnoField.Null)] }


let jsonLog = TechnoLogBuilder()