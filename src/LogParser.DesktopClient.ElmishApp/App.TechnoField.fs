namespace LogParser.App.TechnoField

open System
open LogParser.Core.Types
open Microsoft.Extensions.Logging

type Model =
    {
        TechnoField: TechnoField
        Key: string
        Header: string option
        Text: string option
        Json: string option
        Tag: Tag
        Postfix: string option
    }
and
    Tag =
        | SimpleField = 0
        | JsonField = 1
        | AnnotatedJsonField = 2
        | WithPostfixAnnotatedJsonField = 3

type Msg =
    | CopyValueRequested
    | PinFieldValueInHeader of key: string

module Program =

    open Elmish
    open Microsoft.FSharp.Reflection  


    let init (technoField: TechnoField) =

        match technoField with
        | TechnoField.Json (_, fields) ->
            { 
                TechnoField = technoField 
                Tag = Tag.JsonField; 
                Key = technoField |> TechnoFields.key; 
                Header = "json object" |> Some; 
                Json = fields |> TechnoFields.toString 1 |> Some; 
                Text = None
                Postfix = None
            }
        | TechnoField.JsonAnnotated (_, header, fields) ->
            { 
                TechnoField = technoField 
                Tag = Tag.AnnotatedJsonField; 
                Key = technoField |> TechnoFields.key; 
                Header = "json object" |> Some; 
                Json = fields |> TechnoFields.toString 1 |> Some; 
                Text = header |> Some
                Postfix = None
            }
        | TechnoField.Body fields ->
            { 
                TechnoField = technoField 
                Tag = Tag.JsonField; 
                Key = technoField |> TechnoFields.key; 
                Header = "json object" |> Some; 
                Json = fields |> TechnoFields.toString 1 |> Some; 
                Text = None
                Postfix = None
            }
        | TechnoField.MessageBoddied (text, fields) ->
            { 
                TechnoField = technoField 
                Tag = Tag.AnnotatedJsonField; 
                Key = technoField |> TechnoFields.key; 
                Header = "json object" |> Some; 
                Json = fields |> TechnoFields.toString 1 |> Some; 
                Text = text |> Some
                Postfix = None
            }

        | TechnoField.MessageBoddiedWithPostfix (text, fields, postfix) ->
            { 
                TechnoField = technoField 
                Tag = Tag.WithPostfixAnnotatedJsonField;
                Key = technoField |> TechnoFields.key; 
                Header = "json object" |> Some; 
                Json = fields |> TechnoFields.toString 1 |> Some; 
                Text = text |> Some
                Postfix = postfix |> Some
            }

        | TechnoField.MessageParameterized (text, typeJson) ->
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


    open Elmish.WPF

    let bindings () : Binding<Model, Msg> list =
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