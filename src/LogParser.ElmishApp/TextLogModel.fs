namespace LogParser.ElmishApp.Models

open System

type TextLogModel =
    {
        Id: Guid
        Log: string
    }


module TextLogModel =

    type Msg =
        | CopyLog
        | DecodeLog

    open Elmish


    let init (log: string) =
        {
            Id = Guid.NewGuid()
            Log = log
        }
        , Cmd.none


// -----------------------------------------------------

namespace LogParser.ElmishApp.TextLogModel

open System.Windows.Input
open LogParser.ElmishApp
open LogParser.ElmishApp.Models
open LogParser.ElmishApp.Models.TextLogModel

module Program =

    open System.Windows

    let update msg model =
        match msg with
        | Msg.CopyLog ->
            Clipboard.SetText(model.Log)
            model
        | Msg.DecodeLog ->
            { model with Log = Helpers.decodeUnicodeEscapes model.Log }


type IBindings =
    interface
        abstract Log: string with get
        abstract CopyCommand: ICommand with get
        abstract DecodeCommand: ICommand with get
    end

module Bindings =

    open Elmish.WPF

    let private __ = Unchecked.defaultof<IBindings>

    let bindings : Binding<TextLogModel, TextLogModel.Msg> list =
        [
            nameof __.Log |> Binding.oneWay (fun m -> m.Log)
            nameof __.CopyCommand |> Binding.cmd TextLogModel.Msg.CopyLog
            nameof __.DecodeCommand |> Binding.cmd (fun m -> TextLogModel.Msg.DecodeLog)
        ]