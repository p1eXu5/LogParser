namespace rec LogParser.Core.Types

open System
open System.Net
open Microsoft.Extensions.Logging
open Microsoft.FSharp.Reflection

type TechField =
    | Timespan of Timespan

    // TODO: wrap in separate DU
    | Message of string
    | MessageBoddied of header: string * body: TechJson
    | MessageBoddiedWithPostfix of header: string * body: TechJson * postfix: string

    | Level of LogLevel
    | Method of string
    | StatusCode of HttpStatusCode
    | Path of string
    | Host of string
    | Port of int
    | Body of TechJson
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
    | StringAnnonimous of value: string
    | Int of key: string * value: int
    | IntAnnonimous of value: int
    | Bool of key: string * value: bool
    | Array of key: string * value: string list
    | ArrayInt of key: string * value: int list
    | ArrayJson of key: string * value: TechJson list
    | ArrayJsonAnnonimous of value: TechJson list
    | Null of key: string
    | NullAnnonimous
    | Json of key: string * value: TechJson
    | JsonAnnotated of JsonAnnotated
    | ArrayJsonAnnotated of key: string * JsonAnnotated list
    with
        override this.ToString() =
            match this with
            | Timespan (Timespan.Value v) -> $"\"timespan\": \"{v}\""
            | Timespan (Timespan.Null) -> $"\"timespan\": null"

            | Message v -> $"\"message\": \"{v}\""
            | MessageBoddied (k, v) -> $"\"message\": \"{k},\n {v |> TechField.toString 1}\""
            | MessageBoddiedWithPostfix (k, v, p) -> $"\"message\": \"{k},\n {v |> TechField.toString 1}{p}\""

            | Level v -> $"\"level\": \"{v}\""
            | Method v -> $"\"method\": \"{v}\""
            | StatusCode v -> $"\"statusCode\": \"{v}\""
            | Path v -> $"\"path\": \"{v}\""
            | Host v -> $"\"host\": \"{v}\""
            | Port v -> $"\"port\": {v}"
            | Body v -> $"\"body\": \"{v |> TechField.toString 1}\""
            | SourceContext v -> $"\"sourceContext\": \"{v}\"" 
            | RequestId v -> $"\"requestId\": \"{v}\""
            | RequestPath v -> $"\"requestPath\": \"{v}\""
            | SpanId v -> $"\"spanId: \"{v}\""
            | TraceId v -> $"\"traceId\": \"{v}\""
            | ParentId v -> $"\"parentId\": \"{v}\""
            | ConnectionId v -> $"\"connectionId\": \"{v}\""
            | HierarchicalTraceId v -> $"\"hierarchicalTraceId\": \"{v}\""
            | EventId v -> $"\"eventId\": \"{v}\""
            | String (k, v) -> $"\"{k}\": \"{v}\""
            | StringAnnonimous (v) -> $"\"{v}\""
            | Int (k, v) -> $"\"{k}\": {v}"
            | IntAnnonimous (v) -> $"{v}"
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
                        |> List.map (TechField.toString 1)
                    String.Join(",\n    ",xs |> List.map (fun s -> $"{{ {s} }}"))

                $"\"{k}\": [\n    {values}\n]"
            | ArrayJsonAnnonimous v ->
                let values = 
                    let xs =
                        v
                        |> List.map (TechField.toString 1)
                    String.Join(",\n    ",xs |> List.map (fun s -> $"{{ {s} }}"))

                $"[\n    {values}\n]"

            | ArrayJsonAnnotated (key, fields) ->
                let values =
                    fields
                    |> List.map (fun f ->
                        $"{f.Annotation} {f.Body |> TechField.toString 1}"
                    )
                
                $"\"{key}\": [\n {values}\n]"

            | Null k -> $"\"{k}\": null"
            | NullAnnonimous -> "null"
            | Json (k, v) -> $"\"{k}\": {v |> TechField.toString 1}"
            | JsonAnnotated (tj) -> $"%O{tj}"
and
    TechJson = TechField list
and
    /// Used within message field as `parameter:`
    ///
    /// \"key\": <TypeName> {<Body>}
    JsonAnnotated =
        {
            Key: string
            Annotation: string
            Body: TechField list
        }
        with
            override this.ToString() =
                let fields = this.Body |> TechField.toString 1
                $"\"{this.Key}\": {this.Annotation} {fields}"


// ----------------------------- modules

module TechField =

    open System.Text

    let toString (initTabLevel: int) (fields: TechField list) =
        let fold folder fields state =
            fields
            |> List.fold folder state


        let rec folder (state: {| TabLevel: int; Result: StringBuilder; Comma: bool |}) field =
            if state.Comma 
            then 
                state.Result.Append(",\n") |> ignore

            let tab = String.replicate (state.TabLevel * 4) " "

            match field with
            | Json (key, fields) when fields |> List.isEmpty ->
                state.Result.Append(tab).Append($"\"{key}\": {{ }}") |> ignore

            | Json (key, fields) ->
                state.Result.Append(tab).Append($"\"{key}\": {{\n") |> ignore
                fold folder fields {| state with TabLevel = state.TabLevel + 1; Comma = false |} |> ignore
                state.Result.Append("\n").Append(tab).Append("}") |> ignore

            | ArrayInt (key, fields) ->
                state.Result.Append(tab).Append($"\"{key}\": [\n") |> ignore

                let innerTab = String.replicate ((state.TabLevel + 1) * 4) " "

                fields
                |> List.take (fields.Length - 1)
                |> List.iter (fun f ->
                    state.Result.Append(innerTab).Append($"{f},\n") |> ignore
                )
                
                state.Result.Append(innerTab).Append($"{List.last fields}\n").Append(tab).Append("]") |> ignore

            | Array (key, fields) when fields.Length > 0 ->
                state.Result.Append(tab).Append($"\"{key}\": [\n") |> ignore

                let innerTab = String.replicate ((state.TabLevel + 1) * 4) " "

                fields
                |> List.take (fields.Length - 1)
                |> List.iter (fun f ->
                    state.Result.Append(innerTab).Append($"\"{f}\",\n") |> ignore
                )
                
                state.Result.Append(innerTab).Append($"\"{List.last fields}\"\n").Append(tab).Append("]") |> ignore

            | Array (key, fields) when fields.Length = 0 ->
                state.Result.Append(tab).Append($"\"{key}\": []") |> ignore

            | ArrayJson (key, fields) ->
                state.Result.Append(tab).Append($"\"{key}\": [\n") |> ignore

                fields
                |> List.iter (fun f ->
                    match f |> List.tryHead with
                    | Some (ArrayJsonAnnonimous _) ->
                        fold folder f {| state with TabLevel = state.TabLevel + 1; Comma = false |} |> ignore
                        state.Result.Append("]").Append("\n") |> ignore
                    | Some (StringAnnonimous _)
                    | Some (IntAnnonimous _)
                    | Some (NullAnnonimous) ->
                        fold folder f {| state with TabLevel = state.TabLevel + 1; Comma = false |} |> ignore
                        state.Result.Append(",\n") |> ignore
                    | Some _ ->
                        state.Result.Append(tab).Append("    {\n") |> ignore
                        fold folder f {| state with TabLevel = state.TabLevel + 2; Comma = false |} |> ignore
                        state.Result.Append("\n").Append(tab).Append("    },\n") |> ignore
                    | None ->
                        state.Result.Append(tab).Append("    { },\n") |> ignore
                )
                
                do
                    state.Result.Remove(state.Result.Length - 2, 1).Append(tab).Append("]") |> ignore

            | ArrayJsonAnnonimous fields ->
                state.Result.Append(tab).Append($"[\n") |> ignore

                fields
                |> List.iter (fun f ->
                    state.Result.Append(tab).Append("    {\n") |> ignore
                    fold folder f {| state with TabLevel = state.TabLevel + 2; Comma = false |} |> ignore
                    state.Result.Append("\n").Append(tab).Append("    },\n") |> ignore
                )
                
                state.Result.Remove(state.Result.Length - 2, 1).Append(tab).Append("]") |> ignore

            | ArrayJsonAnnotated (key, fields) ->
                state.Result.Append(tab).Append($"\"{key}\": [\n") |> ignore

                fields
                |> List.iter (fun f ->
                    state.Result.Append(tab).Append(f.Annotation).Append(" ") |> ignore
                    fold folder f.Body {| state with TabLevel = state.TabLevel + 1; Comma = false |} |> ignore
                )
                
                state.Result.Append("\n").Append(tab).Append("]") |> ignore

            | JsonAnnotated tj ->
                state.Result.Append(tab).Append($"\"{tj.Key}\": {tj.Annotation} {{\n") |> ignore
                fold folder tj.Body {| state with TabLevel = state.TabLevel + 1; Comma = false |} |> ignore
                state.Result.Append("\n").Append(tab).Append("}") |> ignore

            | _ ->
                state.Result.Append(tab).Append(field.ToString()) |> ignore

            {|state with Comma = true|}

        let tab = String.replicate ((initTabLevel - 1) * 4) " "

        let result = StringBuilder()
        result.Append(tab).Append("{\n") |> ignore

        let state = fold folder fields {| Result = result; TabLevel = initTabLevel; Comma = false |}

        state.Result.Append("\n").Append(tab).Append("}") |> ignore
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
        | String (_, v)
        | StringAnnonimous v -> v

        | StatusCode v -> 
            let code : int = box v |> unbox
            $"{v} ({code})"
        | Level v -> v.ToString()

        | Port v
        | Int (_, v)
        | IntAnnonimous v -> v.ToString()
        
        | Bool (_, v) -> v.ToString()
        | ArrayInt (_, v) -> 
            let values = String.Join(",\n    ",v)
            $"[\n    {values}\n]"
        | Array (_, v) -> 
            let values = String.Join(",\n    ",v |> List.map (fun s -> $"\"{s}\""))
            $"[\n    {values}\n]"

        | ArrayJsonAnnonimous v
        | ArrayJson (_, v) -> 
            let values = 
                let xs =
                    v
                    |> List.map (TechField.toString 2)
                String.Join(",\n    ",xs |> List.map (fun s -> $"{s}"))

            $"[\n{values}\n]"

        | ArrayJsonAnnotated (_, v) -> 
            let values = 
                let xs =
                    v
                    |> List.map (fun taj -> sprintf "%s %s" taj.Annotation (TechField.toString 2 taj.Body))
                String.Join(",\n    ",xs |> List.map (fun s -> $"{s}"))

            $"[\n{values}\n]"

        | Timespan (Timespan.Null)
        | Null _ -> "null"
        | NullAnnonimous -> "null"

        | MessageBoddied (v, fl) -> $"{v}\n{fl |> toString 1}"
        | MessageBoddiedWithPostfix (v, fl, p) -> $"{v}\n{fl |> toString 1}{p}"

        | Body v
        | Json (_, v) -> v |> toString 1
        | JsonAnnotated v -> v.ToString()


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
        | ArrayJsonAnnotated (k, _)
        | Null k
        | Json (k, _) -> capitalize k

        | Message _
        | MessageBoddied _
        | MessageBoddiedWithPostfix _ -> "Message"

        | JsonAnnotated v -> capitalize v.Key
        | ArrayJsonAnnonimous _ -> failwith "ArrayJsonAnnonimous does not contain key"
        | StringAnnonimous _ -> failwith "StringAnnonimous does not contain key"
        | IntAnnonimous _ -> failwith "IntAnnonimous does not contain key"
        | NullAnnonimous -> failwith "NullAnnonimous does not contain key"

    /// For important fields returns predefined order ("0", "1", ...),
    /// for other returns key value.
    let orderOrKey = function
        | Timespan _ -> "0"
        | Level _ -> "1"
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
        | ArrayJsonAnnotated (k, _)
        | Null k
        | Json (k, _) -> capitalize k

        | JsonAnnotated v -> capitalize v.Key
        | ArrayJsonAnnonimous _ -> (Int32.MaxValue - 3).ToString()
        | StringAnnonimous _ -> (Int32.MaxValue - 2).ToString()
        | IntAnnonimous _ -> (Int32.MaxValue - 1).ToString()
        | NullAnnonimous -> Int32.MaxValue.ToString()

    let arrayTypeJson key jsonList =
        (key, jsonList) |> TechField.ArrayJsonAnnotated
