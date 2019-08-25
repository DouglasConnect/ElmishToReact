namespace ElmishToReact

open Fable.Core
open Elmish

module Rxjs =

  open Fable.Core.JsInterop

  [<Import("Subject", from="rxjs")>]
  let Subject : obj = jsNative

  type IPartialObserver<'a> =
    abstract next : 'a -> unit
    abstract complete : unit -> unit

  type ISubject<'a> =
    abstract next : 'a -> unit
    abstract complete : unit -> unit
    abstract subscribe : IPartialObserver<'a> -> unit

  let subject<'a> : ISubject<'a> =
    createNew Subject () :?> ISubject<'a>

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

type Externalised<'props, 'model, 'msg> =
  private { Program : Program<'props, 'model, 'msg, Fable.React.ReactElement>
            PropsToMsg : ('props -> 'msg) option
            UnmountMsg : 'msg option }

module Externalised =

  open Elmish.React
  open Fable.Core.JsInterop

  let externalise program =
    { Program = program
      PropsToMsg = None
      UnmountMsg = None }

  let withPropsMsg (msgType: 'props -> 'msg) (externalised : Externalised<'props, 'model, 'msg>) =
    { externalised with PropsToMsg = Some msgType }

  let withUnmountMsg (msgType: 'msg) (externalised : Externalised<'props, 'model, 'msg>) =
    { externalised with UnmountMsg = Some msgType }

  type Controls<'props> =
    { Update : 'props -> unit
      Unmount : unit -> unit }

  let runWith (props : 'props) (el : Browser.Types.Element ) (externalised : Externalised<'props, 'model, 'msg>) : Controls<'props> =

    let subject : Rxjs.ISubject<'props> = Rxjs.subject

    let subscription (initial : 'model) : Cmd<'msg> =
      let sub (dispatch : 'msg -> unit) : unit =

        let onNext : 'props -> unit=
          match externalised.PropsToMsg with
          | Some propsToMsg ->
            propsToMsg >> dispatch
          |  None ->
            ignore

        let onComplete : unit -> unit =
          match externalised.UnmountMsg with
          | Some msg ->
            fun () -> dispatch msg
          | None ->
            ignore

        let observer : Rxjs.IPartialObserver<'props> =
          !!{| next = onNext
               complete = onComplete |}

        subject.subscribe observer

      Cmd.ofSub sub

    externalised.Program
    |> Program.withSubscription subscription
    |> Program.withReactSynchronousOnElement el
    |> Program.runWith props

    { Update = subject.next
      Unmount = subject.complete }

module ElmishComponent =

  open Fable.React
  open Fable.React.Props

  let mapToDisposable (f : unit -> unit) : System.IDisposable =
    { new System.IDisposable
      with member this.Dispose() = f() }

  let elmishToReact (program : Externalised<'props, 'model, 'msg>) =
    FunctionComponent.Of( fun (props: 'props) ->
      let divRef : IRefValue<Browser.Types.Element option> = Hooks.useRef(None)
      let controlsRef : IRefValue<(Externalised.Controls<'props>) option> = Hooks.useRef(None)

      let subscription () =
        match divRef.current with
        | Some el ->
          let controls =
            match controlsRef.current with
            | Some c -> c
            | None -> Externalised.runWith props el program

          controlsRef.current <- Some controls
          controls.Update props

          controls.Unmount
        | None ->
          ignore

      Hooks.useEffectDisposable (subscription >> mapToDisposable)

      div [ RefValue divRef ] []
    )
