module LogParser.Core.Parser

open System
open FParsec
open LogParser.Core.Types
open Microsoft.Extensions.Logging
open System.Net


let [<Literal>] NO_QUOTES = ""
let [<Literal>] QUOTES = "\""
let [<Literal>] ESCAPED_QUOTES = "\\\""


///<summary>
/// Skips over any sequence of *zero* or more whitespaces (space (' '), tab ('\t')
/// or newline ("\n", "\r\n" or "\r")).
///</summary> 
let ws = unicodeSpaces // skipManySatisfy (fun ch -> Char.IsWhiteSpace(ch))
let ws1 = unicodeSpaces1 // Parser<unit, unit> = skipMany1Satisfy (fun ch -> Char.IsWhiteSpace(ch))
let pstr s = pstring s



// ----------------
// identifier setup
// ----------------
let isAsciiIdStart    = fun c -> isAsciiLetter c || c = '_' || c = '$' || isDigit c || c = '@' || c = '^' || c = '?' || c = '/'
let isAsciiIdContinue = fun c -> isAsciiLetter c || isDigit c || c = '_' || c = '.' || c = '-' || c = '#' || c = '/' || c = '\\' || c = '|' || c = ' ' || c = ',' || c = '?'


/// <summary>
/// identifier between q
/// </summary>
let internal p_fieldIdentifier (q: string) =
    skipString q
    >>. satisfy isAsciiIdStart
    .>>. manyCharsTill (satisfy isAsciiIdContinue) (
        previousCharSatisfiesNot ((=) '\\')
        >>. choice [
            if q = "" then
                choice [
                    ws >>. nextCharSatisfies ((=) ':')
                    eof
                ]
            else
                choice [
                    attempt (next2CharsSatisfyNot (fun ch1 ch2 -> (ch1 = '\\' && ch2 <> '\\' && isAsciiIdContinue ch2) || (ch1 <> '\\' && isAsciiIdContinue ch1) ) >>. skipString q)
                    skipString q
                ]
        ]
    )
    |>> (fun (ch, s) -> sprintf "%c%s" ch s)

/// ws >>. skipChar ':' >>. ws
let internal p_fieldDelimiterSpaceWrapped =
    ws
    >>. skipChar ':'
    >>. ws


let internal p_fieldStringValue (q: string) =
    skipString q
    >>. manyCharsTill anyChar (previousCharSatisfiesNot ((=) '\\') >>. skipString q)

// TODO: FIX-1.0: uncomment to fix no quoted
// let internal p_quotelessFieldStringValue =
//     let isEndOfFieldValue = fun c -> c = ',' || c = '}'
//     manyCharsTill anyChar (ws >>. choice [ skipSatisfy isEndOfFieldValue; eof ])

let internal p_fieldStringValueChoice (q: string) =
    choice [
        p_fieldStringValue "\\\"" |> attempt
        p_fieldStringValue "\"" |> attempt

        if not (String.IsNullOrEmpty(q)) then
            p_fieldStringValue q |> attempt
        // TODO: FIX-1.0: uncomment to fix no quoted
        // else
        //     p_quotelessFieldStringValue
    ]
// =================
// predefined fields
// =================

let private p_predefinedFieldIdentifierDelimiter q names =
    names
    |> List.map (fun n -> skipStringCI $"{q}{n}{q}")
    |> choice
    >>. p_fieldDelimiterSpaceWrapped


let internal p_predefinedStringField q names mapValue =
    p_predefinedFieldIdentifierDelimiter q names
    >>? p_fieldStringValueChoice q
    |>> mapValue

let internal p_predefinedNullField q names f =
    p_predefinedFieldIdentifierDelimiter q names
    >>? (skipStringCI "null" <|> skipStringCI $"{q}null{q}")
    |>> (fun () -> f)

let internal p_predefinedIntField q names mapValue =
    p_predefinedFieldIdentifierDelimiter q names
    >>. (pint32 <|> attempt (skipStringCI q >>. pint32 .>> skipStringCI q))
    |>> mapValue



// ==============
// special fields
// ==============


let private traceLevels = [ "Trace"; "VRB"; "TRC"; ]
let private debugLevels = [ "Debug"; "DBG"; ]
let private informationLevels = [ "Information"; "Info"; "INF"; ]
let private warningLevels = [ "Warning"; "Warn"; "WRN"; ]
let private errorLevels = [ "Error"; "Err"; ]
let private criticalLevels = [ "Critical"; "Fatal"; "FTL"; "CRT"; "crit" ]

let internal p_logLevelField q =
    p_predefinedStringField q ["logLevel"; "level"; "@l"] (fun s ->
        if traceLevels |> List.exists (equalOrdinalCI s) then LogLevel.Trace |> TechField.Level
        elif debugLevels |> List.exists (equalOrdinalCI s) then LogLevel.Debug |> TechField.Level
        elif informationLevels |> List.exists (equalOrdinalCI s) then LogLevel.Information |> TechField.Level
        elif warningLevels |> List.exists (equalOrdinalCI s) then LogLevel.Warning |> TechField.Level
        elif errorLevels |> List.exists (equalOrdinalCI s) then LogLevel.Error |> TechField.Level
        elif criticalLevels |> List.exists (equalOrdinalCI s) then LogLevel.Critical |> TechField.Level
        else LogLevel.None |> TechField.Level
    )


let internal p_statusCodeField q =
    p_predefinedFieldIdentifierDelimiter q ["statusCode"]
    >>. choice [
        p_fieldStringValue q
        pint32 |>> sprintf "%i"
    ]
    |>> (fun s ->
        match Enum.TryParse(typeof<HttpStatusCode>, s, true) with
        | true, l -> unbox l |> TechField.StatusCode
        | false, _ -> (LanguagePrimitives.EnumOfValue 0) |> TechField.StatusCode
    )

let internal p_port q =
    p_predefinedIntField q ["port"] TechField.Port

let internal p_timestamp q =
    choice [
        p_predefinedStringField q ["timespan"; "timestamp"; "@timestamp"; "@t"] (Timespan.Value >> TechField.Timespan)
        p_predefinedNullField q ["timespan"; "timestamp"; "@timestamp"; "@t"] (TechField.Timespan Timespan.Null)
    ]

let internal p_host q = p_predefinedStringField q ["host"] TechField.Host
let internal p_sourceContext q = p_predefinedStringField q ["sourceContext"] TechField.SourceContext
let internal p_path q = p_predefinedStringField q ["path"] TechField.Path
let internal p_method q = p_predefinedStringField q ["method"] TechField.Method
let internal p_hierarchicalTraceId q = p_predefinedStringField q ["hierarchicalTraceId"] TechField.HierarchicalTraceId
let internal p_connectionId q = p_predefinedStringField q ["connectionId"] TechField.ConnectionId
let internal p_parentId q = p_predefinedStringField q ["parentId"] TechField.ParentId
let internal p_traceId q = p_predefinedStringField q ["traceId"] TechField.TraceId
let internal p_spanId q = p_predefinedStringField q ["spanId"] TechField.SpanId
let internal p_requestPath q = p_predefinedStringField q ["requestPath"] TechField.RequestPath
let internal p_requestId q = p_predefinedStringField q ["requestId"] TechField.RequestId
let internal p_eventId q = p_predefinedStringField q ["eventId"] TechField.EventId


let p_specialField q =
    choice [
        p_port q
        p_timestamp q
        p_host q
        p_sourceContext q
        p_path q
        p_method q
        p_hierarchicalTraceId q
        p_connectionId q
        p_parentId q
        p_traceId q
        p_spanId q
        p_requestPath q
        p_requestId q
        p_eventId q
        p_logLevelField q
        p_statusCodeField q
    ]


// ================
// primitive fields
// ================
let internal p_nullField q =
    p_fieldIdentifier q
    .>> p_fieldDelimiterSpaceWrapped
    .>>? skipStringCI "null"
    |>> TechField.Null


let internal p_stringField q =
    p_fieldIdentifier q
    .>> p_fieldDelimiterSpaceWrapped
    .>>.? p_fieldStringValueChoice q
    |>> TechField.String


let internal p_quotelessStringField q =
    p_fieldIdentifier q
    .>> p_fieldDelimiterSpaceWrapped
    .>>.? manyCharsTill anyChar (nextCharSatisfies ((=) ',') <|> nextCharSatisfies ((=) '}') <|> nextCharSatisfies ((=) ')') <|> (followedBy newline) <|> (followedBy eof) )
    |>> (fun t -> TechField.String (fst t, (snd t).Trim()))


let internal p_intField q =
    p_fieldIdentifier q
    .>>? p_fieldDelimiterSpaceWrapped
    .>>.? choice [
        pint32
    ]
    .>> ws
    .>>? followedBy (skipChar ',' <|> skipChar '}' <|> eof <|> skipNewline)
    |>> TechField.Int


let internal p_boolField q value =
    p_fieldIdentifier q
    .>> p_fieldDelimiterSpaceWrapped
    .>>? skipStringCI $"{value}"
    |>> (fun n -> TechField.Bool (n, value))


let internal p_arrayString q =
    p_fieldIdentifier q
    .>>? p_fieldDelimiterSpaceWrapped
    .>>? skipChar '['
    .>> ws
    .>>.? sepEndBy (p_fieldStringValue q) (attempt(ws >>. skipChar ',' >>. ws))
    .>> ws
    .>> skipChar ']'
    |>> TechField.Array


let internal p_arrayInt q =
    p_fieldIdentifier q
    .>>? p_fieldDelimiterSpaceWrapped
    .>>? skipChar '['
    .>> ws
    .>>.? sepEndBy (pint32) (attempt(ws >>. skipChar ',' >>. ws))
    .>> ws
    .>> skipChar ']'
    |>> TechField.ArrayInt


let p_arrayPrimitiveField q =
    choice [
        p_arrayString q |> attempt
        p_arrayInt q    |> attempt
    ]


let p_primitiveField q =
    choice [
        p_nullField q |> attempt
        p_intField q |> attempt
        p_boolField q true |> attempt
        p_boolField q false |> attempt
        p_stringField q |> attempt
        p_quotelessStringField q |> attempt
    ]



// ===========
// json fields
// ===========


let p_TechLogNoQuotes, p_TechLogNoQuotesR = createParserForwardedToRef()
let p_TechLogWithQuotes, p_TechLogWithQuotesR = createParserForwardedToRef()
let p_TechLogWithEscapedQuotes, p_TechLogWithEscapedQuotesR = createParserForwardedToRef()


// =============
// json
// =============

let private p_jsonChoice q =
    let ws' =
        choice [
            skipString "\\n" >>. ws
            ws
        ]

    let p_jsonEmpty =
        between (skipChar '{') (skipChar '}') ws
        |>> (fun () -> [])

    let ``open`` q =
        skipStringCI q >>? ws'

    let ``close`` q =
        ws' >>. skipStringCI q

    // can be simplified
    choice [
        if not (String.IsNullOrEmpty q) then
            (``open`` q >>? p_TechLogWithEscapedQuotes .>> ``close`` q) |> attempt
            (``open`` q >>? p_TechLogWithQuotes        .>> ``close`` q) |> attempt
            (``open`` q >>? p_jsonEmpty                    .>> ``close`` q) |> attempt
        
        (``open`` "\\\"" >>? p_TechLogWithEscapedQuotes .>> ``close`` "\\\"") |> attempt
        (``open`` "\\\"" >>? p_TechLogWithQuotes        .>> ``close`` "\\\"") |> attempt
        (``open`` "\\\"" >>? p_jsonEmpty                    .>> ``close`` "\\\"") |> attempt
        
        (``open`` "\"" >>? p_TechLogWithEscapedQuotes .>> ``close`` "\"") |> attempt
        (``open`` "\"" >>? p_TechLogWithQuotes        .>> ``close`` "\"") |> attempt
        (``open`` "\"" >>? p_jsonEmpty                    .>> ``close`` "\"") |> attempt
        
        (``open`` "\"\"\"" >>? p_TechLogWithEscapedQuotes .>> ``close`` "\"\"\"") |> attempt // Kibana: "fullMessage": """{...}"""
        (``open`` "\"\"\"" >>? p_TechLogWithQuotes        .>> ``close`` "\"\"\"") |> attempt // Kibana: "fullMessage": """{...}"""
        
        (ws' >>? p_TechLogWithEscapedQuotes) |> attempt
        (ws' >>? p_TechLogWithQuotes) |> attempt
        (ws' >>? p_TechLogNoQuotes) |> attempt
        (ws' >>? p_jsonEmpty) |> attempt
    ]


let internal p_annotation q =
    let pannotation =
        if not (String.IsNullOrEmpty q) then
            many1CharsTill anyChar (nextCharSatisfies ((=) '[') <|> nextCharSatisfies ((=) '{') <|> (previousCharSatisfiesNot ((=) '\\') >>. skipString q))
        else
            many1CharsTill (satisfy ((<>) '\"')) (nextCharSatisfies ((=) '[') <|> nextCharSatisfies ((=) '{') <|> nextCharSatisfies ((=) ',') <|> nextCharSatisfies ((=) '}') )
    
    pannotation
    >>= (fun annotation ->
        if annotation.ToCharArray() |> Array.exists (Char.IsLetterOrDigit) then
            preturn annotation
        else
            fun _ ->
                Reply<string>(ReplyStatus.Error, messageError "bad annotation")
    )

/// `Annotation {<Json>}`
let p_jsonAnnotatedValue q =
    ws
    >>? p_annotation q
    .>> ws
    .>>.? p_jsonChoice q
    |>> (fun t2 -> { Key = ""; Annotation = (fst t2).Trim(); Body = snd t2 })
    .>> ws


let p_jsonField q =
    p_fieldIdentifier q
    .>>? p_fieldDelimiterSpaceWrapped
    .>>.? p_jsonChoice q
    |>> TechField.Json


let p_body q =
    p_predefinedFieldIdentifierDelimiter q ["body"]
    >>? p_jsonChoice q
    |>> TechField.Body


let p_arrayJsonAnnonimous q =
    ws
    >>? skipChar '['
    >>. ws
    >>? sepEndBy (p_jsonChoice q) (attempt(ws >>. skipChar ',' >>. ws))
    .>> ws
    .>> skipChar ']'
    |>> TechField.ArrayJsonAnnonimous

/// Array of json ojects or array of annonimous json objects
let p_arrayJson q =
    p_fieldIdentifier q
    .>>? p_fieldDelimiterSpaceWrapped
    .>>? skipChar '['
    .>> ws
    .>>.? sepEndBy (
        choice [
            (nextCharSatisfiesNot ((=) '[') >>. skipStringCI "null" |>> (fun _ -> TechField.NullAnnonimous |> List.singleton)) |> attempt
            (nextCharSatisfiesNot ((=) '[') >>. p_fieldStringValue q |>> (TechField.StringAnnonimous >> List.singleton)) |> attempt
            (nextCharSatisfiesNot ((=) '[') >>. pint32 |>> (TechField.IntAnnonimous >> List.singleton)) |> attempt
            (nextCharSatisfiesNot ((=) '[') >>. p_jsonChoice q) |> attempt
            p_arrayJsonAnnonimous q |>> List.singleton |> attempt
        ]
    ) (attempt(ws >>. skipChar ',' >>. ws))
    .>> ws
    .>> skipChar ']'
     |>> TechField.ArrayJson




let p_jsonAnnotated q =
    p_fieldIdentifier q
    .>>? p_fieldDelimiterSpaceWrapped
    .>> ws
    .>>.? choice [
        (skipStringCI q >>. p_jsonAnnotatedValue q .>>? skipStringCI q) |> attempt
        (p_jsonAnnotatedValue NO_QUOTES) |> attempt
    ]
    |>> (fun (key, jsonAnnotated) -> { jsonAnnotated with Key = key } |> TechField.JsonAnnotated)


let p_arrayJsonAnnotated q =
    p_fieldIdentifier q
    .>>? p_fieldDelimiterSpaceWrapped
    .>>? skipChar '['
    .>> ws
    .>>.? sepEndBy (p_jsonAnnotatedValue q) (attempt(ws >>. skipChar ',' >>. ws))
    .>> ws
    .>> skipChar ']'
     |>> TechField.ArrayJsonAnnotated


// =============
// message
// =============

let messageFieldNames = ["message"; "@m"; "@mt"; "msg"]

/// \"message\": \"Returning next host: rabbitmq_node:5672\"
let p_messageString q =
    p_predefinedStringField q messageFieldNames TechField.Message


/// (<typeJson>)
let p_messageJson q =
    ws
    >>? between (pchar '(' >>. ws) (ws .>> pchar ')') 
        (
            choice [
                p_arrayJson q |> attempt
                p_arrayJsonAnnotated q |> attempt
                p_jsonAnnotated q |> attempt
                p_jsonField q |> attempt
                p_specialField q |> attempt
                p_primitiveField q |> attempt
            ]
        )
    .>> ws


/// [ (<typeJson>)* ]
let p_messageJsonList q =
    between (skipChar '[' >>. ws) (ws .>> skipChar ']') 
        (sepEndBy (p_messageJson q) (skipChar ','))


let p_messageBoddiedNotClosed q =
    p_predefinedFieldIdentifierDelimiter q ["message"; "@m"; "@mt"]
    >>? skipChar '\"'
    >>? p_annotation q
    .>>.? choice [
        attempt(p_jsonChoice "\\\"")
        attempt(p_jsonChoice q)
        attempt(p_messageJsonList "\\\"")
        attempt(p_messageJsonList q)
    ]


let p_messageBuddied q =
    p_messageBoddiedNotClosed q <??> $"p_messageBoddiedNotClosed (q is <{q}>)"
    .>>? skipChar '\"'
    |>> (fun t -> TechField.MessageBoddied ((fst t).Trim(), (snd t)))


let p_messageBuddiedWithPostfix q =
    p_messageBoddiedNotClosed q
    .>>.? many1CharsTill anyChar (nextCharSatisfies ((=) '\"'))
    .>>? skipChar '\"'
    |>> (fun t -> 
        let ((header, body), postfix) = t
        TechField.MessageBoddiedWithPostfix (header.Trim(), body, postfix.Trim())
    )


let p_message q =
    choice [
        attempt (p_messageBuddied q)
        attempt (p_messageBuddiedWithPostfix q)
        attempt (p_messageString q)
    ]

// ===============================================

let p_TechField q : Parser<TechField, unit> =
    choice [
        skipString "\\n" >>. ws
        ws
    ]
    >>. choice [
        p_specialField q |> attempt
        p_message q |> attempt
        p_body q |> attempt
        p_arrayPrimitiveField q |> attempt
        p_arrayJsonAnnotated q |> attempt
        p_jsonAnnotated q |> attempt
        p_arrayJson q |> attempt
        p_jsonField q |> attempt
        p_primitiveField q |> attempt
    ]
    .>> choice [
        skipString "\\n" >>. ws
        ws
    ]


do
    p_TechLogNoQuotesR.Value <-
        between (skipChar '{') (skipChar '}' >>. skipManySatisfy (fun ch -> ch <> '\n' && Char.IsWhiteSpace(ch))) (sepEndBy (p_TechField NO_QUOTES) (skipChar ','))

    p_TechLogWithQuotesR.Value <-
        between (skipChar '{') (skipChar '}' >>. skipManySatisfy (fun ch -> ch <> '\n' && Char.IsWhiteSpace(ch))) (sepEndBy (p_TechField QUOTES) (skipChar ','))

    p_TechLogWithEscapedQuotesR.Value <-
        between (skipChar '{') (skipChar '}' >>. skipManySatisfy (fun ch -> ch <> '\n' && Char.IsWhiteSpace(ch))) (sepEndBy (p_TechField ESCAPED_QUOTES) (skipChar ','))





let logList =
    /// Tries exctract `fullMessage` field and combine in with others
    let mergeFullMessage fieldList = 
        fieldList
        |> List.partition (fun f ->
            match f with
            | TechField.Json (key, _) when key = "fullMessage" -> true // when there is kibana fillMessage
            | _ -> false
        )
        |> (fun (fullMessageJson, other) ->
            let fullMessageFields =
                fullMessageJson
                |> List.tryHead // only one field can be
                |> Option.map (fun f ->
                    match f with
                    | TechField.Json (key, fl) ->
                        f :: fl
                    | _ -> []
                )
                |> Option.defaultValue []

            (fullMessageFields @ other)
            |> List.distinctBy (fun f -> f |> TechField.key)
        )

    /// example: `some text {<json>}`
    let p_sourcedTechLog =
        manyCharsTill anyChar (nextCharSatisfies ((=) '{') <|> nextCharSatisfies ((=) '\n')) 
        .>>.? p_TechLogWithQuotes
        |>> (fun (source, fieldList) -> 
            {
                // start part of docker log:
                // PMB_WAN_foo_stub.1.o9sjfn7@srv-baz2.technics.bos    | {"timestamp":"2022-07-13T09:06:13.475Z","message":"...
                Source =
                    if String.IsNullOrWhiteSpace(source) then
                        None
                    else
                        ("logSource", source.Trim()) |> TechField.String |> Some; 
                Fields = mergeFullMessage fieldList
            } |> Log.TechLog)
        

    let p_TechLog =
        p_TechLogWithQuotes |>> (fun t -> {Source = None; Fields = t} |> Log.TechLog)

    ws
    >>? sepEndBy 
        (   choice [
                p_sourcedTechLog |> attempt
                p_TechLog |> attempt
                many1Satisfy ((<>) '\n') |>> Log.TextLog
            ]
        )
        (
            choice [
                skipChar ',' >>? newline >>. ws
                skipChar '.' >>? newline >>. ws
                newline >>. ws
                skipChar ','
                ws >>. nextCharSatisfies ((=)'{')
            ]
        )
    .>> eof


let parse input =

    run logList input
    |> function
        | Success (ok,_,_) -> Result.Ok ok
        | Failure (err,_,_) -> Result.Error err
