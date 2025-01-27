module LogParser.Core.Kibana

open System
open FParsec

type [<Measure>] year
type [<Measure>] month
type [<Measure>] day


let fullMessage =
    between (skipString "\"\"\"") (skipString "\"\"\"") (manyCharsTill anyChar (followedBy (manyMinMaxSatisfy 3 3 ((=) '\"') )))

let kibanaOutput =
    skipCharsTillString "\"\"\"" false 2048
    >>. sepEndBy fullMessage (attempt(skipCharsTillString "\"\"\"" false 2048)) 
    .>> skipMany anyChar
    .>> eof


let fullJson =
    between 
        (skipChar '{') 
        (skipString "}," >>. unicodeSpaces1 >>. skipString @"""sort"" : [") 
        (manyCharsTill anyChar (followedBy (skipString "}," >>. unicodeSpaces1 >>. skipString @"""sort"" : [")))


let kibanaOutput2 =
    skipCharsTillString @"""_source"" : " true 2048
    >>. sepEndBy fullJson (attempt(skipCharsTillString  @"""_source"" : " true 2048)) 
    .>> skipMany anyChar
    .>> eof
    |>> (List.map (fun s -> sprintf "{%s}" (s.Trim())))
    

let parse input =
    run kibanaOutput input
    |> function
        | Success (ok,_,_) -> Result.Ok ok
        | Failure (err,_,_) -> Result.Error err


let parse2 input =
    run kibanaOutput2 input
    |> function
        | Success (ok,_,_) -> Result.Ok ok
        | Failure (err,_,_) -> Result.Error err


open Microsoft.Extensions.Logging
open Elasticsearch.Net
open Nest


type LogMessage =
    {
        TraceId: string
        [<Text(Name="fullMessage")>] FullMessage: string
        [<Text(Name="servicename")>] ServiceName: string
        [<Object(Name = "container")>] Container: Container
    }
and
    Container =
        {
            [<Text(Name="id")>] Id: string
            [<Object(Name = "image")>]Image: Image
        }
    and
        Image = 
            {
                [<Text(Name="name")>] Name: string
            }


type TraceIdMessage =
    | TraceId of string
    | Message of string

type TraceIdQueryType =
    | JsonParsed
    | Indexed

type SearchKibanaLogsParams =
    {
        KibanaBaseUri: string
        Login: string
        Password: string
        IndexDate: DateTime option
        StartTime: DateTime option
        EndTime: DateTime option
        TraceIdMessage: TraceIdMessage option
        Size: int option
        From: int option
        ServiceName: string option
        IndexPattern: string
        IndexFormat: int<year> -> int<month> -> int<day> -> string
        TraceIdQuery: TraceIdQueryType
        /// _source array. Used in searchRequest function
        SearchSource: string
    }


let private serviceNameQuery serviceName =
    Func<QueryContainerDescriptor<LogMessage>, QueryContainer>(
        fun (q: QueryContainerDescriptor<LogMessage>) ->
            q.Term(fun t -> t.Field(fun lm -> lm.ServiceName).Value(serviceName))
    )

let indexedTraceIdQuery traceId =
    Func<QueryContainerDescriptor<LogMessage>, QueryContainer>(
        fun (q: QueryContainerDescriptor<LogMessage>) ->
            q.Term(fun t -> t.Field(fun lm -> lm.TraceId).Value(traceId))
    )

let parsedJsonTraceIdQuery traceId =
    Func<QueryContainerDescriptor<LogMessage>, QueryContainer>(
        fun (q: QueryContainerDescriptor<LogMessage>) ->
            q.Term(fun t -> t.Field("parsedJson.traceId").Value(traceId))
    )

let traceIdQuery (traceIdQuery: TraceIdQueryType) =
    match traceIdQuery with
    | Indexed -> indexedTraceIdQuery
    | JsonParsed -> parsedJsonTraceIdQuery

let private messageQuery message =
    Func<QueryContainerDescriptor<LogMessage>, QueryContainer>(
        fun (q: QueryContainerDescriptor<LogMessage>) ->
            q.QueryString(fun t -> t.Query(message).Fields("message").DefaultOperator(Operator.And))
    )

let private dateRangeQuery kibanaParams =
    match kibanaParams.IndexDate, kibanaParams.StartTime, kibanaParams.EndTime with
    | Some date, Some start, Some ``end`` ->
        Func<QueryContainerDescriptor<LogMessage>, QueryContainer>(
            fun (q: QueryContainerDescriptor<LogMessage>) ->
                q.DateRange(fun dr ->
                    dr.Field("@timestamp")
                        .GreaterThanOrEquals(date.Date.Add(start.TimeOfDay))
                        .LessThanOrEquals(date.Date.Add(``end``.TimeOfDay))
                )
        )
        |> Some
    | _, _, _ -> None

let private queryBoolMust (queries: Func<QueryContainerDescriptor<LogMessage>, QueryContainer> seq) (searchDescriptor: SearchDescriptor<LogMessage>) =
     searchDescriptor
        .Query(fun q -> q.Bool(fun b -> b.Must(queries))) |> ignore

let prepareKibanaRequest (kibanaParams: SearchKibanaLogsParams) (settings: ConnectionSettings) =
    settings
        .DefaultFieldNameInferrer(id)
        // .DisableDirectStreaming()
        .BasicAuthentication(kibanaParams.Login, kibanaParams.Password)
        // .ServerCertificateValidationCallback(fun sender cert chain errors -> true)
        // .EnableApiVersioningHeader()
        |> ignore

    let defaultIndex =
        kibanaParams.IndexDate
        |> Option.map (fun dt -> kibanaParams.IndexFormat (dt.Year * 1<year>) (dt.Month * 1<month>) (dt.Day * 1<day>))
        |> Option.defaultValue kibanaParams.IndexPattern

    settings.DefaultIndex(defaultIndex) |> ignore

    let client = ElasticClient(settings)
    let searchDescriptor = SearchDescriptor<LogMessage>()

    match kibanaParams.TraceIdMessage, kibanaParams.ServiceName with
    | Some (TraceId traceId), Some serviceName ->
        searchDescriptor
        |> queryBoolMust [
            serviceNameQuery serviceName
            traceId |> traceIdQuery kibanaParams.TraceIdQuery
        ]

    | Some (Message message), Some serviceName ->
        searchDescriptor
        |> queryBoolMust [
            serviceNameQuery serviceName
            messageQuery message
        ]

    | Some (TraceId traceId), None ->
        searchDescriptor
        |> queryBoolMust [
            traceId |> traceIdQuery kibanaParams.TraceIdQuery
        ]

    | Some (Message message), None ->
        searchDescriptor
        |> queryBoolMust [
            messageQuery message
        ]

    | None, Some serviceName ->
        searchDescriptor
        |> queryBoolMust [
            serviceNameQuery serviceName
            match dateRangeQuery kibanaParams with
            | Some query -> query
            | None -> ()
        ]

    | _, _ -> ()

    match kibanaParams.From with
    | Some from ->
        searchDescriptor.From(from) |> ignore
    | _ -> ()

    match kibanaParams.Size with
    | Some size ->
        searchDescriptor.Size(size) |> ignore
    | _ -> ()

    searchDescriptor.Sort(fun sd -> sd.Field(fun fsd -> fsd.Field("@timestamp").Ascending())) |> ignore

    (client, searchDescriptor, defaultIndex)


let searchLogs (logger: ILogger) (kibanaParams: SearchKibanaLogsParams) =
    task {
        let node = Uri(kibanaParams.KibanaBaseUri)

        use settings = new ConnectionSettings(node)
        let (client, searchDescriptor, _) = settings |> prepareKibanaRequest kibanaParams

        let queryJson = 
            client
                .RequestResponseSerializer
                .SerializeToString(searchDescriptor, SerializationFormatting.Indented);

        logger.LogDebug("Sending kibana request:\n{query}", queryJson)

        let! result = client.SearchAsync<LogMessage>(searchDescriptor)

        return
            result.Documents
            |> Seq.map (fun d -> 
                $"""
                {{
                    "fullMessage": {d.FullMessage},
                    "servicename": {d.ServiceName},
                    "container": {{
                        "id": {d.Container.Id},
                        "image": {{
                            "name": {d.Container.Image.Name}
                        }}
                    }}
                }}
                """
            )
    }


let searchRequest (kibanaParams: SearchKibanaLogsParams) =
//    $"""
//GET /filebeat-*/_search
//{{
//  "query": {{
//    "bool": {{
//      "filter": [
//        {{
//          "term": {{
//            "TraceId": {{
//              "value": "{traceId}"
//            }}
//          }}
//        }}
//      ]
//    }}
//  }},
//  "size": {logCount},
//  "sort": [
//    {{
//      "@timestamp": {{
//        "order": "asc"
//      }}
//    }}
//  ],
//  "_source": [
//    "@timestamp",
//    "level",
//    "fullMessage",
//    "TraceId",
//    "servicename",
//    "container"
//  ]
//}}
//    """
    let node = Uri(kibanaParams.KibanaBaseUri)

    use settings = new ConnectionSettings(node)
    let (client, searchDescriptor, ind) = settings |> prepareKibanaRequest kibanaParams

    let queryJson =
        (client
            .RequestResponseSerializer
            .SerializeToString(searchDescriptor, SerializationFormatting.Indented))
        |> sprintf "GET /%s/_search\n%s" ind
        |> (fun s ->
            s.TrimEnd('}') + kibanaParams.SearchSource
        )

    queryJson

//do
//    System.Net.ServicePointManager.ServerCertificateValidationCallback <-
//        System.Net.Security.RemoteCertificateValidationCallback(fun sender cert chain errors -> true)