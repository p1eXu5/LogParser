namespace LogParser.App

open System
open System.Reactive.Concurrency
open System.Threading

open p1eXu5.FSharp.Reactive

open LogParser.Types

type AppSubject =
    {
        GetObserver: LogSourceId -> IObserver<TechLogPosition>
        Observable: IObservable<LogSourceId * ObservableLogPosition>
        Dispose: unit -> unit
        // test purpose
        SendCompleteToInner: LogSourceId -> unit
    }
    interface IDisposable with
        member this.Dispose() =
            this.Dispose()
    interface IObservable<LogSourceId * TechLogPosition seq> with
        member this.Subscribe (observer: IObserver<LogSourceId * TechLogPosition seq>): IDisposable = 
            this.Observable
            |> Observable.choose (fun (id, lp) -> lp |> function ObservableLogPosition.Next s -> (id, s) |> Some | _ -> None)
            |> Observable.subscribeObserver observer
    interface IObservable<LogSourceId * ObservableLogPosition> with
        member this.Subscribe (observer: IObserver<LogSourceId * ObservableLogPosition>): IDisposable = 
            this.Observable
            |> Observable.subscribeObserver observer
and
    [<RequireQualifiedAccess>]
    ObservableLogPosition =
        | Next of TechLogPosition seq
        | Error of string
        | Completed
and
    OnNextf = LogSourceId * ObservableLogPosition -> unit
and
    OnErrorf = unit -> unit
and
    OnCompletedf = unit -> unit
and
    AppSubjectLogger =
        {
            LogRequestingObserver: LogSourceId -> unit
            LogObserverObtained: LogSourceId -> unit
            LogInnerObserverExists: LogSourceId -> unit
            LogInnerObserverCreated: LogSourceId -> unit
            LogObserverNext: LogSourceId -> TechLogPosition -> unit
            LogObserverError: LogSourceId -> exn -> unit
            LogObserverCompleted: LogSourceId -> unit
            LogSwitchingToIteration: int -> unit
            LogSourceIsDisposing: LogSourceId -> unit
            LogSourceIsDisposed: LogSourceId -> unit
            LogSourceIsDisposedWithMerged: LogSourceId -> unit
        }
        with
            static member Console =
                let now () = DateTimeOffset.Now.ToString("HH':'mm':'ss.fffff")
                let locker = Lock()

                let log fmt =
                    Printf.kprintf (fun msg ->
                        lock locker (fun () ->
                            printfn "[%s] LogParser.App.AppSubject\n\t%s (Thread #%i)."
                                (now ())
                                msg
                                Thread.CurrentThread.ManagedThreadId)
                    ) fmt
                {
                    LogRequestingObserver = log "Requesting: %A"
                    LogObserverObtained = log "Observer is obtained for the log source '%A'"
                    LogInnerObserverExists = log "Inner observer exists: %A"
                    LogInnerObserverCreated = log "Inner observer has been created for log source '%A'"
                    LogObserverNext = fun id l -> log "Next: %A.\n\t%O" id l.Log
                    LogObserverError = log "Error: %A - %A"
                    LogObserverCompleted = log "Completed: %A"
                    LogSwitchingToIteration = log "Switching to the %i iteration"
                    LogSourceIsDisposing = log "Disposing: %A"
                    LogSourceIsDisposed = log "Disposed: %A"
                    LogSourceIsDisposedWithMerged = log "Disposed with merged: %A"
                }

module AppSubject =

    type private State =
        {
            Observers: Map<LogSourceId, (System.Reactive.Subjects.Subject<TechLogPosition> * IDisposable)>
            Merged: System.Reactive.Subjects.Subject<IObservable<LogSourceId * LogPositionSignal>>
            CompleteErrorSignal: System.Reactive.Subjects.Subject<LogSourceId * LogPositionSignal>
            Iteration: int
        }
        static member Init =
            {
                Observers = Map.empty;
                Merged = Subject.broadcast;
                CompleteErrorSignal = Subject.broadcast
                Iteration = 0
            }
    and
        [<RequireQualifiedAccess>]
        private LogPositionSignal =
            | Next of TechLogPosition
            | Error of string
            | Completed

    type private Msg =
        | GetObserver of LogSourceId * mainObserver: IObserver<IObservable<LogSourceId * ObservableLogPosition>> * AsyncReplyChannel<IObserver<TechLogPosition>>
        | FinishObserver of LogSourceId * error: string option
        | Dispose of AsyncReplyChannel<unit>
        | SendCompleteToInner of LogSourceId

    let private agent (logger: AppSubjectLogger) (appConfig: AppConfig) = new MailboxProcessor<Msg>(fun mailbox ->
        let rec loop state =
            async {
                let! msg = mailbox.Receive()
                match msg with
                | Msg.GetObserver (logSourceId, mainObserver, reply) ->
                    match state.Observers |> Map.tryFind logSourceId with
                    | Some (inner, _) ->
                        logger.LogInnerObserverExists logSourceId
                        reply.Reply(inner :> IObserver<TechLogPosition>)
                        return! loop state
                    | None ->
                        let externalSignal = Subject.broadcast
                        let d =
                            externalSignal
                            |> Observable.subscribeSafeWithCallbacks
                                (fun techLogPosition ->
                                    logger.LogObserverNext logSourceId techLogPosition
                                )
                                (fun ex -> 
                                    logger.LogObserverError logSourceId ex
                                    mailbox.Post(Msg.FinishObserver (logSourceId, (ex.Message |> Some)))
                                )
                                (fun () ->
                                    logger.LogObserverCompleted logSourceId
                                    mailbox.Post(Msg.FinishObserver (logSourceId, None))
                                )

                        let iteration =
                            if state.Observers.IsEmpty then
                                // If first observer request:
                                // 1. create 'merged' from state.Merged
                                // 2. push 'merged' to main observer (switch)
                                // 3. push new inner to the state.Merged
                                let merged =
                                    state.Merged
                                    |> Observable.mergeInner
                                    |> Observable.groupByElement fst snd
                                    |> Observable.flatmap (fun g ->
                                        g
                                        |> Observable.bufferSpanCount appConfig.ParserBatchFlushTimeSpan appConfig.ParserSubscriptionBatchSize
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

                                let i = state.Iteration + 1
                                logger.LogSwitchingToIteration i
                                mainObserver.OnNext(merged)
                                state.Merged.OnNext(state.CompleteErrorSignal)
                                i
                            else
                                state.Iteration
                        
                        state.Merged.OnNext(externalSignal |> Observable.map (fun log -> (logSourceId, log |> LogPositionSignal.Next)))
                        reply.Reply(externalSignal :> IObserver<TechLogPosition>)

                        logger.LogInnerObserverCreated logSourceId

                        return! loop
                            { state with
                                Observers = state.Observers |> Map.add logSourceId (externalSignal, d);
                                Iteration = iteration
                            }

                | Msg.SendCompleteToInner logSourceId ->
                    state.CompleteErrorSignal.OnNext((logSourceId, LogPositionSignal.Completed))
                    return! loop state

                | Msg.FinishObserver (logSourceId, errOpt) ->
                    match state.Observers |> Map.tryFind logSourceId with
                    | Some (inner, d) ->
                        logger.LogSourceIsDisposing logSourceId
                        d.Dispose()
                        inner.Dispose()

                        match errOpt with
                        | Some err ->
                            state.CompleteErrorSignal.OnNext((logSourceId, LogPositionSignal.Error err))
                        | None ->
                            state.CompleteErrorSignal.OnNext((logSourceId, LogPositionSignal.Completed))
                        
                        let observers = state.Observers |> Map.remove logSourceId
                        if observers.IsEmpty then
                            state.CompleteErrorSignal.OnCompleted()
                            state.Merged.OnCompleted()

                            logger.LogSourceIsDisposedWithMerged logSourceId

                            state.CompleteErrorSignal.Dispose()
                            state.Merged.Dispose()

                            return! loop { State.Init with Iteration = state.Iteration }
                        else
                            logger.LogSourceIsDisposed logSourceId
                            return! loop { state with Observers = observers; }
                    | None ->
                        return! loop state

                | Msg.Dispose reply ->
                    try
                        state.Observers
                        |> Map.iter (fun _ (s, d) ->
                            d.Dispose()
                            s.Dispose()
                        )
                        state.Merged.Dispose()
                    finally
                        reply.Reply()
            }

        loop State.Init
    )


    let init (logger: AppSubjectLogger) (appConfig: AppConfig) =
        let agent = agent logger appConfig
        agent.Start()

        let mainSubject = Subject.broadcast

        let mainSwitch : IObservable<LogSourceId * ObservableLogPosition> =
            mainSubject
            |> Observable.switch
            |> Observable.publish
            |> Observable.refCount
            |> Observable.observeOn TaskPoolScheduler.Default

        {
            GetObserver =
                fun logSourceId ->
                    logger.LogRequestingObserver logSourceId
                    let (inner) = agent.PostAndReply(fun reply -> Msg.GetObserver (logSourceId, mainSubject, reply))
                    logger.LogObserverObtained logSourceId
                    inner
            Observable = mainSwitch
            Dispose =
                fun () ->
                    mainSubject.OnCompleted()
                    agent.PostAndReply(fun reply -> Msg.Dispose reply)
                    agent.Dispose()
                    mainSubject.Dispose()
            SendCompleteToInner =
                fun logSourceId ->
                    agent.Post(Msg.SendCompleteToInner logSourceId)
        }
