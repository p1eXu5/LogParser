namespace LogParser.ElmishApp.Interfaces

type ISettingsManager =
    interface
        abstract Load : key: string -> obj
        abstract Save : key: string -> value: obj -> unit
    end


type IErrorMessageQueue =
    interface
        abstract EnqueueError : string -> unit
    end