module LogParser.Core.Parser

open System
open FParsec
open LogParser.Core.Types
open Microsoft.Extensions.Logging
open System.Net.Http
open System.Net

///<summary>
/// Skips over any sequence of *zero* or more whitespaces (space (' '), tab ('\t')
/// or newline ("\n", "\r\n" or "\r")).
///</summary> 
let ws = unicodeSpaces // skipManySatisfy (fun ch -> Char.IsWhiteSpace(ch))
let ws1 = unicodeSpaces1 // Parser<unit, unit> = skipMany1Satisfy (fun ch -> Char.IsWhiteSpace(ch))
let str s = pstring s



// ----------------
// identifier setup
// ----------------
let isAsciiIdStart    = fun c -> isAsciiLetter c || c = '_' || c = '$' || isDigit c
let isAsciiIdContinue = fun c -> isAsciiLetter c || isDigit c || c = '_' || c = '.'

let identifier : Parser<string, unit> =
    FParsec.CharParsers.identifier (IdentifierOptions(isAsciiIdStart = isAsciiIdStart, isAsciiIdContinue = isAsciiIdContinue))

let fieldStringValue (q: string) =
    skipString q
    >>. manyCharsTill anyChar (previousCharSatisfiesNot ((=) '\\') >>. skipString q)

/// <summary>
/// identifier between q
/// </summary>
let fieldIdentifier (q: string) =
    between (skipString q) (skipString q) identifier

let fieldDelimiter =
    ws
    >>. skipChar ':'
    >>. ws


let pfield name =
    (skipChar '\"' <|> skipString "\\\"")
    >>. skipString name
    >>. (skipChar '\"' <|> skipString "\\\"")
    >>. fieldDelimiter

let pfieldI =
    (skipChar '\"' <|> skipString "\\\"")
    >>. identifier
    .>> (skipChar '\"' <|> skipString "\\\"")
    .>> fieldDelimiter
    

let customStringField pIdentifier q =
    pIdentifier
    .>> fieldDelimiter
    .>>.? fieldStringValue q
    |>> TechnoField.String


let customQuotelessStringField pIdentifier =
    pIdentifier
    .>> fieldDelimiter
    .>>.? manyCharsTill anyChar (nextCharSatisfies ((=) ',') <|> nextCharSatisfies ((=) '}') <|> nextCharSatisfies ((=) ')') <|> (followedBy newline) )
    |>> (fun t -> TechnoField.String (fst t, (snd t).Trim()))


let customIntField pIdentifier =
    pIdentifier
    .>>? fieldDelimiter
    .>>.? pint32
    .>> ws
    .>>? followedBy (skipChar ',' <|> skipChar '}' <|> eof <|> skipNewline)
    |>> TechnoField.Int


let customBoolField pIdentifier value =
    pIdentifier
    .>> fieldDelimiter
    .>>? skipStringCI $"{value}"
    |>> (fun n -> TechnoField.Bool (n, value))


let array pIdentifier q =
    pIdentifier
    .>>? fieldDelimiter
    .>>? skipChar '['
    .>> ws
    .>>.? sepEndBy (fieldStringValue q) (attempt(ws >>. skipChar ',' >>. ws))
    .>> ws
    .>> skipChar ']'
    |>> TechnoField.Array


let arrayInt pIdentifier =
    pIdentifier
    .>>? fieldDelimiter
    .>>? skipChar '['
    .>> ws
    .>>.? sepEndBy (pint32) (attempt(ws >>. skipChar ',' >>. ws))
    .>> ws
    .>> skipChar ']'
    |>> TechnoField.ArrayInt




let nullField pIdentifier =
    pIdentifier
    .>> fieldDelimiter
    .>>? skipStringCI "null"
    |>> TechnoField.Null






let log, logR = createParserForwardedToRef()
let innerJson, innerJsonR = createParserForwardedToRef()
let parameterJson, parameterJsonR = createParserForwardedToRef()


let arrayJson pIdentifier =
    pIdentifier
    .>>? fieldDelimiter
    .>>? skipChar '['
    .>> ws
    .>>.? sepEndBy (innerJson) (attempt(ws >>. skipChar ',' >>. ws))
    .>> ws
    .>> skipChar ']'
    |>> TechnoField.ArrayJson


let json log pIdentifier =
    pIdentifier
    .>>? fieldDelimiter
    .>>.? choice [
        skipChar '\"' >>? log .>> skipChar '\"'
        log
    ]
    |>> TechnoField.Json


let notWrappedJson pIdentifier =
    pIdentifier
    .>>? fieldDelimiter
    .>>.? log
    |>> TechnoField.Json


let body =
    skipString "\"body\""
    >>? fieldDelimiter
    >>? skipChar '\"'
    >>. ws
    >>? innerJson
    .>> skipChar '\"'
    |>> TechnoField.Body


// -------------------------------
//            message
// -------------------------------

let message =
    skipString "\"message\""
    >>? fieldDelimiter
    >>? skipChar '\"'
    >>? manyCharsTill anyChar (previousCharSatisfies ((<>) '\\') >>. skipChar '\"')
    |>> TechnoField.Message


let pMessageBuddied =
    skipString "\"message\""
    >>? fieldDelimiter
    >>? skipChar '\"'
    >>? manyCharsTill anyChar (nextCharSatisfies ((=) '[') <|> nextCharSatisfies ((=) '{') <|> nextCharSatisfies ((=) '\"'))
    .>>.? attempt(innerJson)


let pBuddied =
    fieldIdentifier "\""
    .>>? fieldDelimiter
    .>>? skipChar '\"'
    .>>.? many1CharsTill anyChar (nextCharSatisfies ((=) '[') <|> nextCharSatisfies ((=) '{') <|> nextCharSatisfies ((=) '\"'))
    .>>.? attempt(innerJson)

let jsonAnnotated =
    pBuddied
    .>>? skipChar '\"'
    |>> (fun t -> 
        let ((key, header), fl) = t
        TechnoField.JsonAnnotated (key.Trim(), header, fl))

let messageBuddied =
    pMessageBuddied
    .>>? skipChar '\"'
    |>> (fun t -> TechnoField.MessageBoddied ((fst t).Trim(), (snd t)))

let messageBuddiedWithPostfix =
    pMessageBuddied
    .>>.? many1CharsTill anyChar (nextCharSatisfies ((=) '\"'))
    .>>? skipChar '\"'
    |>> (fun t -> 
        let ((k, fl), p) = t
        TechnoField.MessageBoddiedWithPostfix (k.Trim(), fl, p))


let typeJson p =
    ws
    >>. p
    .>>? fieldDelimiter
    .>>.? identifier
    .>> ws
    .>>.? parameterJson
    |>> (fun t3 -> { Key = fst t3 |> fst; TypeName = fst t3 |> snd; Body = snd t3 } )
    .>> ws

let partialTypeJson =
    ws
    >>? fieldDelimiter
    .>>.? identifier
    .>> ws
    .>>.? parameterJson
    |>> (fun t3 -> { Key = ""; TypeName = fst t3 |> snd; Body = snd t3 } )
    .>> ws


/// (<typeJson>)
let messageParameter =
    ws
    >>? between (pchar '(') (pchar ')') 
        (choice [
            typeJson (fieldIdentifier "\\\"") |>> MessageParameter.TypeJson
            ws >>? 
                choice [
                    customQuotelessStringField (fieldIdentifier "\\\"")
                ] 
                |>> MessageParameter.TechnoField
        ])
    .>> ws
    


/// [ (<typeJson>)* ]
let messageParameterList =
    between (skipChar '[') (skipChar ']') 
        (sepEndBy messageParameter (skipChar ','))


let messageParameterized =
    ws
    >>. skipString "\"message\""
    >>? fieldDelimiter
    >>? skipChar '\"'
    >>? 
        choice [
            attempt(manyCharsTill anyChar (nextCharSatisfies ((=) '[') <|> nextCharSatisfies ((=) '{') <|> nextCharSatisfies ((=) '\"')) .>>.? messageParameterList)
            attempt(
                manyCharsTill anyChar (nextCharSatisfies ((=) ':') <|> nextCharSatisfies ((=) '{') <|> nextCharSatisfies ((=) '\"')) 
                .>>.? partialTypeJson 
                |>> (fun t ->
                    let (m, tj) = t
                    let arr = m.Split(' ') |> Array.filter (fun s -> not (String.IsNullOrWhiteSpace(s)))
                    (
                        String.Join(' ', arr |> Array.take (arr.Length - 1)), 
                        {tj with Key = arr |> Array.last} |> MessageParameter.TypeJson |> List.singleton)
                )
            )
        ]
    .>> skipManySatisfy ((<>) '\"')
    .>> skipChar '\"'
    |>> (fun t -> TechnoField.MessageParameterized ((fst t).Trim(), (snd t)))
    .>> ws




let predefinedStringField name f =
    skipStringCI $"\"{name}\""
    >>. fieldDelimiter
    >>? fieldStringValue "\""
    |>> f


let predefinedNullField name f =
    skipStringCI $"\"{name}\""
    >>. fieldDelimiter
    >>? skipStringCI "null"
    |>> (fun () -> f)


let predefinedIntField name f =
    skipString $"\"{name}\""
    >>. fieldDelimiter
    >>. pint32
    |>> f


let logLevelField =
    skipStringCI $"\"level\""
    >>. fieldDelimiter
    >>. fieldStringValue "\""
    |>> (fun s ->
        match Enum.TryParse(typeof<LogLevel>, s, true) with
        | true, l -> unbox l |> TechnoField.Level
        | false, _ -> LogLevel.None |> TechnoField.Level
    )


let statusCodeField =
    skipStringCI $"\"statusCode\""
    >>. fieldDelimiter
    >>. choice [
        fieldStringValue "\""
        pint32 |>> sprintf "%i"
    ]
    |>> (fun s ->
        match Enum.TryParse(typeof<HttpStatusCode>, s, true) with
        | true, l -> unbox l |> TechnoField.StatusCode
        | false, _ -> (LanguagePrimitives.EnumOfValue 0) |> TechnoField.StatusCode
    )


let field : Parser<TechnoField, unit> =
    ws
    >>. choice [
        predefinedIntField "port" TechnoField.Port
        predefinedStringField "timestamp" (Timespan.Value >> TechnoField.Timespan)
        predefinedNullField "timestamp" (TechnoField.Timespan Timespan.Null)
        messageParameterized
        messageBuddied
        messageBuddiedWithPostfix
        message
        predefinedStringField "host" TechnoField.Host
        predefinedStringField "sourceContext" TechnoField.SourceContext
        predefinedStringField "path" TechnoField.Path
        predefinedStringField "method" TechnoField.Method
        predefinedStringField "host" TechnoField.Host
        predefinedStringField "hierarchicalTraceId" TechnoField.HierarchicalTraceId
        predefinedStringField "connectionId" TechnoField.ConnectionId
        predefinedStringField "parentId" TechnoField.ParentId
        predefinedStringField "traceId" TechnoField.TraceId
        predefinedStringField "spanId" TechnoField.SpanId
        predefinedStringField "requestPath" TechnoField.RequestPath
        predefinedStringField "requestId" TechnoField.RequestId
        predefinedStringField "eventId" TechnoField.EventId
        logLevelField
        statusCodeField
        body
        notWrappedJson (fieldIdentifier "\"")
        jsonAnnotated
        json innerJson (fieldIdentifier "\"")
        attempt(array (fieldIdentifier "\"") "\"")
        attempt(arrayInt (fieldIdentifier "\""))
        attempt(arrayJson (fieldIdentifier "\""))
        customStringField (fieldIdentifier "\"") "\""
        attempt(customIntField (fieldIdentifier "\""))
        customBoolField (fieldIdentifier "\"") true 
        customBoolField (fieldIdentifier "\"") false
        nullField (fieldIdentifier "\"")
        customQuotelessStringField (fieldIdentifier "\"")
    ]
    .>> ws


let innerField : Parser<TechnoField, unit> =
    ws
    >>. choice [
        attempt(array (fieldIdentifier "\\\"") "\\\"")
        attempt(arrayInt (fieldIdentifier "\\\""))
        attempt(arrayJson (fieldIdentifier "\\\""))
        customStringField (fieldIdentifier "\\\"") "\\\""
        attempt(customIntField (fieldIdentifier "\\\""))
        customBoolField (fieldIdentifier "\\\"") true 
        customBoolField (fieldIdentifier "\\\"") false
        json innerJson (fieldIdentifier "\\\"")
        nullField (fieldIdentifier "\\\"")
        customQuotelessStringField (fieldIdentifier "\\\"")
    ]
    .>> ws

let parameterField =
    ws
    >>. choice [
        attempt(array identifier "\\\"")
        attempt(arrayInt identifier)
        attempt(arrayJson identifier)
        customStringField identifier "\\\""
        attempt(customIntField identifier)
        customBoolField identifier true 
        customBoolField identifier false
        nullField identifier
        (typeJson identifier |>> TechnoField.TypeJson)
        json parameterJson identifier
        customQuotelessStringField identifier
    ]
    .>> ws


let logList =
    ws
    >>? sepEndBy 
        (   choice [
                attempt (
                    manyCharsTill anyChar (nextCharSatisfies ((=) '{')) 
                    .>>.? 
                    log 
                    |>> (fun t -> 
                        {
                            Source =
                                if String.IsNullOrWhiteSpace (fst t) then
                                    None
                                else
                                    (fst t).Trim() |> Some; 
                            Fields = snd t
                        } |> Log.TechnoLog)
                )
                log |>> (fun t -> {Source = None; Fields = t} |> Log.TechnoLog)
                many1Satisfy ((<>) '\n') |>> Log.TextLog
            ]
        )
        (
            choice [
                skipChar ',' >>? newline >>. ws
                newline >>. ws
                skipChar ','
            ]
        )
    .>> eof

let parse input =
    run logList input
    |> function
        | Success (ok,_,_) -> Result.Ok ok
        | Failure (err,_,_) -> Result.Error err

do
    logR :=
        between (skipChar '{') (skipChar '}') (sepEndBy field (skipChar ','))

    innerJsonR :=
        between (skipChar '{') (skipChar '}') (sepEndBy innerField (skipChar ','))

    parameterJsonR :=
        between (skipChar '{') (skipChar '}') (sepEndBy parameterField (skipChar ','))

