module Example

open Fable.Core
open Elmish
open Fable.Elmish.ElmishToReact

type Props =
  [<Emit("$0.count")>] abstract Count : int
  abstract setCount : int -> unit

type Model =
  { InternalCount : int
    Props : Props }

type Msg =
  | IncrementInternal
  | DecrementInternal
  | IncrementExternal
  | DecrementExternal
  | UpdateProps of Props
  | Unmount

let init props =
  { InternalCount = 0
    Props = props }

let update msg model =
  match msg with
  | IncrementInternal -> { model with InternalCount = model.InternalCount + 1 }
  | DecrementInternal -> { model with InternalCount = model.InternalCount - 1 }
  | IncrementExternal ->
      model.Props.setCount (model.Props.Count + 1)
      model
  | DecrementExternal ->
      model.Props.setCount (model.Props.Count - 1)
      model
  | UpdateProps props -> { model with Props = props }
  | Unmount ->
    Browser.Dom.console.log "unmount msg received"
    model

open Fable.React
open Fable.React.Props

let view model dispatch =
  let onClick msg =
    OnClick <| fun _ -> msg |> dispatch

  div [ ClassName "elmish" ]
    [ h3 [] [ str "The elmish program" ]
      p []
        [ str "This is the internal counter. Current value is stored in the elmish model: "
          button [ onClick DecrementInternal ] [ str "-" ]
          str (sprintf " %i "  model.InternalCount)
          button [ onClick IncrementInternal ] [ str "+" ] ]
      p []
        [ str "This is the external counter. Current value is passed to elmish program via React props:"
          button [ onClick DecrementExternal ] [ str "-" ]
          str (sprintf " %i "  model.Props.Count)
          button [ onClick IncrementExternal ] [ str "+" ] ] ]


let program = Program.mkSimple init update view

let externalisedProgram =
  Externalised.externalise program
  |> Externalised.withPropsMsg UpdateProps
  |> Externalised.withUnmountMsg Unmount

let ExampleComponent = elmishToReact externalisedProgram
