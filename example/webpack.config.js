const path = require("path");
const webpack = require("webpack");

var babelOptions = {
    presets: [
        ["@babel/preset-env", {
            "targets": {
                "browsers": ["last 2 versions"]
            },
            "modules": false
        }], "@babel/preset-react"
    ],
    plugins: ["@babel/plugin-transform-runtime"]
};

module.exports = {
    devtool: "source-map",
    entry: './example.js',
    mode: "development",
    output: {
        path: path.join(__dirname, 'dist'),
        filename: "bundle.js"
    },
    resolve: {
        symlinks: false,
        modules: ["node_modules"]
    },
    devServer: {
        contentBase: path.join(__dirname, "public"),
        hot: true,
    },
    module: {
        rules: [
            {
                test: /\.fs(x|proj)?$/,
                use: {
                    loader: "fable-loader",
                    options: {
                        babel: babelOptions,
                        define: ["DEBUG"]
                    }
                }
            },
            {
                test: /\.js$/,
                exclude: /node_modules/,
                use: {
                    loader: 'babel-loader',
                    options: babelOptions
                },
            }
        ]
    },
    plugins: [
        new webpack.HotModuleReplacementPlugin(),
        new webpack.NamedModulesPlugin()
    ]
};
