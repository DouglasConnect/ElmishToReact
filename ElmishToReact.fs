module ElmishToReact

open Fable.Core
open Elmish

module Rxjs =

  open Fable.Core.JsInterop

  [<Import("Subject", from="rxjs")>]
  let Subject : obj = jsNative

  type RxSubject<'a> =
    abstract next : 'a -> unit
    abstract subscribe : ('a -> unit) -> unit

  let subject<'a> : RxSubject<'a> =
    createNew Subject () :?> RxSubject<'a>

type Reactified<'props, 'model, 'msg> =
  private { Program : Program<'props, 'model, 'msg, Fable.React.ReactElement>
            PropsToMsg : ('props -> 'msg) option
            UnmountCmd : Cmd<'msg> }

module Program =

  open Fable.React
  open Elmish.React

  let withReactSynchronousOnElement element (program: Elmish.Program<_,_,_,_>) =
    let setState model dispatch =
      ReactDom.render(
        lazyView2With (fun x y -> obj.ReferenceEquals(x,y)) (Program.view program) model dispatch,
        element
      )

    program
    |> Program.withSetState setState

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

    let subject : Rxjs.RxSubject<'props> = Rxjs.subject

    let subscription (initial : 'model) : Cmd<'msg> =
      match reactified.PropsToMsg with
      | Some propsToMsg ->
        let sub (dispatch : 'msg -> unit) : unit =

          let callback props =
            propsToMsg props |> dispatch

          // TODO: subscribe returns an unsubscribe func
          subject.subscribe callback

        // TODO: Also subscribe to the unmount hook
        Cmd.ofSub sub

      | None ->
        Cmd.none

    reactified.Program
    |> Program.withSubscription subscription
    |> Program.withReactSynchronousOnElement el
    |> Program.withSetState setState
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
