namespace LogParser.Core.Tests.ParserTests

open System.Collections
open System.Net

open Microsoft.Extensions.Logging
open NUnit.Framework

open LogParser.Core.Types

type PrimitiveFieldCases() =

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
