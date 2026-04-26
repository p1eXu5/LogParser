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
    let emptyJson key = TechJsonLogField.Json (key, [])


type JsonLogBuilder () =
    member t.Yield(_) =
        {
            Source = None
            Fields = []
        }

    member _.Run(log) = log |> Log.fieldList

    [<CustomOperation("timestamp")>]
    member _.Timestamp(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> (Timespan.Value >> TechJsonLogField.Timespan))] }

    /// <summary>
    /// "message" : "some text"
    /// </summary>
    [<CustomOperation("message")>]
    member _.Message(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechJsonLogField.Message)] }

    /// <summary>
    /// Buddied message field:
    /// <code>
    /// "message" : "some text { &lt;inner_json&gt; }"
    /// </code>
    /// </summary>
    [<CustomOperation("message")>]
    member _.Message(log: TechLog, header: string, json: TechJsonLogContent) = { log with Fields = log.Fields @ [((header, json) |> TechJsonLogField.MessageBoddied)] }

    /// <summary>
    /// Buddied message field with postfix:
    /// <code>
    /// "message" : "some text { &lt;inner_json&gt; } postfix"
    /// </code>
    /// </summary>
    [<CustomOperation("message")>]
    member _.Message(log: TechLog, header: string, body: TechJsonLogContent, postfix: string) =
        { log with Fields = log.Fields @ [((header, body, postfix) |> TechJsonLogField.MessageBoddiedWithPostfix)] }

    /// TechField.ArrayJson
    [<CustomOperation("message")>]
    member _.Message(log: TechLog, value: TechJsonLogContent list) = { log with Fields = log.Fields @ [(value |> TechJsonLogField.MessageArrayJson)] }

    [<CustomOperation("level")>]
    member _.Level(log: TechLog, value: string) = { log with Fields = log.Fields @ [(Enum.Parse(typeof<LogLevel>, value) |> unbox |> TechJsonLogField.Level)] }

    [<CustomOperation("level")>]
    member _.Level(log: TechLog, value: LogLevel) = { log with Fields = log.Fields @ [value |> TechJsonLogField.Level] }

    [<CustomOperation("host")>]
    member _.Host(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechJsonLogField.Host)] }

    [<CustomOperation("port")>]
    member _.Port(log: TechLog, value: int) = { log with Fields = log.Fields @ [(value |> TechJsonLogField.Port)] }

    [<CustomOperation("sourceContext")>]
    member _.SourceContext(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechJsonLogField.SourceContext)] }

    [<CustomOperation("method")>]
    member _.Method(log: TechLog, value: string) = { log with Fields = log.Fields @ [value |> TechJsonLogField.Method] }

    [<CustomOperation("path")>]
    member _.Path(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechJsonLogField.Path)] }

    [<CustomOperation("statusCode")>]
    member _.StatusCode(log: TechLog, value: string) = { log with Fields = log.Fields @ [(Enum.Parse(typeof<HttpStatusCode>, value) |> unbox |> TechJsonLogField.StatusCode)] }

    [<CustomOperation("statusCode")>]
    member _.StatusCode(log: TechLog, value: HttpStatusCode) = { log with Fields = log.Fields @ [value |> TechJsonLogField.StatusCode] }

    [<CustomOperation("body")>]
    member _.Body(log: TechLog, json: TechJsonLogContent) = { log with Fields = log.Fields @ [json |> TechJsonLogField.Body] }

    [<CustomOperation("requestId")>]
    member _.RequestId(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechJsonLogField.RequestId)] }

    [<CustomOperation("requestPath")>]
    member _.RequestPath(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechJsonLogField.RequestPath)] }

    [<CustomOperation("spanId")>]
    member _.SpanId(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechJsonLogField.SpanId)] }

    [<CustomOperation("traceId")>]
    member _.TraceId(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechJsonLogField.TraceId)] }

    [<CustomOperation("parentId")>]
    member _.ParentId(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechJsonLogField.ParentId)] }

    [<CustomOperation("connectionId")>]
    member _.ConnectionId(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechJsonLogField.ConnectionId)] }

    [<CustomOperation("hierarchicalTraceId")>]
    member _.HierarchicalTraceId(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechJsonLogField.HierarchicalTraceId)] }

    [<CustomOperation("eventId")>]
    member _.EventId(log: TechLog, value: string) = { log with Fields = log.Fields @ [(value |> TechJsonLogField.EventId)] }

    /// TechField.String
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, value: string) = { log with Fields = log.Fields @ [((key, value) |> TechJsonLogField.String)] }

    /// TechField.Int
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, value: int) = { log with Fields = log.Fields @ [((key, value) |> TechJsonLogField.Int)] }

    /// TechField.Bool
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, value: bool) = { log with Fields = log.Fields @ [((key, value) |> TechJsonLogField.Bool)] }

    /// TechField.Json
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, json: TechJsonLogContent) = { log with Fields = log.Fields @ [((key, json) |> TechJsonLogField.Json)] }

    /// TechField.Array
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, value: string list) = { log with Fields = log.Fields @ [((key, value) |> TechJsonLogField.Array)] }

    /// TechField.ArrayInt
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, value: int list) = { log with Fields = log.Fields @ [((key, value) |> TechJsonLogField.ArrayInt)] }

    /// TechField.JsonAnnotated
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, typeName: string, json: TechJsonLogContent) = 
        { log with Fields = log.Fields @ [({Key = key; Annotation = typeName; Body = json} |> TechJsonLogField.JsonAnnotated)] }

    /// TechField.ArrayJson
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, value: TechJsonLogContent list) = { log with Fields = log.Fields @ [((key, value) |> TechJsonLogField.ArrayJson)] }

    /// TechField.ArrayJsonAnnotated
    [<CustomOperation("Field")>]
    member _.Field(log: TechLog, key: string, value: JsonAnnotated list) = { log with Fields = log.Fields @ [((key, value) |> TechJsonLogField.ArrayJsonAnnotated)] }

    /// TechField.Null
    [<CustomOperation("Null")>]
    member _.NullField(log: TechLog, key: string) = 
        { log with Fields = log.Fields @ [(key |> TechJsonLogField.Null)] }


let jsonLog = JsonLogBuilder()