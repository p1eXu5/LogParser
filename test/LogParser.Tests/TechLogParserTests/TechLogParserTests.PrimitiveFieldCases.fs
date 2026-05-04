namespace LogParser.Tests.TechLogParserTests

open System.Collections
open System.Net

open Microsoft.Extensions.Logging
open NUnit.Framework

open LogParser.Types

type PrimitiveFieldCases() =

    /// 02
    static member PrimitiveFields =
        seq {
            TestCaseData(
                "\"customString\": \"foo\"",
                TechJsonLogField.String ("customString", "foo")
            ).SetName("02 - primitive field. String")

            TestCaseData(
                "\"customString\": 123-123-123",
                TechJsonLogField.String ("customString", "123-123-123")
            ).SetName("02 - primitive field. String key with double quotes value with no quotes")

            TestCaseData(
                "customString: foo",
                TechJsonLogField.String ("customString", "foo")
            ).SetName("02 - primitive field. String quoteless")

            TestCaseData(
                "\"customInt\": 123",
                TechJsonLogField.Int ("customInt", 123)
            ).SetName("02 - primitive field. Int")

            TestCaseData(
                "customInt:123",
                TechJsonLogField.Int ("customInt", 123)
            ).SetName("02 - primitive field. Int quoteless")

            TestCaseData(
                "\"customBool\": true",
                TechJsonLogField.Bool ("customBool", true)
            ).SetName("02 - primitive field. Bool: true")

            TestCaseData(
                "\"customBool\": false",
                TechJsonLogField.Bool ("customBool", false)
            ).SetName("02 - primitive field. Bool: false")

            TestCaseData(
                "\"foo\": null",
                TechJsonLogField.Null "foo"
            ).SetName("02 - primitive field. Null")

            TestCaseData(
                "\"consumer #0\": \"foo\"",
                TechJsonLogField.String ("consumer #0", "foo")
            ).SetName("02 - primitive field. Field name contains space and hash char")

            TestCaseData(
                "\"consumer, #0\": \"foo\"",
                TechJsonLogField.String ("consumer, #0", "foo")
            ).SetName("02 - primitive field. Field name contains comma")

            TestCaseData(
                "    \"cvv/icvv\": \"success\"    ".Trim(),
                TechJsonLogField.String ("cvv/icvv", "success")
            ).SetName("02 - primitive field. Field name contains slash char")

            TestCaseData(
                "    \"cvv/icvv\": \"success\"    ".Trim(),
                TechJsonLogField.String ("cvv/icvv", "success")
            ).SetName("02 - primitive field. Field name contains slash char")

            TestCaseData(
                "    \"cvv|icvv\": \"success\"    ".Trim(),
                TechJsonLogField.String ("cvv|icvv", "success")
            ).SetName("02 - primitive field. Field name contains vertical bar char")

            TestCaseData(
                "    \"?column?\": \"========== success==========\"    ".Trim(),
                TechJsonLogField.String ("?column?", "========== success==========")
            ).SetName("02 - primitive field. Field name contains question marks")
        }
