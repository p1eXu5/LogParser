namespace LogParser.App

open System
open System.IO
open System.Threading
open Microsoft.Extensions.Logging
open LogParser.App.Abstractions
open System.Threading.Tasks


type private Msg =
    | ParseFile of ClientId * FilePath * CancellationToken * AsyncReplyChannel<Result<unit, string>>

// --------------------------------------------
// FileStorage
// --------------------------------------------

type private State =
    {
        // not implemented
        Cts: CancellationTokenSource
    }

module State =

    let init =
        {
            Cts = new CancellationTokenSource()
        }

/// <summary>
/// 
/// </summary>
type LogRepository(storageFactory: IStorageFactory<IStorage>, appSubject: IAppSubject, logger: ILogger<LogRepository>) =

    let mutable agent : MailboxProcessor<Msg> | null = null

    let parseText id text reply ct =
        match storageFactory.Init() with
        | Error err ->
            Error err

        | Ok storage ->
            let observer = appSubject.GetObserver id
            
            Async.Sta

    do
        agent <- new MailboxProcessor<Msg>(
            (fun processor ->
                let rec running state =
                    async {
                        let! msg = processor.Receive()

                        // not implemented
                        match msg with
                        | Msg.ParseFile (id, path, ct, reply) ->
                            reply.Reply ()
                            return! running state
                    }

                running ()
            )
        )

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="text">Text to parse.</param>
    /// <param name="ct">Token for the parsing task creating cancellation.</param>
    member _.ParseTextAck(id: ClientId, text: string, ct: CancellationToken) =
        try
            let tmpFile = Path.GetTempFileName()
            match FilePath.create tmpFile with
            | Ok path ->
                agent.PostAndReply(fun reply -> Msg.ParseFile (id, path, ct, reply))
                Ok ()
            | Error err ->
                logger.LogError(err)
                Error "Failed to parse text"
        with ex ->
            logger.LogError(ex, "Failed to parse text.")
            Error "Failed to parse text"

    // TODO: other methods