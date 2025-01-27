module LogParser.Core.Dsl

open LogParser.Core.Types
open System
open Microsoft.Extensions.Logging
open System.Net


[<RequireQualifiedAccess>]
module Log =

    let fieldList (log: TechLog) = log.Fields


[<RequireQualifiedAccess>]
module JsonLog =
    let emptyJson key = TechField.Json (key, [])


type JsonLogBuilder () =
    member t.Yield(_) =
        {
            Source = None
            Fields = []
        }

    member _.Run(log) = log |> Log.fieldList

    [<CustomOperation("timestamp")>]
    member _.Timestamp(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> (Timespan.Value >> TechField.Timespan))] }

    /// <summary>
    /// "message" : "some text"
    /// </summary>
    [<CustomOperation("message")>]
    member _.Message(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechField.Message)] }

    /// <summary>
    /// Buddied message field:
    /// <code>
    /// "message" : "some text { &lt;inner_json&gt; }"
    /// </code>
    /// </summary>
    [<CustomOperation("message")>]
    member _.Message(log: TechLog, header: string, json: TechJson) = { log with Fields = log.Fields @ [((header, json) |> TechField.MessageBoddied)] }

    /// <summary>
    /// Buddied message field with postfix:
    /// <code>
    /// "message" : "some text { &lt;inner_json&gt; } postfix"
    /// </code>
    /// </summary>
    [<CustomOperation("message")>]
    member _.Message(log: TechLog, header: string, body: TechJson, postfix: string) =
        { log with Fields = log.Fields @ [((header, body, postfix) |> TechField.MessageBoddiedWithPostfix)] }

    [<CustomOperation("level")>]
    member _.Level(log: TechLog, value: string) = { log with Fields = log.Fields @ [(Enum.Parse(typeof<LogLevel>, value) |> unbox |> TechField.Level)] }

    [<CustomOperation("level")>]
    member _.Level(log: TechLog, value: LogLevel) = { log with Fields = log.Fields @ [value |> TechField.Level] }

    [<CustomOperation("host")>]
    member _.Host(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechField.Host)] }

    [<CustomOperation("port")>]
    member _.Port(log: TechLog, value: int) = { log with Fields = log.Fields @ [(value |> TechField.Port)] }

    [<CustomOperation("sourceContext")>]
    member _.SourceContext(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechField.SourceContext)] }

    [<CustomOperation("method")>]
    member _.Method(log: TechLog, value: string) = { log with Fields = log.Fields @ [value |> TechField.Method] }

    [<CustomOperation("path")>]
    member _.Path(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechField.Path)] }

    [<CustomOperation("statusCode")>]
    member _.StatusCode(log: TechLog, value: string) = { log with Fields = log.Fields @ [(Enum.Parse(typeof<HttpStatusCode>, value) |> unbox |> TechField.StatusCode)] }

    [<CustomOperation("statusCode")>]
    member _.StatusCode(log: TechLog, value: HttpStatusCode) = { log with Fields = log.Fields @ [value |> TechField.StatusCode] }

    [<CustomOperation("body")>]
    member _.Body(log: TechLog, json: TechJson) = { log with Fields = log.Fields @ [json |> TechField.Body] }

    [<CustomOperation("requestId")>]
    member _.RequestId(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechField.RequestId)] }

    [<CustomOperation("requestPath")>]
    member _.RequestPath(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechField.RequestPath)] }

    [<CustomOperation("spanId")>]
    member _.SpanId(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechField.SpanId)] }

    [<CustomOperation("traceId")>]
    member _.TraceId(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechField.TraceId)] }

    [<CustomOperation("parentId")>]
    member _.ParentId(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechField.ParentId)] }

    [<CustomOperation("connectionId")>]
    member _.ConnectionId(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechField.ConnectionId)] }

    [<CustomOperation("hierarchicalTraceId")>]
    member _.HierarchicalTraceId(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechField.HierarchicalTraceId)] }

    [<CustomOperation("eventId")>]
    member _.EventId(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechField.EventId)] }

    /// TechField.String
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, value: string) = { log with Fields = log.Fields @ [((key, value) |> TechField.String)] }

    /// TechField.Int
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, value: int) = { log with Fields = log.Fields @ [((key, value) |> TechField.Int)] }

    /// TechField.Bool
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, value: bool) = { log with Fields = log.Fields @ [((key, value) |> TechField.Bool)] }

    /// TechField.Json
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, json: TechJson) = { log with Fields = log.Fields @ [((key, json) |> TechField.Json)] }

    /// TechField.Array
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, value: string list) = { log with Fields = log.Fields @ [((key, value) |> TechField.Array)] }

    /// TechField.ArrayInt
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, value: int list) = { log with Fields = log.Fields @ [((key, value) |> TechField.ArrayInt)] }

    /// TechField.JsonAnnotated
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, typeName: string, json: TechJson) = 
        { log with Fields = log.Fields @ [({Key = key; Annotation = typeName; Body = json} |> TechField.JsonAnnotated)] }

    /// TechField.ArrayJson
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, value: TechJson list) = { log with Fields = log.Fields @ [((key, value) |> TechField.ArrayJson)] }

    /// TechField.ArrayJsonAnnotated
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, value: JsonAnnotated list) = { log with Fields = log.Fields @ [((key, value) |> TechField.ArrayJsonAnnotated)] }

    /// TechField.Null
    [<CustomOperation("Null")>]
    member _.NullField(log: TechLog, key: string) = 
        { log with Fields = log.Fields @ [(key |> TechField.Null)] }


let jsonLog = JsonLogBuilder()