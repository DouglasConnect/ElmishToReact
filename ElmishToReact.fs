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
            UnmountMsg : 'msg option }

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
      UnmountMsg = None }

  let withPropsMsg (msgType: 'props -> 'msg) (reactified : Reactified<'props, 'model, 'msg>) =
    { reactified with PropsToMsg = Some msgType }

  let withUnmountMsg (msgType: 'msg) (reactified : Reactified<'props, 'model, 'msg>) =
    { reactified with UnmountMsg = Some msgType }

  type Controls<'props> =
    { Update : 'props -> unit
      Unmount : unit -> unit }

  let runWith (props : 'props) (el : Browser.Types.Element ) (reactified : Reactified<'props, 'model, 'msg>) : Controls<'props> =

    let updateSubject : Rxjs.RxSubject<'props> = Rxjs.subject
    let unmountSubject : Rxjs.RxSubject<unit> = Rxjs.subject

    let subscription (initial : 'model) : Cmd<'msg> =
      let sub (dispatch : 'msg -> unit) : unit =

        match reactified.PropsToMsg with
        | Some propsToMsg ->

            let callback props =
              propsToMsg props |> dispatch

            // TODO: subscribe returns an unsubscribe func
            updateSubject.subscribe callback
        | None -> ()

        match reactified.UnmountMsg with
        | Some msg ->
          // TODO: subscribe returns an unsubscribe func
          unmountSubject.subscribe (fun () -> dispatch msg)
        | None -> ()

      Cmd.ofSub sub

    reactified.Program
    |> Program.withSubscription subscription
    |> Program.withReactSynchronousOnElement el
    |> Program.runWith props

    { Update = updateSubject.next
      Unmount = unmountSubject.next }

module ElmishComponent =

  open Fable.React
  open Fable.React.Props

  let elmishToReact (program : Reactified<'props, 'model, 'msg>) =
    FunctionComponent.Of( fun (props: 'props) ->
      let divRef : IRefValue<Browser.Types.Element option> = Hooks.useRef(None)
      let controlsRef : IRefValue<(Reactified.Controls<'props>) option> = Hooks.useRef(None)

      let subscription () =
        match divRef.current with
        | Some el ->
          let controls =
            match controlsRef.current with
            | Some c -> c
            | None -> Reactified.runWith props el program

          controlsRef.current <- Some controls
          controls.Update props
        | None -> ()

      Hooks.useEffect subscription

      div [ RefValue divRef ] []
    )
