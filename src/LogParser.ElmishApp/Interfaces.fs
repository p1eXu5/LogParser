namespace LogParser.ElmishApp.Interfaces

open LogParser.App

type ISettingsManager =
    interface
        abstract Load : key: string -> obj
        abstract Save : key: string -> value: obj -> unit
        abstract AppConfig : AppConfig with get
    end


type IErrorMessageQueue =
    interface
        abstract EnqueueError : string -> unit
    end