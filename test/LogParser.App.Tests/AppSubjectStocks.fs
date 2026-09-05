namespace LogParser.App.Tests

open System
open System.Reactive.Concurrency
open System.Threading

open NUnit.Framework
open Faqt
open Faqt.Operators
open p1eXu5.FSharp.Reactive
open p1eXu5.FSharp.Reactive.Testing

open LogParser.App
open LogParser.Types

module AppSubjectStocks =


    [<RequireQualifiedAccess>]
    type private LogPositionSignal =
                | Next of TechLogPosition
                | Error of string
                | Completed

    [<Test>]
    let ``main subject to main switch test`` () =

        let logSourceId = LogSourceId.create ()

        let mainSubject = Subject.broadcast

        let mainSwitch : IObservable<LogSourceId * ObservableLogPosition> =
            mainSubject
            |> Observable.switch
            |> Observable.publish
            |> Observable.refCount
            |> Observable.observeOn TaskPoolScheduler.Default

        let mutable isCompleted = 0
        let _ =
            mainSwitch
            |> Observable.subscribeNext
                (fun el -> isCompleted <- isCompleted + 1)

        let mergeSignal = Subject.broadcast
        let merged =
            mergeSignal
            |> Observable.mergeInner
            |> Observable.groupByElement fst snd
            |> Observable.flatmap (fun g ->
                g
                |> Observable.bufferSpanCount (TimeSpan.FromMilliseconds(200.0)) 1
                |> Observable.filter (fun l -> not (Seq.isEmpty l))
                |> Observable.bind (fun signals ->
                    Seq.foldBack
                        (fun sg st ->
                            match sg with
                            | LogPositionSignal.Next tlp ->
                                (tlp :: (fst st), snd st)
                            | LogPositionSignal.Completed ->
                                (fst st, ObservableLogPosition.Completed |> Some)
                            | LogPositionSignal.Error err ->
                                (fst st, ObservableLogPosition.Error err |> Some)
                        ) 
                        signals
                        ([], None)
                    |> fun t ->
                        let (tlps, term) = t
                        Observable.ofSeq
                            (
                                seq {
                                    if tlps.Length > 0 then
                                        yield (g.Key, ObservableLogPosition.Next tlps)

                                    if term.IsSome then
                                        yield (g.Key, term.Value)
                                }
                            )
                )
            )

        mainSubject.OnNext(merged)

        let completeErrorSignal = Subject.broadcast
        let externalSignal = Subject.broadcast

        mergeSignal.OnNext(completeErrorSignal)
        mergeSignal.OnNext(externalSignal)

        externalSignal.OnNext((logSourceId, LogPositionSignal.Completed))
        completeErrorSignal.OnNext((logSourceId, LogPositionSignal.Completed))

        let res = SpinWait.SpinUntil(Func<bool> (fun _ -> isCompleted = 2), 5000)
        %res.Should().BeTrue()


