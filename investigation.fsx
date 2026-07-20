#r "nuget: TrieNet, 1.0.3.26316"

#if INTERACTIVE
#else
module Investigation
#endif

open Gma.DataStructures.StringSearch

let fields = UkkonenTrie<int>()
fields.Add("Охуеть, лог!!!", 1)
fields.Retrieve("лог") |> printfn "%A"

fields.Add("Охуеть, ещё один лог!!!", 2)
fields.Retrieve("лог") |> printfn "%A"
