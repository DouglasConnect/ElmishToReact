# elmish-to-react

Wrap an [Elmish program](https://elmish.github.io/elmish/) as a [React component](https://reactjs.org/).

This is helpful when writing a React app that needs to render an Elmish program as a reuseable component.

## A basic example

### Step 1: Write a Elmish program in F\#

Follow the [Elmish basic instructions](https://elmish.github.io/elmish/basics.html) to build your program in F#.

```F#
let myProgram = Program.mkProgram init update view
```

### Step 2: Wrap your program into a React component

```F#
open Fable.Elmish.ElmishToReact
let MyProgramComponent = elmishToReactSimple myProgram
```

### Step 3: Start using your component

From your javascript/react code you can render your wrapped program just like any regular React component:

```JSX
const WrapperComponent = (props) =>
  <div>
    <p>Here's the wrapped program:</p>
    <MyProgramComponent/>
  </div>

ReactDOM.render(<WrapperComponent/>, element);
```

## Passing Props

In React we like to pass props into child components.  The child should re-render when there is a change in
the props it receives from the parent.

How can we pass in props to our Elmish program and trigger a re-render?

Let's rewrite our program so it is ready to receive props. The props includes a `label` string and a `saySomething`
function is used to pass information from the child back up the parent.

```F#
type Props =
  abstract label : string
  abstract saySomething : string -> unit

type Model =
  { Props : Props
    // ...plus any other model attributes you need
  }

type Msg =
  | UpdateProps of Props
  // ...plus any other Msg types you need

let init props =
  { Props = props }, Cmd.none

let update msg model =
  match msg with
  | UpdateProps props ->
      { model with Props = props }, Cmd.none

let program = Program.mkProgram init update view
```

Next we "externalise" our program. This allows us to externally feed a props message into the Elmish program:

```F#
open Fable.Elmish.ElmishToReact

let externalisedProgram =
  Externalised.externalise program
  |> Externalised.withPropsMsg UpdateProps

let MyProgramComponent = elmishToReact externalisedProgram
```

In our javascript code we can pass the props into our wrapped program:

```JSX
ReactDom.render(
    <MyProgramComponent label="Hello, world"
                        saySomething={(msg) => console.log(msg)}/>,
  element)
```

## Unmounting

React might need to repeatedly mount and unmount the Elmish program as the user navigates around a
single page app. The Elmish program might want to clean-up its resources each time it is unmounted.

Let's rewrite our program so it can receive an unmount message:

```F#
type Msg =
  | Unmount
  // ...plus any other Msg types you need

let update msg model =
  match msg with
  | Unmount ->
      // Do any clean-up here
      model, Cmd.none

let program = Program.mkProgram init update view
```

Next we add the Unmount message to our externalised program.

```F#
open Fable.Elmish.ElmishToReact

let externalisedProgram =
  Externalised.externalise program
  |> Externalised.withUnmountMsg Unmount

let MyProgramComponent = ElmishComponent.elmishToReact externalisedProgram
```

## A complete example

`Example.fs` contains a example of an Elmish program demonstrating all the features described above.

`example.js` is an example of calling the elmish program with react syntax.

To run the example....

    yarn run start

...and open [http://localhost:8080](http://localhost:8080) in your web browser

## Installation with npm

    npm install --save-dev elmish-to-react
    dotnet add reference ./node_modules/elmish-to-react/Fable.Elmish.ElmishToReact.fsproj
    dotnet restore

## Installation with paket

    paket add nuget Fable.Elmish.ElmishToReact -i
