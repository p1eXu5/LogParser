namespace rec LogParser.DesktopClient.ElmishApp.Models

open System
open LogParser.Core.Types


module private Core =
    type TechnoField = LogParser.Core.Types.TechnoField


type TechnoField =
    {
        TechnoField: Core.TechnoField
        Key: string
        Header: string option
        Text: string option
        Json: string option
        Tag: TechnoField.Tag
        Postfix: string option
    }


module TechnoField =

    type Tag =
            | SimpleField = 0
            | JsonField = 1
            | AnnotatedJsonField = 2
            | WithPostfixAnnotatedJsonField = 3

    type Msg =
        | CopyValueRequested
        | PinFieldValueInHeader of key: string



    let init (technoField: Core.TechnoField) =

        match technoField with
        | Core.TechnoField.Json (_, fields) ->
            { 
                TechnoField = technoField 
                Tag = Tag.JsonField; 
                Key = technoField |> TechnoFields.key; 
                Header = "json object" |> Some; 
                Json = fields |> TechnoFields.toString 1 |> Some; 
                Text = None
                Postfix = None
            }
        | Core.TechnoField.JsonAnnotated (_, header, fields) ->
            { 
                TechnoField = technoField 
                Tag = Tag.AnnotatedJsonField; 
                Key = technoField |> TechnoFields.key; 
                Header = "json object" |> Some; 
                Json = fields |> TechnoFields.toString 1 |> Some; 
                Text = header |> Some
                Postfix = None
            }
        | Core.TechnoField.Body fields ->
            { 
                TechnoField = technoField 
                Tag = Tag.JsonField; 
                Key = technoField |> TechnoFields.key; 
                Header = "json object" |> Some; 
                Json = fields |> TechnoFields.toString 1 |> Some; 
                Text = None
                Postfix = None
            }
        | Core.TechnoField.MessageBoddied (text, fields) ->
            { 
                TechnoField = technoField 
                Tag = Tag.AnnotatedJsonField; 
                Key = technoField |> TechnoFields.key; 
                Header = "json object" |> Some; 
                Json = fields |> TechnoFields.toString 1 |> Some; 
                Text = text |> Some
                Postfix = None
            }

        | Core.TechnoField.MessageBoddiedWithPostfix (text, fields, postfix) ->
            { 
                TechnoField = technoField 
                Tag = Tag.WithPostfixAnnotatedJsonField;
                Key = technoField |> TechnoFields.key; 
                Header = "json object" |> Some; 
                Json = fields |> TechnoFields.toString 1 |> Some; 
                Text = text |> Some
                Postfix = postfix |> Some
            }

        | Core.TechnoField.MessageParameterized (text, typeJson) ->
            let parameter = typeJson |> List.map (sprintf "%O") |> (fun l -> String.Join(",\n", l))

            { 
                TechnoField = technoField 
                Tag = Tag.AnnotatedJsonField; 
                Key = technoField |> TechnoFields.key; 
                Header = $"parameters:" |> Some; 
                Json = parameter |> Some; 
                Text = text |> Some
                Postfix = None
            }

        | _ ->
            { 
                TechnoField = technoField 
                Header = None; 
                Tag = Tag.SimpleField; 
                Key = technoField |> TechnoFields.key; 
                Text = technoField |> TechnoFields.value |> Some;
                Json = None
                Postfix = None
            }


namespace rec LogParser.DesktopClient.ElmishApp.TechnoField

open LogParser.DesktopClient.ElmishApp.Models
open LogParser.DesktopClient.ElmishApp.Models.TechnoField


module Program =

    open System.Windows;

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

    let bindings () : Binding<TechnoField, Msg> list =
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