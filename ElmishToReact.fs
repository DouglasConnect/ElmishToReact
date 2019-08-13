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

    // TODO: this next line is very hacky :(
    el.id <- "myid"

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
