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
                let fields = this.Body |> TechnoFields.toString 1
                $"\"{this.Key}\": {this.TypeName} {fields}"

type MessageParameter =
    | TechnoField of TechnoField
    | TypeJson of TypeJson
    with
        override this.ToString() =
            match this with
            | TechnoField tf -> tf.ToString()
            | TypeJson tj -> tj.ToString()

type Timespan =
    | Value of string
    | Null


[<RequireQualifiedAccess>]
type TechnoFieldType =
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
    // | Custom of key: string


type TechnoField =
        | Timespan of Timespan
        | Message of string
        | MessageBoddied of header: string * body: TechnoField list
        | MessageBoddiedWithPostfix of header: string * body: TechnoField list * postfix: string
        /// { "message": "Some text, parameters: [(\"request\": TypeName { TypeProperty: \"value\", ... })]. " }
        | MessageParameterized of header: string * parameters: MessageParameter list
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
        | ArrayJson of key: string * value: TechnoField list list
        | Null of key: string
        | Json of key: string * value: TechnoField list
        | JsonAnnotated of key: string * header: string * body: TechnoField list
        | TypeJson of TypeJson
        with
            override this.ToString() =
                match this with
                | Timespan (Timespan.Value v) -> $"\"timespan\": \"{v}\""
                | Timespan (Timespan.Null) -> $"\"timespan\": null"
                | Message v -> $"\"message\": \"{v}\""
                | MessageBoddied (k, v) -> $"\"message\": \"{k}\n, {v |> TechnoFields.toString 1}\""
                | MessageBoddiedWithPostfix (k, v, p) -> $"\"message\": \"{k}\n, {v |> TechnoFields.toString 1}{p}\""
                | MessageParameterized (header, v) ->
                    let content = v |> List.map (sprintf "%O") |> (fun l -> String.Join(",\n", l))
                    $"\"message\": \"{header} [{content}]\""
                | Level v -> $"\"level\": \"{v}\""
                | Method v -> $"\"method\": \"{v}\""
                | StatusCode v -> $"\"statusCode\": \"{v}\""
                | Path v -> $"\"path\": \"{v}\""
                | Host v -> $"\"host\": \"{v}\""
                | Port v -> $"\"port\": {v}"
                | Body v -> $"\"body\": \"{v |> TechnoFields.toString 1}\""
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
                | ArrayJson (k, v) ->
                    let values = 
                        let xs =
                            v
                            |> List.map (TechnoFields.toString 1)
                        String.Join(",\n    ",xs |> List.map (fun s -> $"{{ {s} }}"))

                    $"\"{k}\": [\n    {values}\n]"

                | Null k -> $"\"{k}\": null"
                | Json (k, v) -> $"\"{k}\": {v |> TechnoFields.toString 1}"
                | JsonAnnotated (k, h, v) -> $"\"{k}\": \"{h}{v |> TechnoFields.toString 1}\""
                | TypeJson (tj) -> $"%O{tj}"


module TechnoFields =

    open System.Text

    let toString (initTabLevel: int) (fields: TechnoField list) =
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

            | JsonAnnotated (key, header, fields) ->
                state.Result.Append(tab).Append($"\"{key}\": \"{header} {{\n") |> ignore
                fold folder fields {| state with TabLevel = state.TabLevel + 1; Comma = false |} |> ignore
                state.Result.Append("\n").Append(tab).Append("}\"") |> ignore

            | ArrayInt (key, fields) ->
                state.Result.Append(tab).Append($"\"{key}\": [\n") |> ignore

                let innerTab = String.replicate ((state.TabLevel + 1) * 4) " "

                fields
                |> List.take (fields.Length - 1)
                |> List.iter (fun f ->
                    state.Result.Append(innerTab).Append($"{f},\n") |> ignore
                )
                
                state.Result.Append(innerTab).Append($"{List.last fields}\n").Append(tab).Append("]") |> ignore

            | Array (key, fields) ->
                state.Result.Append(tab).Append($"\"{key}\": [\n") |> ignore

                let innerTab = String.replicate ((state.TabLevel + 1) * 4) " "

                fields
                |> List.take (fields.Length - 1)
                |> List.iter (fun f ->
                    state.Result.Append(innerTab).Append($"\"{f}\",\n") |> ignore
                )
                
                state.Result.Append(innerTab).Append($"\"{List.last fields}\"\n").Append(tab).Append("]") |> ignore

            | ArrayJson (key, fields) ->
                state.Result.Append(tab).Append($"\"{key}\": [\n") |> ignore

                fields
                |> List.iter (fun f ->
                    fold folder f {| state with TabLevel = state.TabLevel + 1; Comma = false |} |> ignore
                )
                
                state.Result.Append("\n").Append(tab).Append("]") |> ignore

            | TypeJson tj ->
                state.Result.Append(tab).Append($"\"{tj.Key}\": {tj.TypeName} {{\n") |> ignore
                fold folder tj.Body {| state with TabLevel = state.TabLevel + 1; Comma = false |} |> ignore
                state.Result.Append("\n").Append(tab).Append("}") |> ignore

            | _ ->
                state.Result.Append(tab).Append(field.ToString()) |> ignore

            {|state with Comma = true|}

        let result = StringBuilder()
        result.Append("{\n") |> ignore

        let state = fold folder fields {| Result = result; TabLevel = initTabLevel; Comma = false |}

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

        | ArrayJson (_, v) -> 
            let values = 
                let xs =
                    v
                    |> List.map (TechnoFields.toString 1)
                String.Join(",\n    ",xs |> List.map (fun s -> $"{{ {s} }}"))

            $"[\n    {values}\n]"

        | Timespan (Timespan.Null)
        | Null _ -> "null"

        | MessageBoddied (v, fl) -> $"{v}\n{fl |> toString 1}"
        | MessageBoddiedWithPostfix (v, fl, p) -> $"{v}\n{fl |> toString 1}{p}"
        | MessageParameterized (v, tj) ->
            let content = tj |> List.map (sprintf "%O") |> (fun l -> String.Join(",\n", l))
            $"{v}\n{content}"
        | JsonAnnotated (_, h, v) -> $"{h}\n{v |> toString 1}"
        | Body v
        | Json (_, v) -> v |> toString 1
        | TypeJson v -> v.ToString()


    let capitalize (s: string) =
        Char.ToUpper(s[0]) |> sprintf "%c%s" <| s[1..]


    let key field =
        let GetUnionCaseName (x:'a) = 
            match FSharpValue.GetUnionFields(x, typeof<'a>) with
            | case, _ -> case.Name

        match field with
        | Timespan _
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
        | ArrayJson (k, _)
        | ArrayInt (k, _)
        | Array (k, _)
        | Null k
        | JsonAnnotated (k, _, _)
        | Json (k, _) -> capitalize k

        | Message _
        | MessageBoddied _
        | MessageBoddiedWithPostfix _
        | MessageParameterized _ -> "Message"

        | TypeJson v -> capitalize v.Key




    let order = function
        | Timespan _ -> "0"
        | Level _ -> "1"
        | MessageParameterized _
        | MessageBoddied _
        | MessageBoddiedWithPostfix _
        | Message _ -> "2"
        | Json (k, _) when k.Equals("message", StringComparison.InvariantCultureIgnoreCase) -> "2"
        | Method _ -> nameof(Method)
        | Path _ -> nameof(Path)
        | Host _ -> nameof(Host)
        | SourceContext _ -> nameof(SourceContext)
        | RequestId _ -> nameof(RequestId)
        | RequestPath _ -> nameof(RequestPath)
        | SpanId _ -> "4"
        | TraceId _ -> "5"
        | ParentId _ -> "3"
        | ConnectionId _ -> nameof(ConnectionId)
        | HierarchicalTraceId _ -> "6"
        | StatusCode _ -> nameof(StatusCode)
        | Port _ -> nameof(Port)
        | Body _ -> nameof(Body)
        | EventId _ -> nameof(EventId)

        | String (k, _)
        | Int (k, _)
        | Bool (k, _)
        | ArrayInt (k, _)
        | ArrayJson (k, _)
        | Array (k, _)
        | Null k
        | Json (k, _) -> capitalize k
        | JsonAnnotated (k, _, _) -> capitalize k

        | TypeJson v -> capitalize v.Key


module Log =

    let tryFind fieldType log =
        match log with
        | Log.TextLog _ -> None
        | Log.TechnoLog technoLog ->
            technoLog.Fields
            |> List.tryPick (fun field -> 
                match fieldType, field with
                | TechnoFieldType.Timespan, TechnoField.Timespan (Timespan.Value v)
                | TechnoFieldType.Message, TechnoField.Message v 
                | TechnoFieldType.Method, TechnoField.Method v
                | TechnoFieldType.Path, TechnoField.Path v
                | TechnoFieldType.Host, TechnoField.Host v
                | TechnoFieldType.SourceContext, TechnoField.SourceContext v
                | TechnoFieldType.RequestId, TechnoField.RequestId v
                | TechnoFieldType.RequestPath, TechnoField.RequestPath v
                | TechnoFieldType.SpanId, TechnoField.SpanId v
                | TechnoFieldType.TraceId, TechnoField.TraceId v
                | TechnoFieldType.EventId, TechnoField.EventId v
                | TechnoFieldType.ParentId, TechnoField.ParentId v
                | TechnoFieldType.ConnectionId, TechnoField.ConnectionId v
                | TechnoFieldType.HierarchicalTraceId, TechnoField.HierarchicalTraceId v 
                    -> 
                        Some v

                | TechnoFieldType.StatusCode, TechnoField.StatusCode v -> Some (v.ToString())
                | TechnoFieldType.Level, TechnoField.Level v -> Some (v.ToString())
                | TechnoFieldType.Port, TechnoField.Port v -> Some (v.ToString())
                | _ -> None
            )

    let hierarchicalTraceId = tryFind TechnoFieldType.HierarchicalTraceId