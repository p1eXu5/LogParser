// [<RequireQualifiedAccess>]
module LogParser.TechLogParser

open System
open FParsec
open LogParser.Types
open Microsoft.Extensions.Logging
open System.Net

[<Literal>]
let internal NO_QUOTES = ""

[<Literal>]
let internal QUOTES = "\""

[<Literal>]
let internal ESCAPED_QUOTES = "\\\""


///<summary>
/// Skips over any sequence of *zero* or more whitespaces (space (' '), tab ('\t')
/// or newline ("\n", "\r\n" or "\r")).
///</summary> 
let internal ws = unicodeSpaces // skipManySatisfy (fun ch -> Char.IsWhiteSpace(ch))
let internal ws1 = unicodeSpaces1 // Parser<unit, unit> = skipMany1Satisfy (fun ch -> Char.IsWhiteSpace(ch))
let internal pstr s = pstring s



// ----------------
// identifier setup
// ----------------
let internal isAsciiIdStart    = fun c -> isAsciiLetter c || c = '_' || c = '$' || isDigit c || c = '@' || c = '^' || c = '?' || c = '/'
let internal isAsciiIdContinue = fun c -> isAsciiLetter c || isDigit c || c = '_' || c = '.' || c = '-' || c = '#' || c = '/' || c = '\\' || c = '|' || c = ' ' || c = ',' || c = '?'


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

/// Skips names and p_fieldDelimiterSpaceWrapped
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
        if traceLevels |> List.exists (equalOrdinalCI s) then LogLevel.Trace |> TechJsonLogField.Level
        elif debugLevels |> List.exists (equalOrdinalCI s) then LogLevel.Debug |> TechJsonLogField.Level
        elif informationLevels |> List.exists (equalOrdinalCI s) then LogLevel.Information |> TechJsonLogField.Level
        elif warningLevels |> List.exists (equalOrdinalCI s) then LogLevel.Warning |> TechJsonLogField.Level
        elif errorLevels |> List.exists (equalOrdinalCI s) then LogLevel.Error |> TechJsonLogField.Level
        elif criticalLevels |> List.exists (equalOrdinalCI s) then LogLevel.Critical |> TechJsonLogField.Level
        else LogLevel.None |> TechJsonLogField.Level
    )


let internal p_statusCodeField q =
    p_predefinedFieldIdentifierDelimiter q ["statusCode"]
    >>. choice [
        p_fieldStringValue q
        pint32 |>> sprintf "%i"
    ]
    |>> (fun s ->
        match Enum.TryParse(typeof<HttpStatusCode>, s, true) with
        | true, l -> unbox l |> TechJsonLogField.StatusCode
        | false, _ -> (LanguagePrimitives.EnumOfValue 0) |> TechJsonLogField.StatusCode
    )

let internal p_port q =
    p_predefinedIntField q ["port"] TechJsonLogField.Port

let internal p_timestamp q =
    choice [
        p_predefinedStringField q ["timespan"; "timestamp"; "@timestamp"; "@t"] (Timespan.Value >> TechJsonLogField.Timespan)
        p_predefinedNullField q ["timespan"; "timestamp"; "@timestamp"; "@t"] (TechJsonLogField.Timespan Timespan.Null)
    ]

let internal p_host q = p_predefinedStringField q ["host"] TechJsonLogField.Host
let internal p_sourceContext q = p_predefinedStringField q ["sourceContext"] TechJsonLogField.SourceContext
let internal p_path q = p_predefinedStringField q ["path"] TechJsonLogField.Path
let internal p_method q = p_predefinedStringField q ["method"] TechJsonLogField.Method
let internal p_hierarchicalTraceId q = p_predefinedStringField q ["hierarchicalTraceId"] TechJsonLogField.HierarchicalTraceId
let internal p_connectionId q = p_predefinedStringField q ["connectionId"] TechJsonLogField.ConnectionId
let internal p_parentId q = p_predefinedStringField q ["parentId"] TechJsonLogField.ParentId
let internal p_traceId q = p_predefinedStringField q ["traceId"] TechJsonLogField.TraceId
let internal p_spanId q = p_predefinedStringField q ["spanId"] TechJsonLogField.SpanId
let internal p_requestPath q = p_predefinedStringField q ["requestPath"] TechJsonLogField.RequestPath
let internal p_requestId q = p_predefinedStringField q ["requestId"] TechJsonLogField.RequestId
let internal p_eventId q = p_predefinedStringField q ["eventId"] TechJsonLogField.EventId


let internal p_specialField q =
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
    |>> TechJsonLogField.Null


let internal p_stringField q =
    p_fieldIdentifier q
    .>> p_fieldDelimiterSpaceWrapped
    .>>.? p_fieldStringValueChoice q
    |>> TechJsonLogField.String


let internal p_quotelessStringField q =
    p_fieldIdentifier q
    .>> p_fieldDelimiterSpaceWrapped
    .>>.? manyCharsTill anyChar (nextCharSatisfies ((=) ',') <|> nextCharSatisfies ((=) '}') <|> nextCharSatisfies ((=) ')') <|> (followedBy newline) <|> (followedBy eof) )
    |>> (fun t -> TechJsonLogField.String (fst t, (snd t).Trim()))


let internal p_intField q =
    p_fieldIdentifier q
    .>>? p_fieldDelimiterSpaceWrapped
    .>>.? choice [
        pint32
    ]
    .>> ws
    .>>? followedBy (skipChar ',' <|> skipChar '}' <|> eof <|> skipNewline)
    |>> TechJsonLogField.Int


let internal p_boolField q value =
    p_fieldIdentifier q
    .>> p_fieldDelimiterSpaceWrapped
    .>>? skipStringCI $"{value}"
    |>> (fun n -> TechJsonLogField.Bool (n, value))

/// Wrapped q[
let inline internal p_squareBraketOpenW q =
    skipString q
    .>> ws
    .>>? skipChar '['
    .>> ws

/// Wrapped ]q
let inline internal p_squareBraketCloseW q =
    ws
    .>>? skipChar ']'
    .>> ws
    .>>? skipString q

let internal p_squareBraketOpen : Parser<unit, unit> =
    skipChar '['
    .>> ws

let internal p_squareBraketClose : Parser<unit, unit> =
    ws
    .>>? skipChar ']'


let internal p_arrayString q =
    let p = sepEndBy (p_fieldStringValue q) (attempt(ws >>. skipChar ',' >>. ws))

    p_fieldIdentifier q
    .>>? p_fieldDelimiterSpaceWrapped
    .>>.? choice [
        (p_squareBraketOpenW q >>. p .>> p_squareBraketCloseW q) |> attempt
        (p_squareBraketOpen >>. p .>> p_squareBraketClose) |> attempt
    ]
    |>> TechJsonLogField.Array


let internal p_arrayInt q =
    let p = sepEndBy (pint32) (attempt(ws >>. skipChar ',' >>. ws))

    p_fieldIdentifier q
    .>>? p_fieldDelimiterSpaceWrapped
    .>>.? choice [
        (p_squareBraketOpenW q >>. p .>> p_squareBraketCloseW q) |> attempt
        (p_squareBraketOpen >>. p .>> p_squareBraketClose) |> attempt
    ]
    |>> TechJsonLogField.ArrayInt


let internal p_arrayPrimitiveField q =
    choice [
        p_arrayString q |> attempt
        p_arrayInt q    |> attempt
    ]


let internal p_primitiveField q =
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


let internal p_TechLogNoQuotes, internal p_TechLogNoQuotesR = createParserForwardedToRef()
let internal p_TechLogWithQuotes, internal p_TechLogWithQuotesR = createParserForwardedToRef()
let internal p_TechLogWithEscapedQuotes, internal p_TechLogWithEscapedQuotesR = createParserForwardedToRef()


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
let internal p_jsonAnnotatedValue q =
    ws
    >>? p_annotation q
    .>> ws
    .>>.? p_jsonChoice q
    |>> (fun t2 -> { Key = ""; Annotation = (fst t2).Trim(); Body = snd t2 })
    .>> ws


let internal p_jsonField q =
    p_fieldIdentifier q
    .>>? p_fieldDelimiterSpaceWrapped
    .>>.? p_jsonChoice q
    |>> TechJsonLogField.Json


let internal p_body q =
    p_predefinedFieldIdentifierDelimiter q ["body"]
    >>? p_jsonChoice q
    |>> TechJsonLogField.Body


let internal p_arrayJsonAnnonimous q =
    let p = sepEndBy (p_jsonChoice q) (attempt(ws >>. skipChar ',' >>. ws))
    ws
    >>? choice [
        (p_squareBraketOpenW q >>. p .>> p_squareBraketCloseW q) |> attempt
        (p_squareBraketOpen >>. p .>> p_squareBraketClose) |> attempt
    ]
    |>> TechJsonLogField.ArrayJsonAnnonimous

let internal p_arrayJsonItems q =
    sepEndBy (
        choice [
            (nextCharSatisfiesNot ((=) '[') >>. skipStringCI "null" |>> (fun _ -> TechJsonLogField.NullAnnonimous |> List.singleton)) |> attempt
            (nextCharSatisfiesNot ((=) '[') >>. p_fieldStringValue q |>> (TechJsonLogField.StringAnnonimous >> List.singleton)) |> attempt
            (nextCharSatisfiesNot ((=) '[') >>. pint32 |>> (TechJsonLogField.IntAnnonimous >> List.singleton)) |> attempt
            (nextCharSatisfiesNot ((=) '[') >>. p_jsonChoice q) |> attempt
            p_arrayJsonAnnonimous q |>> List.singleton |> attempt
        ]
    ) (attempt(ws >>. skipChar ',' >>. ws))


/// Array of json ojects or array of annonimous json objects
let internal p_arrayJson q =
    p_fieldIdentifier q
    .>>? p_fieldDelimiterSpaceWrapped
    .>>.? choice [
        (p_squareBraketOpenW q >>. p_arrayJsonItems q .>> p_squareBraketCloseW q) |> attempt
        (p_squareBraketOpen >>. p_arrayJsonItems q .>> p_squareBraketClose) |> attempt
    ]
     |>> TechJsonLogField.ArrayJson



let internal p_jsonAnnotated q =
    p_fieldIdentifier q
    .>>? p_fieldDelimiterSpaceWrapped
    .>> ws
    .>>.? choice [
        (skipStringCI q >>. p_jsonAnnotatedValue q .>>? skipStringCI q) |> attempt
        (p_jsonAnnotatedValue NO_QUOTES) |> attempt
    ]
    |>> (fun (key, jsonAnnotated) -> { jsonAnnotated with Key = key } |> TechJsonLogField.JsonAnnotated)


let internal p_arrayJsonAnnotated q =
    p_fieldIdentifier q
    .>>? p_fieldDelimiterSpaceWrapped
    .>>? skipChar '['
    .>> ws
    .>>.? sepEndBy (p_jsonAnnotatedValue q) (attempt(ws >>. skipChar ',' >>. ws))
    .>> ws
    .>> skipChar ']'
     |>> TechJsonLogField.ArrayJsonAnnotated


// =============
// message
// =============

let internal messageFieldNames = ["message"; "@m"; "@mt"; "msg"]

/// \"message\": \"Returning next host: rabbitmq_node:5672\"
let internal p_messageString q =
    p_predefinedStringField q messageFieldNames TechJsonLogField.Message

/// Array of json ojects or array of annonimous json objects
let internal p_messageArrayJson q =
    p_predefinedFieldIdentifierDelimiter q messageFieldNames
    >>. choice [
        (p_squareBraketOpenW q >>. p_arrayJsonItems q .>> p_squareBraketCloseW q) |> attempt
        (p_squareBraketOpen >>. p_arrayJsonItems q .>> p_squareBraketClose) |> attempt
    ]
    |>> TechJsonLogField.MessageArrayJson

/// (<typeJson>)
let internal p_jsonSpecialPrimitiveInBraces q =
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
let internal p_messageJsonList q =
    between (skipChar '[' >>. ws) (ws .>> skipChar ']') 
        (sepEndBy (p_jsonSpecialPrimitiveInBraces q) (skipChar ','))


let internal p_messageBoddiedNotClosed q =
    p_predefinedFieldIdentifierDelimiter q messageFieldNames
    >>? skipChar '\"'
    >>? p_annotation q
    .>>.? choice [
        attempt(p_jsonChoice "\\\"")
        attempt(p_jsonChoice q)
        attempt(p_messageJsonList "\\\"")
        attempt(p_messageJsonList q)
    ]


let internal p_messageBuddied q =
    p_messageBoddiedNotClosed q <??> $"p_messageBoddiedNotClosed (q is <{q}>)"
    .>>? skipChar '\"'
    |>> (fun t -> TechJsonLogField.MessageBoddied ((fst t).Trim(), (snd t)))


let internal p_messageBuddiedWithPostfix q =
    p_messageBoddiedNotClosed q
    .>>.? many1CharsTill anyChar (nextCharSatisfies ((=) '\"'))
    .>>? skipChar '\"'
    |>> (fun t -> 
        let ((header, body), postfix) = t
        TechJsonLogField.MessageBoddiedWithPostfix (header.Trim(), body, postfix.Trim())
    )


let internal p_message q =
    choice [
        attempt (p_messageBuddied q)
        attempt (p_messageBuddiedWithPostfix q)
        attempt (p_messageArrayJson q)
        attempt (p_messageString q)
    ]

// ===============================================

let internal p_TechField q : Parser<TechJsonLogField, unit> =
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




/// Tries exctract `fullMessage` field and combine in with others
let private mergeFullMessage fieldList = 
    fieldList
    |> List.partition (fun f ->
        match f with
        | TechJsonLogField.Json (key, _) when key = "fullMessage" -> true // when there is kibana fillMessage
        | _ -> false
    )
    |> (fun (fullMessageJson, other) ->
        let fullMessageFields =
            fullMessageJson
            |> List.tryHead // only one field can be
            |> Option.map (fun f ->
                match f with
                | TechJsonLogField.Json (key, fl) ->
                    f :: fl
                | _ -> []
            )
            |> Option.defaultValue []

        (fullMessageFields @ other)
        |> List.distinctBy (fun f -> f |> TechJsonLogField.key)
    )

/// example: `some text {<json>}`
let private p_sourcedTechLog (observer: IObserver<TechLogPosition>) =
    getPosition .>>. 
    manyCharsTill anyChar (nextCharSatisfies ((=) '{') <|> nextCharSatisfies ((=) '\n')) 
    .>>.? p_TechLogWithQuotes
    .>>. getPosition
    |>> (fun (((startPosition, source), fieldList), endPosition) ->
        let log =
            {
                // start part of docker log:
                // PMB_WAN_foo_stub.1.o9sjfn7@srv-baz2.technics.bos    | {"timestamp":"2022-07-13T09:06:13.475Z","message":"...
                Source =
                    if String.IsNullOrWhiteSpace(source) then
                        None
                    else
                        ("logSource", source.Trim()) |> TechJsonLogField.String |> Some; 
                Fields = mergeFullMessage fieldList
            } |> TechLog.JsonLog

        observer.OnNext({ Start = startPosition; End = endPosition; Log = log })
        log
    )
        

let private p_TechLog (observer: IObserver<TechLogPosition>) =
    getPosition .>>. p_TechLogWithQuotes .>>. getPosition
    |>> (fun ((startPosition, techFieldList), endPosition) ->
        let log = {Source = None; Fields = techFieldList} |> TechLog.JsonLog
        observer.OnNext({ Start = startPosition; End = endPosition; Log = log })
        log
    )

let private p_TextLog (observer: IObserver<TechLogPosition>) =
    getPosition .>>. many1Satisfy ((<>) '\n') .>>. getPosition
    |>> (fun ((startPosition, text), endPosition) ->
        let log = TechLog.TextLog text
        observer.OnNext({ Start = startPosition; End = endPosition; Log = log })
        log
    )

let internal logList (observer: IObserver<TechLogPosition>) =
    ws
    >>? sepEndBy 
        ( 
            choice [
                p_sourcedTechLog observer |> attempt
                p_TechLog observer |> attempt
                p_TextLog observer
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

let internal logListIgnore (observer: IObserver<TechLogPosition>) =
    ws
    >>? skipSepEndBy
        ( 
            choice [
                p_sourcedTechLog observer |> attempt
                p_TechLog observer |> attempt
                p_TextLog observer
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


exception LogParsingException of string


let public parse (observer: IObserver<TechLogPosition>) input =
    run (logList observer) input
    |> function
        | Success (ok,_,_) ->
            observer.OnCompleted()
            Result.Ok ok

        | Failure (err, _, _) ->
            observer.OnError(err |> LogParsingException)
            Result.Error err

let public parseStream (observer: IObserver<TechLogPosition>) streamName stream =
    runParserOnStream (logListIgnore observer) () streamName stream (Text.Encoding.UTF8)
    |> function
        | Success (ok,_,_) ->
            observer.OnCompleted()
            Result.Ok ok

        | Failure (err, _, _) ->
            observer.OnError(err |> LogParsingException)
            Result.Error err
