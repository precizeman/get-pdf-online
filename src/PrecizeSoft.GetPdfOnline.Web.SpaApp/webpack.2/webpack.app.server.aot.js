﻿const webpack = require('webpack');
const merge = require('webpack-merge');
const { AotPlugin } = require('@ngtools/webpack');
const helpers = require('./helpers');
const serverConfig = require('./webpack.app.server.js');

module.exports = (env) => {
    const isDevBuild = !(env && env.prod);

    return merge(serverConfig(env), {
        entry: {
            'main-server': helpers.root('ClientApp', 'boot-server-aot.ts')
        },
        module: {
            rules: [
                {
                    test: /\.ts$/,
                    include: helpers.root('ClientApp'),
                    loader: '@ngtools/webpack'
                }
            ]
        },
        plugins: [
            new AotPlugin({
                // mainPath: helpers.root('ClientApp', 'boot-server-aot.ts'),
                entryModule: helpers.root('ClientApp', 'app', 'server-app.module#ServerAppModule'),
                /*hostReplacementPaths: {
                    'environments\\environment.ts': 'environments\\environment.ts'
                },*/
                tsConfigPath: helpers.root('ClientApp', 'tsconfig.server.aot.json'),
                skipCodeGeneration: false
            })
        ]
    })
};