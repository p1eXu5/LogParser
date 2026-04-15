module LogParser.ElmishApp.Helpers

open System
open System.Text.RegularExpressions
open System.Globalization

let notEmpty s = not (String.IsNullOrWhiteSpace(s))

let decodeUnicodeEscapes input =
    // Pattern matches literal \u followed by 4 hex digits
    let pattern1 = @"\\\\u([a-fA-F0-9]{4})"
    let pattern2 = @"\\u([a-fA-F0-9]{4})"
    
    let evaluator = MatchEvaluator(fun m ->
        // Extract the 4-digit hex code from the first capturing group
        let hexCode = m.Groups.[1].Value
        
        // Parse hex to integer, then cast to char and convert to string
        let charCode = Int32.Parse(hexCode, NumberStyles.HexNumber)
        char charCode |> string
    )

    Regex.Replace(input, pattern1, evaluator)
    |> fun res -> Regex.Replace(res, pattern2, evaluator)