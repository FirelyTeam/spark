$(document).ready(function ()
{
    // initialize progress bar
    var progress = 0;
    $("#pbar").css("width", progress + "%").attr('aria-valuenow', progress);

    // initialize the connection to the server
    var progressNotifier = $.connection.initializeHub;

    // client-side sendMessage function that will be called from the server-side
    progressNotifier.client.sendMessage =
        function (message)
        {
            UpdateProgress(message);
        };

    // establish the connection to the server and start server-side operation
    $.connection.hub.start().done(
        function ()
        {
            progressNotifier.server.loadData();
        });

});

function UpdateProgress(message)
{
    var output = $("#resultmessage");
    n = parseInt(message.Progress);

    output.html("Status: <b>" + n + "%</b> " + message.Message);

    //var percetage = $("#percentage");
    //percetage.html(message.Progress + "%");

    $("#pbar").css("width", n + "%").attr('aria-valuenow', n);

    if (n == 100)
    {
        $("#pbar").removeClass("progress-bar-striped");
    }
}