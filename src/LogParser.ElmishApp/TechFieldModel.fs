namespace rec LogParser.ElmishApp.Models

open System
open LogParser.Core.Types


type TechFieldModel =
    {
        TechField: TechJsonLogField
        Key: string
        Header: string option
        Text: string option
        Json: string option
        Tag: TechFieldModel.Tag
        Postfix: string option
    }


module TechFieldModel =

    type Tag =
        | SimpleField = 0
        | JsonField = 1
        | AnnotatedJsonField = 2
        | WithPostfixAnnotatedJsonField = 3

    // Intensions. Currently processed in MainModel Program.
    type Msg =
        | CopyValue
        | DecodeValue
        | InsertLineBreaks
        | PinFieldValueInHeader of key: string

    let init (techField: TechJsonLogField) =

        match techField with
        | TechJsonLogField.Json (_, fields) ->
            { 
                TechField = techField 
                Tag = Tag.JsonField; 
                Key = techField |> TechJsonLogField.key; 
                Header = "json object" |> Some; 
                Json = fields |> TechJsonLogField.toString 1 |> Some; 
                Text = None
                Postfix = None
            }
        | TechJsonLogField.JsonAnnotated jta ->
            { 
                TechField = techField 
                Tag = Tag.AnnotatedJsonField; 
                Key = techField |> TechJsonLogField.key; 
                Header = "json object" |> Some; 
                Json = jta.Body |> TechJsonLogField.toString 1 |> Some; 
                Text = jta.Annotation |> Some
                Postfix = None
            }

        | TechJsonLogField.Body fields ->
            { 
                TechField = techField 
                Tag = Tag.JsonField; 
                Key = techField |> TechJsonLogField.key; 
                Header = "json object" |> Some; 
                Json = fields |> TechJsonLogField.toString 1 |> Some; 
                Text = None
                Postfix = None
            }
        | TechJsonLogField.MessageBoddied (text, fields) ->
            { 
                TechField = techField 
                Tag = Tag.AnnotatedJsonField; 
                Key = techField |> TechJsonLogField.key; 
                Header = "json object" |> Some; 
                Json = fields |> TechJsonLogField.toString 1 |> Some; 
                Text = text |> Some
                Postfix = None
            }

        | TechJsonLogField.MessageBoddiedWithPostfix (text, fields, postfix) ->
            { 
                TechField = techField 
                Tag = Tag.WithPostfixAnnotatedJsonField;
                Key = techField |> TechJsonLogField.key; 
                Header = "json object" |> Some; 
                Json = fields |> TechJsonLogField.toString 1 |> Some; 
                Text = text |> Some
                Postfix = postfix |> Some
            }

        | TechJsonLogField.MessageArrayJson _ ->
            { 
                TechField = techField 
                Tag = Tag.JsonField;
                Key = techField |> TechJsonLogField.key; 
                Header = "json object" |> Some; 
                Json = techField |> TechJsonLogField.value |> Some; 
                Text = None
                Postfix = None
            }

        | _ ->
            { 
                TechField = techField 
                Header = None; 
                Tag = Tag.SimpleField; 
                Key = techField |> TechJsonLogField.key; 
                Text = techField |> TechJsonLogField.value |> Some;
                Json = None
                Postfix = None
            }


namespace rec LogParser.ElmishApp.TechFieldModel

open System.Windows.Input
open LogParser.ElmishApp
open LogParser.ElmishApp.Models
open LogParser.ElmishApp.Models.TechFieldModel


module Program =

    open System
    open System.Windows

    let update msg model =
        match msg with
        | CopyValue ->
            match model.Tag with
            | Tag.JsonField
            | Tag.AnnotatedJsonField ->
                Clipboard.SetText(model.Json |> Option.defaultValue "")
            | _ -> 
                Clipboard.SetText(model.Text |> Option.defaultValue "")
            model
        | DecodeValue ->
            { model with
                Text =
                    match model.Text with
                    | Some text -> Helpers.decodeUnicodeEscapes text |> Some
                    | _ -> None
                Json =
                    match model.Json with
                    | Some json -> Helpers.decodeUnicodeEscapes json |> Some
                    | _ -> None
            }

        | InsertLineBreaks ->
            { model with
                Text =
                    match model.Text with
                    | Some text -> Helpers.insertLineBreaks text |> Some
                    | _ -> None
                Json =
                    match model.Json with
                    | Some json -> Helpers.insertLineBreaks json |> Some
                    | _ -> None
            }
        | _ ->
            model


type IBindings =
    interface
        abstract Tag: Tag with get
        abstract Key: string with get
        abstract Text: string option with get
        abstract Header: string option with get
        abstract Postfix: string option with get
        abstract Json: string option with get
        abstract CopyCommand: ICommand with get
        abstract PinCommand: ICommand with get
        abstract DecodeCommand: ICommand with get
        abstract InsertLineBreaksCommand: ICommand with get
    end

module Bindings =

    open Elmish.WPF

    let private __ = Unchecked.defaultof<IBindings>

    let bindings () : Binding<TechFieldModel, TechFieldModel.Msg> list =
        [
            nameof __.Tag |> Binding.oneWay (fun m -> m.Tag)
            nameof __.Key |> Binding.oneWay (fun m -> m.Key)
            nameof __.Text |> Binding.oneWayOpt (fun m -> m.Text)
            nameof __.Header |> Binding.oneWayOpt (fun m -> m.Header)
            nameof __.Postfix |> Binding.oneWayOpt (fun m -> m.Postfix)
            nameof __.Json |> Binding.oneWayOpt (fun m -> m.Json)
            nameof __.CopyCommand |> Binding.cmd Msg.CopyValue
            nameof __.PinCommand |> Binding.cmd (fun m -> Msg.PinFieldValueInHeader m.Key)
            nameof __.DecodeCommand |> Binding.cmd (fun m -> Msg.DecodeValue)
            nameof __.InsertLineBreaksCommand |> Binding.cmd (fun m -> Msg.InsertLineBreaks)
        ]