namespace LogParser.App.Tests

open System
open Microsoft.Extensions.Logging

open NUnit.Framework
open Faqt
open Faqt.Operators
open FSharp.Control.Reactive.Testing
open LogParser.Tests.Fakers
open p1eXu5.AspNetCore.Testing.Logging

open FSharp.Control.Reactive

open LogParser.App

module AppSubjectTests =

    let private collect (appSubject: AppSubject) =
        let buf = ResizeArray<Guid * LogParseMsg>()
        let sub =
            (appSubject :> IObservable<Guid * LogParseMsg>)
                .Subscribe(fun item -> buf.Add(item))
        buf, sub

    let private appSubject () = new AppSubject(TestLogger<AppSubject>(TestContextWriters.GetInstance()) :> ILogger<AppSubject>)

    let msg () = LogPosition.generate () |> LogParseMsg.LogPosition
    let errMsg () = Guid.NewGuid().ToString("N") |> LogParseMsg.ParsingError

    [<Test>]
    let ``GetObserver returns non-null observer`` () =
        let appSubject = appSubject ()
        let obs = appSubject.GetObserver(Guid.NewGuid())
        %obs.Should().NotBeNull()

    [<Test>]
    let ``Messages from a single observer reach subscribers`` () =
        let appSubject = appSubject ()
        let buf, _ = collect appSubject
        let id = Guid.CreateVersion7()
        let msg0 = msg ()
        let msg1 = msg ()

        let obs = appSubject.GetObserver(id)
        obs.OnNext(msg0)
        obs.OnNext(msg1)

        %buf.Should().HaveLength(2)
        %buf.[0].Should().Be((id, msg0))
        %buf.[1].Should().Be((id, msg1))

    [<Test>]
    let ``Multiple observers are merged into a single stream`` () =
        let appSubject = appSubject ()
        let buf, _ = collect appSubject
        let idA = Guid.CreateVersion7()
        let idB = Guid.CreateVersion7()

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

        %buf.Should().HaveLength(4)
        %buf.Should()
            .SequenceEqual(
                seq {
                    (idA, msgA0)
                    (idB, msgB0)
                    (idA, msgA1)
                    (idB, msgB1)
                }
            )

    [<Test>]
    let ``Observers requested at different times are all linked`` () =
        let appSubject = appSubject ()
        let buf, _ = collect appSubject

        let idA = Guid.NewGuid()
        let oA = appSubject.GetObserver(idA)
        let msgA0 = msg ()
        oA.OnNext(msgA0)

        // requested later
        let idB = Guid.NewGuid()
        let oB = appSubject.GetObserver(idB)
        let msgB0 = msg ()
        oB.OnNext(msgB0)

        let msgA1 = msg ()
        oA.OnNext(msgA1)

        %buf.Should()
            .SequenceEqual(
                seq {
                    (idA, msgA0)
                    (idB, msgB0)
                    (idA, msgA1)
                }
            )

    [<Test>]
    let ``OnCompleted on one observer does not complete the merged stream`` () =
        let appSubject = appSubject ()
        let buf = ResizeArray<Guid * LogParseMsg>()
        let mutable completed = false
        let _ =
            (appSubject :> IObservable<Guid * LogParseMsg>).Subscribe(
                buf.Add,
                (fun () -> completed <- true))

        let idA = Guid.NewGuid()
        let oA = appSubject.GetObserver(idA)

        let idB = Guid.NewGuid()
        let oB = appSubject.GetObserver(idB)

        let msgA = msg ()
        oA.OnNext(msgA)
        oA.OnCompleted()

        // Stream remains open — oB still works.
        let msgB = msg ()
        oB.OnNext(msgB)

        %buf.Should()
            .SequenceEqual(
                seq {
                    (idA, msgA)
                    (idB, msgB)
                }
            )
        %completed.Should().BeFalse()

    [<Test>]
    let ``After OnCompleted further OnNext from same observer is ignored`` () =
        let appSubject = appSubject ()
        let buf, _ = collect appSubject

        let idA = Guid.NewGuid()
        let oA = appSubject.GetObserver(idA)
        let msgA = msg ()
        oA.OnNext(msgA)
        oA.OnCompleted()

        // Subject<T> throws on OnNext after OnCompleted? No — it just ignores.
        // (System.Reactive Subject<T> swallows post-completion notifications.)
        let act = fun () -> oA.OnNext(msg ())

        %act.Should().ThrowExactly<ObjectDisposedException, _>()
        %buf.Should()
            .SequenceEqual(
                seq {
                    (idA, msgA)
                }
            )

    [<Test>]
    let ``ProcessingError messages are propagated as regular OnNext`` () =
        let appSubject = appSubject ()
        let buf, _ = collect appSubject

        let id = Guid.NewGuid()
        let o = appSubject.GetObserver(id)
        let errMsg = errMsg ()
        o.OnNext(errMsg)

        %buf.Should()
            .SequenceEqual(
                seq {
                    (id, errMsg)
                }
            )

    [<Test>]
    let ``Late subscriber receives only messages emitted after subscription`` () =
        let appSubject = appSubject ()
        
        let id = Guid.NewGuid()
        let o = appSubject.GetObserver(id)
        o.OnNext(msg ())   // emitted before subscription

        let buf, _ = collect appSubject
        let msg2 = msg ()
        o.OnNext(msg2)

        %buf.Should()
            .SequenceEqual(
                seq {
                    (id, msg2)
                }
            )

    [<Test>]
    let ``Many observers stress test`` () =
        let appSubject = appSubject ()
        let buf, _ = collect appSubject
        let msgs =
            [ for _ in 1..50 -> msg () ]

        let observers =
            [ for i in 1..50 -> appSubject.GetObserver(Guid.NewGuid()), i ]

        for (o, i) in observers do
            o.OnNext(msgs |> List.item (i - 1))

        %buf.Should().HaveLength(50)
        %(buf |> Seq.map snd).Should().SequenceEqual(msgs)

    [<Test>]
    let ``Completed observer is unlinked - new emissions from a fresh observer still flow`` () =
        let appSubject = appSubject ()
        let buf, _ = collect appSubject

        let id = Guid.NewGuid()
        let oA = appSubject.GetObserver(id)
        let msgA = msg ()
        oA.OnNext(msgA)
        oA.OnCompleted()

        // New observer with the same id (re-registration scenario).
        let oB = appSubject.GetObserver(id)
        let msgB = msg ()
        oB.OnNext(msgB)

        %buf.Should()
            .SequenceEqual(
                seq {
                    (id, msgA)
                    (id, msgB)
                }
            )

    [<Test>]
    let ``Disposing AppSubject completes the merged stream`` () =
        let appSubject = appSubject ()
        let mutable completed = false
        let _ =
            (appSubject :> IObservable<Guid * LogParseMsg>).Subscribe(
                (fun _ -> ()),
                (fun () -> completed <- true))

        (appSubject :> IDisposable).Dispose()
        %completed.Should().BeTrue()

    [<Test>]
    let ``Messages are observed in the order emitted across observers`` () =
        let appSubject = appSubject ()
        let scheduler = TestScheduler.create ()

        let observer = scheduler |> TestScheduler.createObserver
        let _ = (appSubject :> IObservable<Guid * LogParseMsg>).Subscribe(observer)

        let idA = Guid.NewGuid()
        let oA = appSubject.GetObserver(idA)

        let idB = Guid.NewGuid()
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
            observer.Messages
            |> Seq.choose (fun m ->
                match m.Value.Kind with
                | System.Reactive.NotificationKind.OnNext -> Some m.Value.Value
                | _ -> None)
            |> Seq.toList

        %values.Should()
            .SequenceEqual(
                seq {
                    (idA, msgA0)
                    (idB, msgB0)
                    (idB, msgB1)
                }
            )
