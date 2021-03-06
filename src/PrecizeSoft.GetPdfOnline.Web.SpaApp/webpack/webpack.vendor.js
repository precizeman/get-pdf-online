﻿const webpack = require('webpack');
const CompressionPlugin = require('compression-webpack-plugin');
const helpers = require('./helpers');

module.exports = (env) => {
    const isDevBuild = !(env && env.prod);

    return {
        stats: { modules: false },
        resolve: { extensions: ['.js'] },
        module: {
            rules: [
                /*{
                    test: /\.(png|woff|woff2|eot|ttf|svg)(\?|$)/,
                    use: 'url-loader?limit=100000'
                }*/
                {
                    test: /\.(png|jpe?g|gif|svg|woff|woff2|ttf|eot|ico)$/,
                    loader: 'file-loader?name=assets/[name].[hash].[ext]'
                }
            ]
        },
        output: {
            publicPath: '/dist/',
            filename: '[name].js',
            library: '[name]_[hash]'
        },
        plugins: [
            new webpack.ProvidePlugin({ $: 'jquery', jQuery: 'jquery' }) // Maps these identifiers to the jQuery package (because Bootstrap expects it to be a global variable)
            // new webpack.IgnorePlugin(/^vertx$/) // Workaround for https://github.com/stefanpenner/es6-promise/issues/100
        ].concat(isDevBuild ? [] : [
            // Plugins that apply in production builds only
            new webpack.NoEmitOnErrorsPlugin(),
            new webpack.optimize.UglifyJsPlugin({
                // https://github.com/angular/angular/issues/10618
                // mangle: {
                //   keep_fnames: true
                // },
                output: {
                    comments: false
                }//,
                //sourceMap: false
            }),
            new CompressionPlugin({
                asset: "[path].gz[query]",
                algorithm: "gzip",
                test: /\.(js|html|css)$/,
                threshold: 10240,
                minRatio: 0.8
            })
        ])
    };
};
