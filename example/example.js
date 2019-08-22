import React from "react";
import ReactDOM from "react-dom";
import {ExampleComponent} from "./Example.fsproj"

const element = document.getElementById("app");
ReactDOM.render(<ExampleComponent label="mylabel"/>, element);
