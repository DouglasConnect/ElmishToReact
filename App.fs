module ElmishToReact.Example

open Fable.Core
open Fable.Import
open Elmish

module Rxjs =

  open Fable.Core.JsInterop

  [<Import("Subject", from="rxjs")>]
  let Subject : obj = jsNative

  type ISubject<'a> =
    interface
      inherit System.IObservable<'a>
      abstract next : 'a -> unit
    end

  let subject<'a> : ISubject<'a> =
    createNew Subject () :?> ISubject<'a>

type Reactified<'props, 'model, 'msg> =
  private { Program : Program<'props, 'model, 'msg, Fable.React.ReactElement>
            PropsToMsg : ('props -> 'msg) option
            UnmountCmd : Cmd<'msg> }

module Reactified =

  open Elmish.React

  let reactify program =
    { Program = program
      PropsToMsg = None
      UnmountCmd = Cmd.none }

  let withPropsMsg (msgType: 'props -> 'msg) (reactified : Reactified<'props, 'model, 'msg>) =
    { reactified with PropsToMsg = msgType |> Some }

  let withUnmountMsg (msgType: 'msg) (reactified : Reactified<'props, 'model, 'msg>) =
    { reactified with UnmountCmd = Cmd.ofMsg msgType }

  let runWith (props : 'props) (el : Browser.Types.Element ) (reactified : Reactified<'props, 'model, 'msg>) : 'props -> unit =

    let subject : Rxjs.ISubject<'props> = Rxjs.subject

    let subscription (initial : 'model) : Cmd<'msg> =
      match reactified.PropsToMsg with
      | Some propsToMsg ->
        let sub (dispatch : 'msg -> unit) : unit =

          let callback props =
            propsToMsg props |> dispatch

          Observable.subscribe callback subject |> ignore

        Cmd.ofSub sub

      | None ->
        Cmd.none


    reactified.Program
    |> Program.withSubscription subscription
    |> Program.withReactSynchronous el.id
    |> Program.runWith props

    subject.next

module ElmishComponent =

  open Fable.React
  open Fable.React.Props

  let elmishToReact (program : Reactified<'props, 'model, 'msg>) =
    FunctionComponent.Of( fun (props: 'props) ->
      let divRef : IRefValue<Browser.Types.Element option> = Hooks.useRef(None)
      let onUpdateRef : IRefValue<('props -> unit) option> = Hooks.useRef(None)

      let subscription () =
        match divRef.current with
        | Some el ->
          let onUpdate =
            match onUpdateRef.current with
            | Some fn -> fn
            | None -> Reactified.runWith props el program

          onUpdate props
        | None -> ()

      Hooks.useEffect subscription

      div [ RefValue divRef ] []
    )


type Props =
  { Label : string }

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

let view count dispatch =
  let onClick msg =
    OnClick <| fun _ -> msg |> dispatch

  div []
    [ button [ onClick Decrement ] [ str "-" ]
      div [] [ str (string count) ]
      button [ onClick Increment ] [ str "+" ] ]


let program = Program.mkSimple init update view

let reactifiedProgram =
  Reactified.reactify program
  |> Reactified.withPropsMsg UpdateProps
  |> Reactified.withUnmountMsg Unmount

let ExampleComponent = ElmishComponent.elmishToReact reactifiedProgram
