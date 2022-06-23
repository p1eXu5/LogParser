module LogParser.Core.Kibana

open Nest
open System
open Elasticsearch.Net

type LogMessage =
    {
        TraceId: string
        FullMessage: string
    }


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