namespace LogParser.App

open System
open System.Collections.Generic
open System.Threading
open Microsoft.Extensions.Logging
open FSharp.Control.Reactive

type IAppSubject =
    interface
        inherit IObservable<Guid * LogParseMsg>
        inherit IDisposable
        abstract GetObserver: Guid -> IObserver<LogParseMsg>
    end

/// AppSubject merges messages from many dynamically-created IObserver<Foo> 
/// instances into a single IObservable<Foo> stream.
type AppSubject(logger: ILogger<AppSubject>) =
    // Outer subject pushes "streams of Foo" (one per requested observer).
    // Merge flattens them into a single IObservable<Foo>.
    // Use ReplaySubject so late subscribers receive inner observables added before their subscription.
    let outer = new System.Reactive.Subjects.ReplaySubject<IObservable<Guid * LogParseMsg>>()

    // Track inner subjects so we can dispose/remove them on OnCompleted.
    let inners = Dictionary<Guid, System.Reactive.Subjects.Subject<LogParseMsg>>()
    let _lock = Lock()

    // The merged stream — this is what Elmish.Wpf subscribes to.
    let merged : IObservable<Guid * LogParseMsg> = outer |> Observable.mergeInner

    /// Create and register a new IObserver<Foo> identified by `id`.
    /// The returned observer is linked to the main IObservable.
    /// When OnCompleted is called the link is removed automatically.
    member _.GetObserver(id: Guid) : IObserver<LogParseMsg> =
        let inner = Subject.broadcast

        lock _lock (fun () ->
            inners.[id] <- inner
        )

        // When inner completes, remove it from the dictionary.
        let _ =
            inner.Subscribe(
                (fun _ -> ()),                        // OnNext (ignored here)
                (fun (ex: exn) ->
                    logger.LogError(ex, "{Id} observable error", id)
                    lock _lock (fun () ->
                        match inners.TryGetValue(id) with
                        | true, s ->
                            inners.Remove(id) |> ignore
                            s.Dispose()
                        | _ -> ()
                    )
                ),                 // OnError (Processor never calls)
                (fun () ->                            // OnCompleted
                    lock _lock (fun () ->
                        match inners.TryGetValue(id) with
                        | true, s ->
                            inners.Remove(id) |> ignore
                            s.Dispose()
                        | _ -> ()
                    )
                )
            )
            |> ignore

        // Push this inner observable into the outer stream so Merge picks it up.
        outer.OnNext(inner :> IObservable<LogParseMsg> |> Observable.map (fun msg -> id, msg))
        inner :> IObserver<LogParseMsg>

    interface IObservable<Guid * LogParseMsg> with
        member _.Subscribe(observer) =
            merged.Subscribe(observer)

    interface IDisposable with
        member _.Dispose() =
            lock _lock (fun () ->
                for KeyValue(_, s) in inners do s.Dispose()
                inners.Clear())
            outer.OnCompleted()
            outer.Dispose()

    interface IAppSubject with
        member this.GetObserver(id: Guid) : IObserver<LogParseMsg> =
            this.GetObserver(id)