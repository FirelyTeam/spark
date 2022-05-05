function UpdateProgress(message) {
    var output = $("#resultmessage");
    var n = parseInt(message.Progress);

    output.html("Status: <b>" + n + "%</b> " + message.Message);

    $("#pbar").css("width", n + "%").attr('aria-valuenow', n);

    if (n === 100) {
        $("#pbar").removeClass("progress-bar-striped");
    }
}

/**
 * @param {any} callback Calls on server hub after connection is initialized. Accepts single argument - server.
 */
function InitProgress(callback) {
    $(document).ready(function () {
        // initialize progress bar
        var progress = 0;
        $("#pbar").css("width", progress + "%").attr('aria-valuenow', progress);

        // initialize the connection to the server
        var progressNotifier = $.connection.initializerHub;

        // client-side sendMessage function that will be called from the server-side
        progressNotifier.client.sendMessage =
            function (message) {
                UpdateProgress(message);
            };

        // establish the connection to the server and start server-side operation
        $.connection.hub.start().done(
            function () {
                callback(progressNotifier.server);
            });

    });
}