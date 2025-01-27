module LogParser.Core.Csv

open FSharp.Data

type CsvLog = CsvProvider<"1,2,3", Schema="@timestamp (string),message (string), container.name (string)">

let parse input =
    let csvLog = CsvLog.Parse(input)
    csvLog.Rows
    |> Seq.map (fun r ->
        (r.``Container.name``, r.Message)
        ||> sprintf "%s %s"
    )
    |> fun s -> System.String.Join("\n", s)