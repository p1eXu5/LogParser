namespace rec LogParser.DesktopClient.ElmishApp.Models

open System
open LogParser.Core.Types


type TechFieldModel =
    {
        TechField: TechField
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

    type Msg =
        | CopyValueRequested
        | PinFieldValueInHeader of key: string



    let init (techField: TechField) =

        match techField with
        | TechField.Json (_, fields) ->
            { 
                TechField = techField 
                Tag = Tag.JsonField; 
                Key = techField |> TechField.key; 
                Header = "json object" |> Some; 
                Json = fields |> TechField.toString 1 |> Some; 
                Text = None
                Postfix = None
            }
        | TechField.JsonAnnotated jta ->
            { 
                TechField = techField 
                Tag = Tag.AnnotatedJsonField; 
                Key = techField |> TechField.key; 
                Header = "json object" |> Some; 
                Json = jta.Body |> TechField.toString 1 |> Some; 
                Text = jta.Annotation |> Some
                Postfix = None
            }
        | TechField.Body fields ->
            { 
                TechField = techField 
                Tag = Tag.JsonField; 
                Key = techField |> TechField.key; 
                Header = "json object" |> Some; 
                Json = fields |> TechField.toString 1 |> Some; 
                Text = None
                Postfix = None
            }
        | TechField.MessageBoddied (text, fields) ->
            { 
                TechField = techField 
                Tag = Tag.AnnotatedJsonField; 
                Key = techField |> TechField.key; 
                Header = "json object" |> Some; 
                Json = fields |> TechField.toString 1 |> Some; 
                Text = text |> Some
                Postfix = None
            }

        | TechField.MessageBoddiedWithPostfix (text, fields, postfix) ->
            { 
                TechField = techField 
                Tag = Tag.WithPostfixAnnotatedJsonField;
                Key = techField |> TechField.key; 
                Header = "json object" |> Some; 
                Json = fields |> TechField.toString 1 |> Some; 
                Text = text |> Some
                Postfix = postfix |> Some
            }

        | _ ->
            { 
                TechField = techField 
                Header = None; 
                Tag = Tag.SimpleField; 
                Key = techField |> TechField.key; 
                Text = techField |> TechField.value |> Some;
                Json = None
                Postfix = None
            }


namespace rec LogParser.DesktopClient.ElmishApp.TechFieldModel

open LogParser.DesktopClient.ElmishApp.Models
open LogParser.DesktopClient.ElmishApp.Models.TechFieldModel


module Program =

    open System.Windows

    let update msg model =
        match msg with
        | CopyValueRequested ->
            match model.Tag with
            | Tag.JsonField
            | Tag.AnnotatedJsonField ->
                Clipboard.SetText(model.Json |> Option.defaultValue "")
            | _ -> 
                Clipboard.SetText(model.Text |> Option.defaultValue "")

        | _ ->
            ()


module Bindings =

    open Elmish.WPF

    let bindings () : Binding<TechFieldModel, TechFieldModel.Msg> list =
        [
            "Tag" |> Binding.oneWay (fun m -> m.Tag)
            "Key" |> Binding.oneWay (fun m -> m.Key)
            "Text" |> Binding.oneWayOpt (fun m -> m.Text)
            "Header" |> Binding.oneWayOpt (fun m -> m.Header)
            "Postfix" |> Binding.oneWayOpt (fun m -> m.Postfix)
            "Json" |> Binding.oneWayOpt (fun m -> m.Json)
            "CopyCommand" |> Binding.cmd Msg.CopyValueRequested
            "PinCommand" |> Binding.cmd (fun m -> Msg.PinFieldValueInHeader m.Key)
        ]