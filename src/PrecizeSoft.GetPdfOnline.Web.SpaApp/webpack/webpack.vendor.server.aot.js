﻿const webpack = require('webpack');
const merge = require('webpack-merge');
const helpers = require('./helpers');
const serverConfig = require('./webpack.vendor.server.js');

module.exports = (env) => {
    const isDevBuild = !(env && env.prod);

    return merge(serverConfig(env), {
    })
};
