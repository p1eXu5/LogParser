namespace LogParser.ElmishApp.Models

open LogParser.ElmishApp.Types
open System.IO
open p1eXu5.FSharp.ElmishExtensions
open LogParser.App
open LogParser.App.LogRepository

type LogFileModel =
    {
        Id: int
        Repo: LogRepository
        Title: string option
        LogFile: LogFile option
        TechLogModelList: TechLogModel list
        SelectedTechLogId: TechLogId voption
        CacheKey: CacheKey
        ImportLogsState: AsyncDeferredState
    }

module LogFileModel =

    open System

    type Msg = 
        | TechLogModelMsg of TechLogId * TechLogModel.Msg
        | PastFromClipboardRequested of AsyncOperation<string, ParseTextRequestResult>
        | AppendFirstLogBatch of LogMetaBatch
        | SetSelectedTechLogId of TechLogId voption
        | OrderByTimestamp
        | OnError of string

    module MsgWith =

        let (|``Start of PastFromClipboardRequested``|_|) (model: LogFileModel) (msg: Msg) =
            match msg, model.ImportLogsState with
            | Msg.PastFromClipboardRequested (AsyncOperation.Start text), AsyncDeferredState.NotRequested
            | Msg.PastFromClipboardRequested (AsyncOperation.Start text), AsyncDeferredState.Retrieved ->
                let (state, cts) = model.ImportLogsState |> AsyncDeferredState.forceInProgressWithCancellation
                (text, state, cts) |> Some
            | _ -> None

        let (|``Finish of PastFromClipboardRequested``|_|) (model: LogFileModel) (msg: Msg) =
            match msg with
            | Msg.PastFromClipboardRequested (AsyncOperation.Finish (res, cts)) ->
                model.ImportLogsState
                |> AsyncDeferredState.chooseRetrieved res cts
            | _ -> None

    let initNew (id: int) (repo: LogRepository) =
        {
            Id = id
            Repo = repo
            LogFile = None
            Title = None
            TechLogModelList = []
            SelectedTechLogId = ValueNone
            CacheKey = Guid.NewGuid()
            ImportLogsState = AsyncDeferredState.NotRequested
        }

    let inline fileNameWithoutExtension (m: LogFileModel) =
        m.LogFile
        |> Option.bind (LogFile.fileNameWithoutExtension)

    let inline fullPath (m: LogFileModel) =
        m.LogFile
        |> Option.bind (LogFile.fullPath)

    let title (m: LogFileModel) =
        match m.Title with
        | Some v -> v
        | None ->
            m
            |> fileNameWithoutExtension
            |> Option.defaultValue "New"

    let inline withTechLogListModel techLogListModel (m: LogFileModel) =
        { m with TechLogModelList = techLogListModel }


