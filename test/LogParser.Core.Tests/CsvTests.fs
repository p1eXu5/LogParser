namespace LogParser.Core.Tests

open NUnit.Framework
open FsUnit
open LogParser.Core

module CsvTests =

    [<Test>]
    let ``parse returns expected`` () =
        let input =
            """
"@timestamp",message,"container.name"
"Aug 30, 2023 @ 08:54:39.342","{""timestamp"":""2023-08-30T08:54:39.342Z""}","PMP_DMZ_tsm_accessgate.1.lkuzbn9efs73p9gefzhbtxi2n"
            """.Trim()

        let expected =
            """
PMP_DMZ_tsm_accessgate.1.lkuzbn9efs73p9gefzhbtxi2n {"timestamp":"2023-08-30T08:54:39.342Z"}
            """.Trim()

        let actual = Csv.parse input

        actual |> should equal expected