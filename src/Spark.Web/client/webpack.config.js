const path = require('path');
const webpack = require('webpack');

const main = {
  entry: {
    main: './js/main.js'  // multiple entries can be added here
  },
  output: {
    path: path.resolve('../wwwroot/assets/js/'),
    filename: '[name].js'  // based on entry name, e.g. main.js
  },
  plugins: [
    new webpack.ProvidePlugin({
        $: "jquery",
        jQuery: "jquery",
        "window.jQuery": "jquery"
    })
],
  module: {
  },
  // externals are loaded via base.html and not included in the webpack bundle.
  externals: {
    //gettext: 'gettext',
  }
}


if (process.env.NODE_ENV === 'development') {
  // Create JS source maps in the dev mode
  // See https://webpack.js.org/configuration/devtool/ for more options
  options['devtool'] = 'inline-source-map';
}

module.exports = [main];