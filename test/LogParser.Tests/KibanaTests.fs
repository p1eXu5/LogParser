namespace LogParser.Tests

open NUnit.Framework
open FsUnit
open FsToolkit.ErrorHandling
open LogParser.Tests.ShouldExtensions

open LogParser

module KibanaTests =

    [<Test>]
    let ``fullMessage - kibana fullMessage test`` () =
        let log = """{"traceId":"9fdl11e047ace655822c5417d02843f7","timestamp":"2012-07-23T09:03:06.036Z"}"""
        let fullMessage = $"\"\"\"{log}\"\"\""

        FParsec.runResult Kibana.fullMessage fullMessage
        |> Result.shouldEqual log


    [<Test>]
    let ``parse - kibana log test`` () =
        let log = """{"traceId":"9fdl11e047ace655822c5417d02843f7","timestamp":"2012-07-23T09:03:06.036Z"}"""
        
        let kibana =

            let fm = $"\"\"\"{log}\"\"\""
            $"""
            {{
              "took" : 1073,
              "timed_out" : false,
              "_shards" : {{
                "total" : 13,
                "successful" : 13,
                "skipped" : 0,
                "failed" : 0
              }},
              "hits" : {{
                "total" : {{
                  "value" : 42,
                  "relation" : "eq"
                }},
                "max_score" : 0.0,
                "hits" : [
                  {{
                    "_index" : "filebeat-2022.06.23",
                    "_type" : "_doc",
                    "_id" : "5ifNj4EB-fw2kdSFBGZD",
                    "_score" : 0.0,
                    "_source" : {{
                      "fullMessage" : {fm},
                      "TraceId" : "9fde11e047ace615822c5417d02843f7"
                    }}
                  }},
                  {{
                    "_index" : "filebeat-2022.06.23",
                    "_type" : "_doc",
                    "_id" : "DifNj4EB-fw2kdSFBGdD",
                    "_score" : 0.0,
                    "_source" : {{
                      "fullMessage" : {fm},
                      "TraceId" : "9fde11e047ace615822c5417d02843f7"
                    }}
                  }}
                ]
              }}
            }}
            """

        Kibana.parse kibana
        |> Result.should equivalent [log; log]


    [<Test>]
    let ``parse2 - kibana2 log test`` () =
        let log = """{"traceId":"9fdl11e047ace655822c5417d02843f7","timestamp":"2012-07-23T09:03:06.036Z"}"""
        let fullMessage = $"\"\"\"{log}\"\"\""

        let expected = 
            $"""{{"fullMessage" : {fullMessage}, "TraceId" : "9fde11e047ace615822c5417d02843f7"}}""".Replace(" ", "").Replace("\r", "").Replace("\n", "")
        
        let kibana =

            let fm = $"\"\"\"{log}\"\"\""
            $"""
            {{
              "took" : 1073,
              "timed_out" : false,
              "_shards" : {{
                "total" : 13,
                "successful" : 13,
                "skipped" : 0,
                "failed" : 0
              }},
              "hits" : {{
                "total" : {{
                  "value" : 42,
                  "relation" : "eq"
                }},
                "max_score" : 0.0,
                "hits" : [
                  {{
                    "_index" : "filebeat-2022.06.23",
                    "_type" : "_doc",
                    "_id" : "5ifNj4EB-fw2kdSFBGZD",
                    "_score" : 0.0,
                    "_source" : {{
                      "fullMessage" : {fm},
                      "TraceId" : "9fde11e047ace615822c5417d02843f7"
                    }},
                    "sort" : [
                      1657601105052
                    ]
                  }},
                  {{
                    "_index" : "filebeat-2022.06.23",
                    "_type" : "_doc",
                    "_id" : "DifNj4EB-fw2kdSFBGdD",
                    "_score" : 0.0,
                    "_source" : {{
                      "fullMessage" : {fm},
                      "TraceId" : "9fde11e047ace615822c5417d02843f7"
                    }},
                    "sort" : [
                      1657601105052
                    ]
                  }}
                ]
              }}
            }}
            """

        Kibana.parse2 kibana
        |> Result.map (List.map (fun s -> s.Replace(" ", "").Replace("\r", "").Replace("\n", "")))
        |> Result.should equivalent [expected; expected]

    