namespace rec LogParser.Core.Types

open Microsoft.Extensions.Logging
open System.Net
open System
open Microsoft.FSharp.Reflection

type Log =
    | TextLog of string
    | TechnoLog of TechnoLog
    with
        override this.ToString() =
            match this with
            | TextLog s -> s
            | TechnoLog tl -> tl.ToString()

type TechnoLog =
        {
            Source: string option
            Fields: TechnoField list
        }
        with
            override this.ToString() =
                this.Fields
                |> List.map (sprintf "%O")
                |> (fun l -> String.Join("\n", l))

/// Used within message field as `parameter:`
///
/// \"key\": <TypeName> {<Body>}
type TypeJson =
        {
            Key: string
            TypeName: string
            Body: TechnoField list
        }
        with
            override this.ToString() =
                let fields = this.Body |> TechnoField.toString
                $"{this.Key}: {this.TypeName} {fields}"


type Timespan =
    | Value of string
    | Null


type TechnoField =
        | Timespan of Timespan
        | Message of string
        | MessageBoddied of header: string * body: TechnoField list
        /// { "message": "Some text, parameters: [(\"request\": TypeName { TypeProperty: \"value\", ... })]. " }
        | MessageParameterized of header: string * parameters: TypeJson list
        | Level of LogLevel
        | Method of string
        | StatusCode of HttpStatusCode
        | Path of string
        | Host of string
        | Port of int
        | Body of TechnoField list
        | SourceContext of string
        | RequestId of string
        | RequestPath of string
        | SpanId of string
        | TraceId of string
        | EventId of string
        | ParentId of string
        | ConnectionId of string
        | HierarchicalTraceId of string
        | String of key: string * value: string
        | Int of key: string * value: int
        | Bool of key: string * value: bool
        | Array of key: string * value: string list
        | ArrayInt of key: string * value: int list
        | Null of key: string
        | Json of key: string * value: TechnoField list
        | TypeJson of TypeJson
        with
            override this.ToString() =
                match this with
                | Timespan (Timespan.Value v) -> $"\"timespan\": \"{v}\""
                | Timespan (Timespan.Null) -> $"\"timespan\": null"
                | Message v -> $"\"message\": \"{v}\""
                | MessageBoddied (k, v) -> $"\"message\": \"{k}\n, {v |> TechnoField.toString}\""
                | MessageParameterized (header, v) -> 
                    let content = v |> List.map (sprintf "%O") |> (fun l -> String.Join(",\n", l))
                    $"\"message\": \"{header} [{content}]\""
                | Level v -> $"\"level\": \"{v}\""
                | Method v -> $"\"method\": \"{v}\""
                | StatusCode v -> $"\"statusCode\": \"{v}\""
                | Path v -> $"\"path\": \"{v}\""
                | Host v -> $"\"host\": \"{v}\""
                | Port v -> $"\"port\": {v}"
                | Body v -> $"\"body\": \"{v |> TechnoField.toString}\""
                | SourceContext v -> $"\"sourceContext\": \"{v}\"" 
                | RequestId v -> $"\"requestId\": \"{v}\""
                | RequestPath v -> $"\"requestPath\": \"{v}\""
                | SpanId v -> $"\"spanId: \"{v}\""
                | TraceId v -> $"\"traceId\": \"{v}\""
                | ParentId v -> $"\"parentId\": \"{v}\""
                | ConnectionId v -> $"\"connectionId\": \"{v}\""
                | HierarchicalTraceId v -> $"\"hierarchicalTraceId\": \"{v}\""
                | EventId v -> $"\"eventId: \"{v}\""
                | String (k, v) -> $"\"{k}\": \"{v}\""
                | Int (k, v) -> $"\"{k}\": {v}"
                | Bool (k, v) -> $"\"{k}\": {v.ToString().ToLowerInvariant()}"
                | ArrayInt (k, v) ->
                    let values = String.Join(",\n    ",v)
                    $"\"{k}\": [\n    {values}\n]"
                | Array (k, v) -> 
                    let values = String.Join(",\n    ",v |> List.map (fun s -> $"\"{s}\""))
                    $"\"{k}\": [\n    {values}\n]"
                | Null k -> $"\"{k}\": null"
                | Json (k, v) -> $"\"{k}\": {v |> TechnoField.toString}"
                | TypeJson (tj) -> $"%O{tj}"

module TechnoField =

    open System.Text


    let toString (fields: TechnoField list) =
        let fold folder fields state =
            fields
            |> List.fold folder state


        let rec folder (state: {| TabLevel: int; Result: StringBuilder; Comma: bool |}) field =
            if state.Comma 
            then 
                state.Result.Append(",\n") |> ignore

            let tab = String.replicate (state.TabLevel * 4) " "

            match field with
            | Json (key, fields) ->
                state.Result.Append(tab).Append($"\"{key}\": {{\n") |> ignore
                fold folder fields {| state with TabLevel = state.TabLevel + 1; Comma = false |} |> ignore
                state.Result.Append("\n").Append(tab).Append("}") |> ignore

            | TypeJson tj ->
                state.Result.Append(tab).Append($"\"{tj.Key}\": {tj.TypeName} {{\n") |> ignore
                fold folder tj.Body {| state with TabLevel = state.TabLevel + 1; Comma = false |} |> ignore
                state.Result.Append("\n").Append(tab).Append("}") |> ignore

            | _ ->
                state.Result.Append(tab).Append(field.ToString()) |> ignore

            {|state with Comma = true|}

        let result = StringBuilder()
        result.Append("{\n") |> ignore
        let state = fold folder fields {| Result = result; TabLevel = 1; Comma = false |}
        state.Result.Append("\n}") |> ignore
        state.Result.ToString()


    let value field =
        match field with
        | Timespan (Timespan.Value v)
        | Message v
        | Method v
        | Path v
        | Host v
        | SourceContext v 
        | RequestId v 
        | RequestPath v
        | SpanId v
        | TraceId v
        | ParentId v
        | ConnectionId v
        | HierarchicalTraceId v
        | EventId v
        | String (_, v) -> v

        | StatusCode v -> 
            let code : int = box v |> unbox
            $"{v} ({code})"
        | Level v -> v.ToString()

        | Port v
        | Int (_, v) -> v.ToString()
        
        | Bool (_, v) -> v.ToString()
        | ArrayInt (_, v) -> 
            let values = String.Join(",\n    ",v)
            $"[\n    {values}\n]"
        | Array (_, v) -> 
            let values = String.Join(",\n    ",v |> List.map (fun s -> $"\"{s}\""))
            $"[\n    {values}\n]"

        | Timespan (Timespan.Null)
        | Null _ -> "null"

        | MessageBoddied (v, fl) -> $"{v}\n{fl |> toString}"
        | MessageParameterized (v, tj) -> $"{v}\n{tj.ToString()}"
        | Body v
        | Json (_, v) -> v |> toString
        | TypeJson v -> v.ToString()


    let key field =
        let GetUnionCaseName (x:'a) = 
            match FSharpValue.GetUnionFields(x, typeof<'a>) with
            | case, _ -> case.Name

        match field with
        | Timespan _
        | Message _
        | Method _
        | Path _
        | Host _
        | SourceContext _
        | RequestId _
        | RequestPath _
        | SpanId _
        | TraceId _
        | ParentId _
        | ConnectionId _
        | HierarchicalTraceId _
        | StatusCode _
        | Level _
        | Port _
        | Body _
        | EventId _ -> GetUnionCaseName field

        | String (k, _)
        | Int (k, _)
        | Bool (k, _)
        | ArrayInt (k, _)
        | Array (k, _)
        | Null k
        | Json (k, _) -> k

        | MessageBoddied _
        | MessageParameterized _ -> "Message"

        | TypeJson v -> v.Key

