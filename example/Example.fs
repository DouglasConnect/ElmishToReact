module Example

open Fable.Core
open Elmish
open ElmishToReact

type Props =
  [<Emit("$0.label")>] abstract Label : string

type Model =
  { Count : int
    Props : Props }

type Msg =
  | Increment
  | Decrement
  | UpdateProps of Props
  | Unmount

let init props =
  { Count = 0
    Props = props }

let update msg model =
  match msg with
  | Increment -> { model with Count = model.Count + 1 }
  | Decrement -> { model with Count = model.Count - 1 }
  | UpdateProps props -> { model with Props = props }
  | Unmount ->
    Browser.Dom.console.log "unmount msg received"
    model

open Fable.React
open Fable.React.Props

let view model dispatch =
  let onClick msg =
    OnClick <| fun _ -> msg |> dispatch

  fragment []
    [ div [] [ str model.Props.Label ]
      button [ onClick Decrement ] [ str "-" ]
      div [] [ str (string model.Count) ]
      button [ onClick Increment ] [ str "+" ] ]


let program = Program.mkSimple init update view

let externalisedProgram =
  Externalised.externalise program
  |> Externalised.withPropsMsg UpdateProps
  |> Externalised.withUnmountMsg Unmount

let ExampleComponent = ElmishComponent.elmishToReact externalisedProgram
