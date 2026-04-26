namespace LogParser.Core.Tests.ParserTests

open System.Collections
open System.Net

open Microsoft.Extensions.Logging
open NUnit.Framework

open LogParser.Core.Types

type ArrayPrimitiveFieldCases() =
    
    static member ArrayFields =
        seq {
            TestCaseData(
                "\"scope\":[\"HTTP POST http://gate_app:5000/d/push\"]",
                TechField.Array ("scope", ["HTTP POST http://gate_app:5000/d/push"])
            ).SetName("03 - privitive field. Array")

            TestCaseData(
                "\"scope\":\"[\"HTTP POST http://gate_app:5000/d/push\"]\"",
                TechField.Array ("scope", ["HTTP POST http://gate_app:5000/d/push"])
            ).SetName("03 - privitive field. Array wrapped in quotes")

            TestCaseData(
                "\"scope\":[3]",
                TechField.ArrayInt ("scope", [3])
            ).SetName("03 - privitive field. ArrayInt")
        }