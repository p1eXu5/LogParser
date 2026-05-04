namespace LogParser.ElmishApp.Types

type LogFile =
    | Existing of string
    | New

[<Struct>]
type ShowMode =
    | All
    | OnlyParsedLogs

[<Struct>]
type FileState =
    | NewTemp
    | Existing