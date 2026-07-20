namespace LogParser.ElmishApp.Models

open System
open LogParser.Types

type TextLogModelV2 =
    {
        Id: Guid
        LogPosition: LogPosition
    }


module TextLogModelV2 =

    type Msg =
        | CopyLog
        | DecodeLog
        | InsertLineBreaks

    open Elmish


    let init (log: string) =
        {
            Id = Guid.NewGuid()
            Log = log
        }
        , Cmd.none


// -----------------------------------------------------

namespace LogParser.ElmishApp.TextLogModelV2

open System.Windows.Input
open LogParser.ElmishApp
open LogParser.ElmishApp.Models
open LogParser.ElmishApp.Models.TextLogModelV2

module Program =

    open System.Windows

    let update msg model =
        match msg with
        | Msg.CopyLog ->
            Clipboard.SetText(model.Log)
            model
        | Msg.DecodeLog ->
            { model with Log = Helpers.decodeUnicodeEscapes model.Log }
        | Msg.InsertLineBreaks ->
            { model with Log = Helpers.insertLineBreaks model.Log }


type IBindings =
    interface
        abstract Log: string with get
        abstract CopyCommand: ICommand with get
        abstract DecodeCommand: ICommand with get
        abstract InsertLineBreaksCommand: ICommand with get
    end

module Bindings =

    open Elmish.WPF

    let private __ = Unchecked.defaultof<IBindings>

    let bindings : Binding<TextLogModelV2, TextLogModelV2.Msg> list =
        [
            nameof __.Log |> Binding.oneWay (fun m -> m.Log)
            nameof __.CopyCommand |> Binding.cmd TextLogModelV2.Msg.CopyLog
            nameof __.DecodeCommand |> Binding.cmd (fun m -> TextLogModelV2.Msg.DecodeLog)
            nameof __.InsertLineBreaksCommand |> Binding.cmd (fun m -> Msg.InsertLineBreaks)
        ]