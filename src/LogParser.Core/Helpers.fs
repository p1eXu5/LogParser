[<AutoOpen>]
module LogParser.Core.Helpers

open System

let equalOrdinalCI (s1: string) (s2: string) =
    s1.Equals(s2, StringComparison.OrdinalIgnoreCase)

let equalOrdinal (s1: string) (s2: string) =
    s1.Equals(s2, StringComparison.Ordinal)