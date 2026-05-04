namespace LogParser.App

open LogParser.Types

type LogParseMsg =
    | LogPosition of LogPosition
    | LogPositionBunch of LogPosition list
    | ParsingError of string
