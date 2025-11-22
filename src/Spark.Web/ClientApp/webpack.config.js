const path = require('path');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');

module.exports = (env, argv) => {
    const isProduction = argv.mode === 'production';
    
    return {
        entry: {
            main: './js/main.js',
            signalr: './js/signalr.js'
        },
        output: {
            filename: '[name].js',
            path: path.resolve(__dirname, '../wwwroot/assets/js'),
            clean: false // Don't clean the entire directory
        },
        mode: argv.mode || 'development',
        devtool: isProduction ? false : 'eval-source-map',
        optimization: {
            minimize: isProduction
        },
        module: {
            rules: [
                {
                    test: /\.css$/i,
                    use: [
                        MiniCssExtractPlugin.loader,
                        'css-loader'
                    ]
                }
            ]
        },
        plugins: [
            new MiniCssExtractPlugin({
                filename: '../css/[name].css'
            })
        ]
    };
};

