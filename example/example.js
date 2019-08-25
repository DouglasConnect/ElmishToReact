import React, {useState} from "react";
import ReactDOM from "react-dom";
import {ExampleComponent} from "./Example.fsproj"

const WrapperComponent = () => {
  const [count, setCount] = useState(0);
  const [show, setShow] = useState(true);

  return <div>
      <p>
        <button onClick={() => setShow(!show)}>Toggle elmish component</button>
        Check the console to see the unmount message
      </p>

      { show && <ExampleComponent count={count} setCount={setCount}/> }

    </div>;
}

const element = document.getElementById("app");
ReactDOM.render(<WrapperComponent/>, element);
