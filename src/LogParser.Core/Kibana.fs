module LogParser.Core.Kibana

open System

open FParsec


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
    "level",
    "fullMessage",
    "TraceId",
    "servicename",
    "container"
  ]
}}
    """




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


let searchLogs (logger: ILogger) (uri: string) (dt: DateTime option) (login: string) (password: string) (traceId: string) =
    task {
        let node = Uri(uri)
        use settings = new ConnectionSettings(node)
        settings.DefaultFieldNameInferrer(id).BasicAuthentication(login, password) |> ignore

        dt |> Option.iter(fun dt -> settings.DefaultIndex(sprintf "filebeat-%i.%i.%i" dt.Year dt.Month dt.Day) |> ignore)
        let client = ElasticClient(settings)

        let searchDescriptor = SearchDescriptor<LogMessage>("filebeat-*")

        do
            searchDescriptor
                .Query(fun q -> 
                    q.Bool(fun b ->
                        b.Filter(fun (f: QueryContainerDescriptor<LogMessage>) ->
                            f.Term(
                                field=(fun (l: LogMessage) -> l.TraceId),
                                value=(box traceId)
                            )
                        )
                    )
                )
                .Size(500)
                .Sort(fun sd -> sd.Ascending("@timestamp"))
                |> ignore

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
