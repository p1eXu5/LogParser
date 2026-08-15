namespace LogParser.ElmishApp.Models

open System

type LogFileListModel =
    {
        LogFileModelList: LogFileModel list
        SelectedLogFileModelId: int
    }

module LogFileListModel =

    open System
    open LogParser.ElmishApp.Types

    type Msg =
        | AddNewLogFile
        | SelectLogFileId of int
        | LogFileModelMsg of int * LogFileModel.Msg

    let init () =
        let logFileModel = LogFileModel.initNew (1)
        {
            LogFileModelList =
                [
                    logFileModel
                ]
            SelectedLogFileModelId = logFileModel.Id
        }

    let addLogFileModel (m: LogFileListModel) =
        let length = m.LogFileModelList.Length
        let id = length + 1
        let logFileModel = LogFileModel.initNew (id)
        { m with
            LogFileModelList = m.LogFileModelList |> List.insertAt length logFileModel
            SelectedLogFileModelId = id
        }

    let selecteLogFileTitle (m: LogFileListModel) =
        m.LogFileModelList
        |> List.tryFind (fun f -> f.Id = m.SelectedLogFileModelId)
        |> Option.map (fun f -> f |> LogFileModel.title)

    let selecteLogFileName (m: LogFileListModel) =
        m.LogFileModelList
        |> List.tryFind (fun f -> f.Id = m.SelectedLogFileModelId)
        |> Option.map (fun f -> f |> LogFileModel.fullPath)

    let inline withLogFileList logFileList (m: LogFileListModel) =
        { m with LogFileModelList = logFileList }

