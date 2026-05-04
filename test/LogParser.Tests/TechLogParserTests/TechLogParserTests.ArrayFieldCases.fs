namespace LogParser.Tests.TechLogParserTests

open System.Collections
open System.Net

open Microsoft.Extensions.Logging
open NUnit.Framework

open LogParser.Types

type ArrayPrimitiveFieldCases() =
    
    static member ArrayFields =
        seq {
            TestCaseData(
                "\"scope\":[\"HTTP POST http://gate_app:5000/d/push\"]",
                TechJsonLogField.Array ("scope", ["HTTP POST http://gate_app:5000/d/push"])
            ).SetName("03 - privitive field. Array")

            TestCaseData(
                "\"scope\":\"[\"HTTP POST http://gate_app:5000/d/push\"]\"",
                TechJsonLogField.Array ("scope", ["HTTP POST http://gate_app:5000/d/push"])
            ).SetName("03 - privitive field. Array wrapped in quotes")

            TestCaseData(
                "\"scope\":[3]",
                TechJsonLogField.ArrayInt ("scope", [3])
            ).SetName("03 - privitive field. ArrayInt")
        }