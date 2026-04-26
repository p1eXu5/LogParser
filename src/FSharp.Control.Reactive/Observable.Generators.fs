namespace FSharp.Control.Reactive.Observables.Generators

open System
open System.Reactive.Linq
open System.Reactive.Concurrency

/// The Reactive module provides operators for working with IObservable<_> in F#.
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Observable =

    /// Creates a range as an observable
    let inline range start count = Observable.Range(start, count)


    /// Creates a range as an observable, using the specified scheduler to send out observer messages.
    let inline rangeOn (scheduler: IScheduler) start count = Observable.Range(start, count, scheduler)

    
    /// Generates an observable sequence by running a state-driven loop producing the sequence's elements.
    let inline generate initialState condition iterate resultSelector =
        Observable.Generate(                            
            initialState,
            Func<'State,bool> condition,
            Func<'State,'State> iterate,
            Func<'State,'Result> resultSelector
        )

    /// Generates an observable sequence by running a state-driven loop producing the sequence's elements.
    let inline generateOn (scheduler:IScheduler) initialState condition iterate resultSelector =
        Observable.Generate(
            initialState,
            Func<'State, bool> condition,
            Func<'State, 'State> iterate,
            Func<'State, 'TResult> resultSelector,
            scheduler
        )


    /// Generates an observable sequence by running a state-driven and temporal loop producing the sequence's elements.
    let inline generateDateTime (initialState:'State) condition iterate resultSelector timeSelector : IObservable<'Result> =
        Observable.Generate(
            initialState,
            Func<'State,bool> condition,
            Func<'State,'State> iterate,
            Func<'State,'Result> resultSelector,
            Func<'State, DateTimeOffset> timeSelector
        )


    /// Generates an observable sequence by running a state-driven and temporal loop producing the sequence's elements,
    /// using a specified scheduler to run timers and to send out observer messages.
    let inline generateDateTimeOn (scheduler:IScheduler) (initialState:'State) condition iterate resultSelector timeSelector : IObservable<'Result> =
        Observable.Generate(
            initialState,
            Func<'State,bool> condition,
            Func<'State,'State> iterate,
            Func<'State,'Result> resultSelector,
            Func<'State, DateTimeOffset> timeSelector,
            scheduler
        )

    /// Generates an observable sequence by running a state-driven and temporal loop producing the sequence's elements.
    let inline generateTimeSpan (initialState:'State) condition iterate resultSelector timeSelector : IObservable<'Result> =
        Observable.Generate(
            initialState,
            Func<_,_> condition,
            Func<_,_> iterate,
            Func<'State,'Result> resultSelector,
            Func<'State,TimeSpan> timeSelector
        )


    /// Generates an observable sequence by running a state-driven and temporal loop producing the sequence's elements,
    /// using a specified scheduler to run timers and to send out observer messages.
    let inline generateTimeSpanOn (scheduler:IScheduler) (initialState: 'State) condition iterate resultSelector timeSelector : IObservable<'Result> =
        Observable.Generate(
            initialState,
            Func<_,_> condition,
            Func<_,_> iterate,
            Func<'State,'Result> resultSelector,
            Func<'State,TimeSpan> timeSelector,
            scheduler
        )

    /// Returns an observable sequence that produces a value after each period
    let inline interval period =
        Observable.Interval(period)

    /// Returns an observable sequence that produces a value on the specified scheduler after each period
    let inline intervalOn (scheduler : IScheduler) period =
        Observable.Interval(period, scheduler)


