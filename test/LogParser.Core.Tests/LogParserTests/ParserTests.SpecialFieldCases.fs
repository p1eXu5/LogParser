namespace LogParser.Core.Tests.ParserTests

open System.Collections
open System.Net

open Microsoft.Extensions.Logging
open NUnit.Framework

open LogParser.Core.Types

type SpecialFieldCases() =

    static member SpecialFields : IEnumerable =
        seq {
            TestCaseData(
                "\"timestamp\": \"2022-04-14T11:49:52.912Z\"",
                TechJsonField.Timespan (Timespan.Value "2022-04-14T11:49:52.912Z")
            ).SetName("01 - special field. Timespan")

            TestCaseData(
                "\"level\": \"Debug\"",
                TechJsonField.Level LogLevel.Debug
            ).SetName("01 - special field. Level")

            TestCaseData(
                "\"level\": \"debuG\"",
                TechJsonField.Level LogLevel.Debug
            ).SetName("01 - special field. Level CI")

            TestCaseData(
                "\"method\": \"Post\"",
                TechJsonField.Method "Post"
            ).SetName("01 - special field. Method")

            TestCaseData(
                "\"statusCode\": \"200\"",
                TechJsonField.StatusCode HttpStatusCode.OK
            ).SetName("01 - special field. StatusCode")

            TestCaseData(
                "\"StatusCode\": \"200\"",
                TechJsonField.StatusCode HttpStatusCode.OK
            ).SetName("01 - special field. StatusCode CI")

            TestCaseData(
                "\"path\": \"MassTransit\"",
                TechJsonField.Path "MassTransit"
            ).SetName("01 - special field. Path")

            TestCaseData(
                "\"host\": \"MassTransit\"",
                TechJsonField.Host "MassTransit"
            ).SetName("01 - special field. Host")

            TestCaseData(
                "\"port\": 5672",
                TechJsonField.Port 5672
            ).SetName("01 - special field. Port")

            TestCaseData(
                "\"sourceContext\": \"MassTransit\"",
                TechJsonField.SourceContext "MassTransit"
            ).SetName("01 - special field. SourceContext")

            TestCaseData(
                "\"RequestId\": \"MassTransit\"",
                TechJsonField.RequestId "MassTransit"
            ).SetName("01 - special field. RequestId")

            TestCaseData(
                "\"RequeStPath\": \"MassTransit\"",
                TechJsonField.RequestPath "MassTransit"
            ).SetName("01 - special field. RequestPath")

            TestCaseData(
                "\"SpanId\": \"MassTransit\"",
                TechJsonField.SpanId "MassTransit"
            ).SetName("01 - special field. SpanId")

            TestCaseData(
                "\"TraceId\": \"MassTransit\"",
                TechJsonField.TraceId "MassTransit"
            ).SetName("01 - special field. TraceId")

            TestCaseData(
                "\"ParentId\": \"MassTransit\"",
                TechJsonField.ParentId "MassTransit"
            ).SetName("01 - special field. ParentId")

            TestCaseData(
                "\"ConnectionId\": \"MassTransit\"",
                TechJsonField.ConnectionId "MassTransit"
            ).SetName("01 - special field. ConnectionId")

            TestCaseData(
                "\"HierarchicalTraceId\":\"|835674ebb32a89144396ba52f55405ef.457.c49360ab_\"",
                TechJsonField.HierarchicalTraceId "|835674ebb32a89144396ba52f55405ef.457.c49360ab_"
            ).SetName("01 - special field. HierarchicalTraceId")

            TestCaseData(
                "\"hierarchicalTraceId\":\"|835674ebb32a89144396ba52f55405ef.457.c49360ab_\"",
                TechJsonField.HierarchicalTraceId "|835674ebb32a89144396ba52f55405ef.457.c49360ab_"
            ).SetName("01 - special field. hierarchicalTraceId")
        }