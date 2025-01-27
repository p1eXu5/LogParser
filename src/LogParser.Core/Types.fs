namespace LogParser.Core.Types

open System

type Timespan =
    | Value of string
    | Null

/// Used in Log.tryFind
[<RequireQualifiedAccess>]
type TechFieldType =
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