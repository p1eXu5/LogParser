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
    let emptyJson key = TechJsonField.Json (key, [])


type JsonLogBuilder () =
    member t.Yield(_) =
        {
            Source = None
            Fields = []
        }

    member _.Run(log) = log |> Log.fieldList

    [<CustomOperation("timestamp")>]
    member _.Timestamp(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> (Timespan.Value >> TechJsonField.Timespan))] }

    /// <summary>
    /// "message" : "some text"
    /// </summary>
    [<CustomOperation("message")>]
    member _.Message(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechJsonField.Message)] }

    /// <summary>
    /// Buddied message field:
    /// <code>
    /// "message" : "some text { &lt;inner_json&gt; }"
    /// </code>
    /// </summary>
    [<CustomOperation("message")>]
    member _.Message(log: TechLog, header: string, json: TechJson) = { log with Fields = log.Fields @ [((header, json) |> TechJsonField.MessageBoddied)] }

    /// <summary>
    /// Buddied message field with postfix:
    /// <code>
    /// "message" : "some text { &lt;inner_json&gt; } postfix"
    /// </code>
    /// </summary>
    [<CustomOperation("message")>]
    member _.Message(log: TechLog, header: string, body: TechJson, postfix: string) =
        { log with Fields = log.Fields @ [((header, body, postfix) |> TechJsonField.MessageBoddiedWithPostfix)] }

    /// TechField.ArrayJson
    [<CustomOperation("message")>]
    member _.Message(log: TechLog, value: TechJson list) = { log with Fields = log.Fields @ [(value |> TechJsonField.MessageArrayJson)] }

    [<CustomOperation("level")>]
    member _.Level(log: TechLog, value: string) = { log with Fields = log.Fields @ [(Enum.Parse(typeof<LogLevel>, value) |> unbox |> TechJsonField.Level)] }

    [<CustomOperation("level")>]
    member _.Level(log: TechLog, value: LogLevel) = { log with Fields = log.Fields @ [value |> TechJsonField.Level] }

    [<CustomOperation("host")>]
    member _.Host(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechJsonField.Host)] }

    [<CustomOperation("port")>]
    member _.Port(log: TechLog, value: int) = { log with Fields = log.Fields @ [(value |> TechJsonField.Port)] }

    [<CustomOperation("sourceContext")>]
    member _.SourceContext(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechJsonField.SourceContext)] }

    [<CustomOperation("method")>]
    member _.Method(log: TechLog, value: string) = { log with Fields = log.Fields @ [value |> TechJsonField.Method] }

    [<CustomOperation("path")>]
    member _.Path(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechJsonField.Path)] }

    [<CustomOperation("statusCode")>]
    member _.StatusCode(log: TechLog, value: string) = { log with Fields = log.Fields @ [(Enum.Parse(typeof<HttpStatusCode>, value) |> unbox |> TechJsonField.StatusCode)] }

    [<CustomOperation("statusCode")>]
    member _.StatusCode(log: TechLog, value: HttpStatusCode) = { log with Fields = log.Fields @ [value |> TechJsonField.StatusCode] }

    [<CustomOperation("body")>]
    member _.Body(log: TechLog, json: TechJson) = { log with Fields = log.Fields @ [json |> TechJsonField.Body] }

    [<CustomOperation("requestId")>]
    member _.RequestId(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechJsonField.RequestId)] }

    [<CustomOperation("requestPath")>]
    member _.RequestPath(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechJsonField.RequestPath)] }

    [<CustomOperation("spanId")>]
    member _.SpanId(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechJsonField.SpanId)] }

    [<CustomOperation("traceId")>]
    member _.TraceId(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechJsonField.TraceId)] }

    [<CustomOperation("parentId")>]
    member _.ParentId(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechJsonField.ParentId)] }

    [<CustomOperation("connectionId")>]
    member _.ConnectionId(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechJsonField.ConnectionId)] }

    [<CustomOperation("hierarchicalTraceId")>]
    member _.HierarchicalTraceId(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechJsonField.HierarchicalTraceId)] }

    [<CustomOperation("eventId")>]
    member _.EventId(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechJsonField.EventId)] }

    /// TechField.String
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, value: string) = { log with Fields = log.Fields @ [((key, value) |> TechJsonField.String)] }

    /// TechField.Int
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, value: int) = { log with Fields = log.Fields @ [((key, value) |> TechJsonField.Int)] }

    /// TechField.Bool
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, value: bool) = { log with Fields = log.Fields @ [((key, value) |> TechJsonField.Bool)] }

    /// TechField.Json
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, json: TechJson) = { log with Fields = log.Fields @ [((key, json) |> TechJsonField.Json)] }

    /// TechField.Array
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, value: string list) = { log with Fields = log.Fields @ [((key, value) |> TechJsonField.Array)] }

    /// TechField.ArrayInt
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, value: int list) = { log with Fields = log.Fields @ [((key, value) |> TechJsonField.ArrayInt)] }

    /// TechField.JsonAnnotated
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, typeName: string, json: TechJson) = 
        { log with Fields = log.Fields @ [({Key = key; Annotation = typeName; Body = json} |> TechJsonField.JsonAnnotated)] }

    /// TechField.ArrayJson
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, value: TechJson list) = { log with Fields = log.Fields @ [((key, value) |> TechJsonField.ArrayJson)] }

    /// TechField.ArrayJsonAnnotated
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, value: JsonAnnotated list) = { log with Fields = log.Fields @ [((key, value) |> TechJsonField.ArrayJsonAnnotated)] }

    /// TechField.Null
    [<CustomOperation("Null")>]
    member _.NullField(log: TechLog, key: string) = 
        { log with Fields = log.Fields @ [(key |> TechJsonField.Null)] }


let jsonLog = JsonLogBuilder()