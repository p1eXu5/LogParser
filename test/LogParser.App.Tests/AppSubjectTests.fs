namespace LogParser.App.Tests

open System
open System.Threading
open System.Threading.Tasks

open NUnit.Framework
open Faqt
open Faqt.Operators
open p1eXu5.FSharp.Reactive
open p1eXu5.FSharp.Reactive.Testing

open LogParser.App
open LogParser.Types
open LogParser.Tests.Fakers

module AppSubjectTests =
    let private appSubject (parserSubscriptionBatchSize: int) =
        let appConfig =
            {
                ParserSubscriptionBatchSize = parserSubscriptionBatchSize
                ParserBatchFlushTimeSpan = TimeSpan.FromMilliseconds(200.0)
            }
        AppSubject.init AppSubjectLogger.Console appConfig

    let private createBufAndSubscribe (appSubject: AppSubject) =
        let buf = ResizeArray<LogSourceId * TechLogPosition list>()
        let sub =
            appSubject
            :> IObservable<LogSourceId * TechLogPosition seq>
            |> Observable.subscribeNext
                (fun (id, positions) ->
                    let now = DateTimeOffset.Now.ToString("HH':'mm':'ss.fffff")
                    printfn "[%s] LogParser.App.Tests.AppSubjectTests\n\tLogPosition seq for %A received (Thread #%i)" now id Thread.CurrentThread.ManagedThreadId
                    buf.Add((id, Seq.toList positions))
                )
        buf, sub

    let private msg () = LogPosition.generate ()
    let private errMsg () = Guid.NewGuid().ToString("N") |> LogParseMsg.ParsingError

    let spinWait buffCount (buf: ResizeArray<LogSourceId * TechLogPosition list>) =
        let res = SpinWait.SpinUntil(Func<bool> (fun _ -> buf.Count = buffCount), 5000)
        %res.Should().BeTrue()

    let spinWaitItems (lastItemsCounts: int list) (buf: ResizeArray<LogSourceId * TechLogPosition list>) =
        let res = SpinWait.SpinUntil(Func<bool> (fun _ -> buf |> Seq.map (snd >> _.Length) |> Seq.toList |> List.sort |> (=) (lastItemsCounts |> List.sort)), 5000)
        %res.Should().BeTrue()

    [<SetUp>]
    let logTestThread () =
        let now = DateTimeOffset.Now.ToString("HH':'mm':'ss.fffff")
        printfn "[%s] LogParser.App.Tests.AppSubjectTests\n\tStarting test (Thread #%i)" now Thread.CurrentThread.ManagedThreadId

    [<Test>]
    let ``01: GetObserver returns non-null observer`` () =
        let appSubject = appSubject 1
        let obs = appSubject.GetObserver(LogSourceId.create ())
        %obs.Should().NotBeNull()

    [<Test>]
    let ``0x: OnCompleted test`` () =
        let appSubject = appSubject 1
        let mutable isCompleted = false
        let _ =
            appSubject.Observable
                |> Observable.subscribeNext (fun (_ , p) ->
                    match p with
                    | ObservableLogPosition.Completed ->
                        isCompleted <- true
                    | _ -> ()
                )
        let logSourceId = LogSourceId.create ()
        let obs = appSubject.GetObserver logSourceId

        appSubject.SendCompleteToInner logSourceId

        let res = SpinWait.SpinUntil(Func<bool> (fun _ -> isCompleted = true), 5000)

        %res.Should().BeTrue()

    [<Test>]
    let ``02: Messages from a single observer reach subscribers`` () =
        let appSubject = appSubject 2
        let buf, _ = createBufAndSubscribe appSubject
        let id = LogSourceId.create ()
        let msg0 = msg ()
        let msg1 = msg ()

        let obs = appSubject.GetObserver(id)
        obs.OnNext(msg0)
        obs.OnNext(msg1)

        buf |> spinWait 1

        %buf.Should().HaveLength(1)
        %buf[0].Should().Be((id, [ msg0; msg1 ]))

    [<Test>]
    let ``03: Multiple observers are merged into a single stream`` () =
        let appSubject = appSubject 2
        let buf, _ = createBufAndSubscribe appSubject
        let idA = LogSourceId.create ()
        let idB = LogSourceId.create ()

        let oA = appSubject.GetObserver(idA)
        let oB = appSubject.GetObserver(idB)

        let msgA0 = msg ()
        let msgA1 = msg ()
        let msgB0 = msg ()
        let msgB1 = msg ()

        oA.OnNext(msgA0)
        oB.OnNext(msgB0)
        oA.OnNext(msgA1)
        oB.OnNext(msgB1)

        buf |> spinWait 2

        %buf.Should().HaveLength(2)
        %buf.Should()
            .SequenceEqual(
                seq {
                    (idA, [ msgA0; msgA1 ])
                    (idB, [ msgB0; msgB1 ])
                }
            )

    [<Test>]
    let ``04: Observers requested at different times are all linked`` () =
        let appSubject = appSubject 2
        let buf, _ = createBufAndSubscribe appSubject

        let idA = LogSourceId.create ()
        let oA = appSubject.GetObserver(idA)
        let msgA0 = msg ()
        oA.OnNext(msgA0)

        // requested later
        let idB = LogSourceId.create ()
        let oB = appSubject.GetObserver(idB)
        let msgB0 = msg ()
        oB.OnNext(msgB0)

        let msgA1 = msg ()
        oA.OnNext(msgA1)

        let msgB1 = msg ()
        oB.OnNext(msgB1)

        buf |> spinWaitItems [2; 2]

        %buf.Should()
            .SequenceEqual(
                seq {
                    (idA, [ msgA0; msgA1 ])
                    (idB, [ msgB0; msgB1 ])
                }
            )

    [<Test>]
    let ``05: OnCompleted on one observer does not complete the merged stream`` () =
        let appSubject = appSubject 1
        let buf = ResizeArray<LogSourceId * TechLogPosition list>()
        let mutable completed = false
        let _ =
            appSubject
            :> IObservable<LogSourceId * TechLogPosition seq>
            |> Observable.subscribeNext
                (fun (id, positions) -> buf.Add((id, Seq.toList positions)))

        let idA = LogSourceId.create ()
        let oA = appSubject.GetObserver(idA)

        let idB = LogSourceId.create ()
        let oB = appSubject.GetObserver(idB)

        let msgA = msg ()
        oA.OnNext(msgA)
        oA.OnCompleted()

        // Stream remains open — oB still works.
        let msgB = msg ()
        oB.OnNext(msgB)

        buf |> spinWait 2

        %buf.Should()
            .SequenceEqual(
                seq {
                    (idA, [ msgA ])
                    (idB, [ msgB ])
                }
            )
        %completed.Should().BeFalse()

    [<Test>]
    let ``06: After OnCompleted further OnNext from same observer is ignored`` () =
        let appSubject = appSubject 1
        let buf, _ = createBufAndSubscribe appSubject

        let idA = LogSourceId.create ()
        let oA = appSubject.GetObserver(idA)
        let msgA = msg ()
        oA.OnNext(msgA)
        oA.OnCompleted()

        // Subject<T> throws on OnNext after OnCompleted? No — it just ignores.
        // (System.Reactive Subject<T> swallows post-completion notifications.)
        let act = fun () -> oA.OnNext(msg ())

        %act.Should().ThrowExactly<ObjectDisposedException, _>()
        %buf.Should().HaveLength(1)
        %(fst buf[0]).Should().Be(idA)
        %(snd buf[0]).Should().SequenceEqual(seq { msgA })
        %buf.Should()
            .SequenceEqual(
                [
                    (idA, [ msgA ] )
                ]
            )

    //[<Test>]
    //let ``ProcessingError messages are propagated as regular OnNext`` () =
    //    let appSubject = appSubject 1
    //    let buf, _ = createBufAndSubscribe appSubject

    //    let id = LogSourceId.create ()
    //    let o = appSubject.GetObserver(id)
    //    let errMsg = errMsg ()
    //    o.OnNext(errMsg)

    //    %buf.Should()
    //        .SequenceEqual(
    //            seq {
    //                (id, errMsg)
    //            }
    //        )
    

    [<Test>]
    let ``07: Many observers stress test`` () =
        let appSubject = appSubject 1
        let buf, _ = createBufAndSubscribe appSubject
        let msgs =
            [ for _ in 1..50 -> msg () ]

        let observers =
            [ for i in 1..50 -> appSubject.GetObserver(LogSourceId.create ()), i ]

        for (o, i) in observers do
            o.OnNext(msgs |> List.item (i - 1))

        buf |> spinWait 50

        %buf.Should().HaveLength(50)
        %(buf |> Seq.map snd |> Seq.concat).Should().SequenceEqual(msgs)

    [<Test>]
    let ``08: Completed observer is unlinked - new emissions from a fresh observer still flow`` () =
        let appSubject = appSubject 1
        let buf, _ = createBufAndSubscribe appSubject

        let id = LogSourceId.create ()
        let oA = appSubject.GetObserver(id)
        let msgA = msg ()
        oA.OnNext(msgA)
        oA.OnCompleted()

        // New observer with the same id (re-registration scenario).
        let oB = appSubject.GetObserver(id)
        let msgB = msg ()
        oB.OnNext(msgB)

        buf |> spinWaitItems [1; 1]

        %buf.Should()
            .SequenceEqual(
                seq {
                    (id, [ msgA ])
                    (id, [ msgB ])
                }
            )

    [<Test>]
    let ``09: Disposing AppSubject completes the merged stream`` () =
        let appSubject = appSubject 1
        let mutable completed = false
        let _ =
            appSubject
            :> IObservable<LogSourceId * TechLogPosition seq>
            |> Observable.subscribeCompleted
                (fun () -> completed <- true)

        (appSubject :> IDisposable).Dispose()
        %completed.Should().BeTrue()

    [<Test>]
    let ``10: Messages are observed in the order emitted across observers`` () =
        // Если поставить размер буфера 2, то после oA.OnCompleted() нужно подождать,
        // пока Msg.FinishObserver не обработается агентом, чтобы буфер флушнулся
        let appSubject = appSubject 1
        let scheduler = TestScheduler.create ()

        let testObserver = scheduler |> TestScheduler.createObserver
        let _ = 
            appSubject
            :> IObservable<LogSourceId * TechLogPosition seq>
            |> Observable.subscribeObserver(testObserver)

        let idA = LogSourceId.create ()
        let oA = appSubject.GetObserver(idA)

        let idB = LogSourceId.create ()
        let oB = appSubject.GetObserver(idB)

        let msgA0 = msg ()
        scheduler |> Schedule.actionSpanTicks 10L (fun () -> oA.OnNext(msgA0)) |> ignore

        let msgB0 = msg ()
        scheduler |> Schedule.actionSpanTicks 20L (fun () -> oB.OnNext(msgB0)) |> ignore

        scheduler |> Schedule.actionSpanTicks 30L (fun () -> oA.OnCompleted()) |> ignore

        let msgB1 = msg ()
        scheduler |> Schedule.actionSpanTicks 40L (fun () -> oB.OnNext(msgB1)) |> ignore

        scheduler.Start()

        let values =
            testObserver.Messages
            |> Seq.choose (fun m ->
                match m.Value.Kind with
                | System.Reactive.NotificationKind.OnNext -> Some (fst m.Value.Value, snd m.Value.Value |> Seq.toList)
                | _ -> None)
            |> Seq.toList

        %values.Should()
            .HaveSameItemsAs(
                seq { 
                    (idA, [ msgA0 ])
                    (idB, [ msgB0 ])
                    (idB, [ msgB1 ])
                }
            )

    [<Test>]
    let ``10: Messages are observed in the order emitted across observers with buff count greater than 1`` () =
        // Если поставить размер буфера 2, то после oA.OnCompleted() нужно подождать,
        // пока Msg.FinishObserver не обработается агентом, чтобы буфер флушнулся
        let appSubject = appSubject 50
        let buf, _ = createBufAndSubscribe appSubject

        let scheduler = TestScheduler.create ()

        let testObserver = scheduler |> TestScheduler.createObserver
        let _ = 
            appSubject
            :> IObservable<LogSourceId * TechLogPosition seq>
            |> Observable.subscribeObserver(testObserver)

        let idA = LogSourceId.create ()
        let oA = appSubject.GetObserver(idA)

        let idB = LogSourceId.create ()
        let oB = appSubject.GetObserver(idB)

        let msgA0 = msg ()
        scheduler |> Schedule.actionSpanTicks 10L (fun () -> oA.OnNext(msgA0)) |> ignore

        let msgB0 = msg ()
        scheduler |> Schedule.actionSpanTicks 20L (fun () -> oB.OnNext(msgB0)) |> ignore

        scheduler |> Schedule.actionSpanTicks 30L (fun () -> oA.OnCompleted()) |> ignore

        let msgB1 = msg ()
        scheduler |> Schedule.actionSpanTicks 40L (fun () -> oB.OnNext(msgB1)) |> ignore

        scheduler |> Schedule.actionSpanTicks 50L (fun () -> oB.OnCompleted()) |> ignore

        scheduler.Start()

        buf |> spinWaitItems [1; 2]

        let values =
            testObserver.Messages
            |> Seq.choose (fun m ->
                match m.Value.Kind with
                | System.Reactive.NotificationKind.OnNext -> Some (fst m.Value.Value, snd m.Value.Value |> Seq.toList)
                | _ -> None)
            |> Seq.toList

        %values.Should()
            .HaveSameItemsAs(
                seq { 
                    (idA, [ msgA0 ])
                    (idB, [ msgB0; msgB1 ])
                }
            )
