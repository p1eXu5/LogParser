module LogParser.Core.Kibana

open System
open Elasticsearch.Net



type LogMessage =
    {
        TraceId: string
        FullMessage: string
    }



open FParsec

let fullMessage =
    between (skipString "\"\"\"") (skipString "\"\"\"") (manyCharsTill anyChar (followedBy (manyMinMaxSatisfy 3 3 ((=) '\"') )))

let kibanaOutput =
    skipCharsTillString "\"\"\"" false 2048
    >>. sepEndBy fullMessage (attempt(skipCharsTillString "\"\"\"" false 2048)) 
    .>> skipMany anyChar
    .>> eof

let parse input =
    run kibanaOutput input
    |> function
        | Success (ok,_,_) -> Result.Ok ok
        | Failure (err,_,_) -> Result.Error err



let searchRequest logCount traceId =
    $"""
GET /filebeat-*/_search
{{
  "query": {{
    "bool": {{
      "filter": [
        {{
          "term": {{
            "TraceId": {{
              "value": "{traceId}"
            }}
          }}
        }}
      ]
    }}
  }},
  "size": {logCount},
  "sort": [
    {{
      "@timestamp": {{
        "order": "asc"
      }}
    }}
  ],
  "_source": [
    "@timestamp",
    "TraceId",
    "fullMessage"
  ]
}}
    """


open Nest

let searchLogs (uri: string) (dt: DateTime option) (login: string) (password: string) (traceId: string) =
    task {
        let node = Uri(uri)
        use settings = new ConnectionSettings(node)
        settings.DefaultFieldNameInferrer(id).BasicAuthentication(login, password) |> ignore

        dt |> Option.iter(fun dt -> settings.DefaultIndex(sprintf "filebeat-%i.%i.%i" dt.Year dt.Month dt.Day) |> ignore)
        let client = ElasticClient(settings)

        let searchDescriptor = SearchDescriptor<LogMessage>()

        do
            searchDescriptor.Query(fun q -> 
                q.Bool(fun b ->
                    b.Filter(fun (f: QueryContainerDescriptor<LogMessage>) ->
                        f.Term(
                            field=(fun (l: LogMessage) -> l.TraceId),
                            value=(box traceId)
                        )
                    )
                )
            )
            |> ignore

        let queryJson = 
            client
                .RequestResponseSerializer
                .SerializeToString(searchDescriptor, SerializationFormatting.Indented);

        let! result = client.SearchAsync<LogMessage>(searchDescriptor)

        return
            result.Documents
            |> Seq.map (fun d -> d.FullMessage)
    }
