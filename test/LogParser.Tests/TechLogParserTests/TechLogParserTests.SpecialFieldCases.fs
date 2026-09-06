namespace LogParser.Tests.TechLogParserTests

open System.Collections
open System.Net

open Microsoft.Extensions.Logging
open NUnit.Framework

open LogParser.Types

type SpecialFieldCases() =

    static member SpecialFields : IEnumerable =
        seq {
            TestCaseData(
                "\"timestamp\": \"2022-04-14T11:49:52.912Z\"",
                TechJsonLogField.Timestamp (Timespan.Value "2022-04-14T11:49:52.912Z")
            ).SetName("01 - special field. Timespan")

            TestCaseData(
                "\"level\": \"Debug\"",
                TechJsonLogField.Level LogLevel.Debug
            ).SetName("01 - special field. Level")

            TestCaseData(
                "\"level\": \"debuG\"",
                TechJsonLogField.Level LogLevel.Debug
            ).SetName("01 - special field. Level CI")

            TestCaseData(
                "\"method\": \"Post\"",
                TechJsonLogField.Method "Post"
            ).SetName("01 - special field. Method")

            TestCaseData(
                "\"statusCode\": \"200\"",
                TechJsonLogField.StatusCode HttpStatusCode.OK
            ).SetName("01 - special field. StatusCode")

            TestCaseData(
                "\"StatusCode\": \"200\"",
                TechJsonLogField.StatusCode HttpStatusCode.OK
            ).SetName("01 - special field. StatusCode CI")

            TestCaseData(
                "\"path\": \"MassTransit\"",
                TechJsonLogField.Path "MassTransit"
            ).SetName("01 - special field. Path")

            TestCaseData(
                "\"host\": \"MassTransit\"",
                TechJsonLogField.Host "MassTransit"
            ).SetName("01 - special field. Host")

            TestCaseData(
                "\"port\": 5672",
                TechJsonLogField.Port 5672
            ).SetName("01 - special field. Port")

            TestCaseData(
                "\"sourceContext\": \"MassTransit\"",
                TechJsonLogField.SourceContext "MassTransit"
            ).SetName("01 - special field. SourceContext")

            TestCaseData(
                "\"RequestId\": \"MassTransit\"",
                TechJsonLogField.RequestId "MassTransit"
            ).SetName("01 - special field. RequestId")

            TestCaseData(
                "\"RequeStPath\": \"MassTransit\"",
                TechJsonLogField.RequestPath "MassTransit"
            ).SetName("01 - special field. RequestPath")

            TestCaseData(
                "\"SpanId\": \"MassTransit\"",
                TechJsonLogField.SpanId "MassTransit"
            ).SetName("01 - special field. SpanId")

            TestCaseData(
                "\"TraceId\": \"MassTransit\"",
                TechJsonLogField.TraceId "MassTransit"
            ).SetName("01 - special field. TraceId")

            TestCaseData(
                "\"ParentId\": \"MassTransit\"",
                TechJsonLogField.ParentId "MassTransit"
            ).SetName("01 - special field. ParentId")

            TestCaseData(
                "\"ConnectionId\": \"MassTransit\"",
                TechJsonLogField.ConnectionId "MassTransit"
            ).SetName("01 - special field. ConnectionId")

            TestCaseData(
                "\"HierarchicalTraceId\":\"|835674ebb32a89144396ba52f55405ef.457.c49360ab_\"",
                TechJsonLogField.HierarchicalTraceId "|835674ebb32a89144396ba52f55405ef.457.c49360ab_"
            ).SetName("01 - special field. HierarchicalTraceId")

            TestCaseData(
                "\"hierarchicalTraceId\":\"|835674ebb32a89144396ba52f55405ef.457.c49360ab_\"",
                TechJsonLogField.HierarchicalTraceId "|835674ebb32a89144396ba52f55405ef.457.c49360ab_"
            ).SetName("01 - special field. hierarchicalTraceId")
        }