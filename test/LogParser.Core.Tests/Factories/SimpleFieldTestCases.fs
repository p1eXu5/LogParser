namespace LogParser.Core.Tests.Factories

open System.Collections
open NUnit.Framework
open LogParser.Core.Types
open Microsoft.Extensions.Logging
open System.Net


type SimpleFieldTestCases() =

    static member SpecialFields : IEnumerable =
        seq {
            TestCaseData(
                "\"timestamp\": \"2022-04-14T11:49:52.912Z\"",
                TechField.Timespan (Timespan.Value "2022-04-14T11:49:52.912Z")
            ).SetName("01 - special field. Timespan")

            TestCaseData(
                "\"level\": \"Debug\"",
                TechField.Level LogLevel.Debug
            ).SetName("01 - special field. Level")

            TestCaseData(
                "\"level\": \"debuG\"",
                TechField.Level LogLevel.Debug
            ).SetName("01 - special field. Level CI")

            TestCaseData(
                "\"method\": \"Post\"",
                TechField.Method "Post"
            ).SetName("01 - special field. Method")

            TestCaseData(
                "\"statusCode\": \"200\"",
                TechField.StatusCode HttpStatusCode.OK
            ).SetName("01 - special field. StatusCode")

            TestCaseData(
                "\"StatusCode\": \"200\"",
                TechField.StatusCode HttpStatusCode.OK
            ).SetName("01 - special field. StatusCode CI")

            TestCaseData(
                "\"path\": \"MassTransit\"",
                TechField.Path "MassTransit"
            ).SetName("01 - special field. Path")

            TestCaseData(
                "\"host\": \"MassTransit\"",
                TechField.Host "MassTransit"
            ).SetName("01 - special field. Host")

            TestCaseData(
                "\"port\": 5672",
                TechField.Port 5672
            ).SetName("01 - special field. Port")

            TestCaseData(
                "\"sourceContext\": \"MassTransit\"",
                TechField.SourceContext "MassTransit"
            ).SetName("01 - special field. SourceContext")

            TestCaseData(
                "\"RequestId\": \"MassTransit\"",
                TechField.RequestId "MassTransit"
            ).SetName("01 - special field. RequestId")

            TestCaseData(
                "\"RequeStPath\": \"MassTransit\"",
                TechField.RequestPath "MassTransit"
            ).SetName("01 - special field. RequestPath")

            TestCaseData(
                "\"SpanId\": \"MassTransit\"",
                TechField.SpanId "MassTransit"
            ).SetName("01 - special field. SpanId")

            TestCaseData(
                "\"TraceId\": \"MassTransit\"",
                TechField.TraceId "MassTransit"
            ).SetName("01 - special field. TraceId")

            TestCaseData(
                "\"ParentId\": \"MassTransit\"",
                TechField.ParentId "MassTransit"
            ).SetName("01 - special field. ParentId")

            TestCaseData(
                "\"ConnectionId\": \"MassTransit\"",
                TechField.ConnectionId "MassTransit"
            ).SetName("01 - special field. ConnectionId")

            TestCaseData(
                "\"HierarchicalTraceId\":\"|835674ebb32a89144396ba52f55405ef.457.c49360ab_\"",
                TechField.HierarchicalTraceId "|835674ebb32a89144396ba52f55405ef.457.c49360ab_"
            ).SetName("01 - special field. HierarchicalTraceId")

            TestCaseData(
                "\"hierarchicalTraceId\":\"|835674ebb32a89144396ba52f55405ef.457.c49360ab_\"",
                TechField.HierarchicalTraceId "|835674ebb32a89144396ba52f55405ef.457.c49360ab_"
            ).SetName("01 - special field. hierarchicalTraceId")
        }

    /// 02
    static member PrimitiveFields =
        seq {
            TestCaseData(
                "\"customString\": \"foo\"",
                TechField.String ("customString", "foo")
            ).SetName("02 - primitive field. String")

            TestCaseData(
                "\"customString\": 123-123-123",
                TechField.String ("customString", "123-123-123")
            ).SetName("02 - primitive field. String key with double quotes value with no quotes")

            TestCaseData(
                "customString: foo",
                TechField.String ("customString", "foo")
            ).SetName("02 - primitive field. String quoteless")

            TestCaseData(
                "\"customInt\": 123",
                TechField.Int ("customInt", 123)
            ).SetName("02 - primitive field. Int")

            TestCaseData(
                "customInt:123",
                TechField.Int ("customInt", 123)
            ).SetName("02 - primitive field. Int quoteless")

            TestCaseData(
                "\"customBool\": true",
                TechField.Bool ("customBool", true)
            ).SetName("02 - primitive field. Bool: true")

            TestCaseData(
                "\"customBool\": false",
                TechField.Bool ("customBool", false)
            ).SetName("02 - primitive field. Bool: false")

            TestCaseData(
                "\"foo\": null",
                TechField.Null "foo"
            ).SetName("02 - primitive field. Null")

            TestCaseData(
                "\"consumer #0\": \"foo\"",
                TechField.String ("consumer #0", "foo")
            ).SetName("02 - primitive field. Field name contains space and hash char")

            TestCaseData(
                "\"consumer, #0\": \"foo\"",
                TechField.String ("consumer, #0", "foo")
            ).SetName("02 - primitive field. Field name contains comma")

            TestCaseData(
                "    \"cvv/icvv\": \"success\"    ".Trim(),
                TechField.String ("cvv/icvv", "success")
            ).SetName("02 - primitive field. Field name contains slash char")

            TestCaseData(
                "    \"cvv/icvv\": \"success\"    ".Trim(),
                TechField.String ("cvv/icvv", "success")
            ).SetName("02 - primitive field. Field name contains slash char")

            TestCaseData(
                "    \"cvv|icvv\": \"success\"    ".Trim(),
                TechField.String ("cvv|icvv", "success")
            ).SetName("02 - primitive field. Field name contains vertical bar char")

            TestCaseData(
                "    \"?column?\": \"========== success==========\"    ".Trim(),
                TechField.String ("?column?", "========== success==========")
            ).SetName("02 - primitive field. Field name contains question marks")
        }


    static member ArrayFields =
        seq {
            TestCaseData(
                "\"scope\":[\"HTTP POST http://gate_app:5000/d/push\"]",
                TechField.Array ("scope", ["HTTP POST http://gate_app:5000/d/push"])
            ).SetName("03 - privitive field. Array")

            TestCaseData(
                "\"scope\":[3]",
                TechField.ArrayInt ("scope", [3])
            ).SetName("03 - privitive field. ArrayInt")
        }
