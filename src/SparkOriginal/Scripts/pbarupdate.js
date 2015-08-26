$(document).ready(function () {

    // initialize progress bar

    var progress = 0;
    $("#pbar").css("width", progress + "%").attr('aria-valuenow', progress);

    //var progress = $("#pbar").attr('aria-valuenow');
    //progress = parseInt(progress) + 40; 
    //$("#pbar").css("width", progress + "%").attr('aria-valuenow', progress);

    // initialize the connection to the server
    var progressNotifier = $.connection.initializeHub;

    // client-side sendMessage function that will be called from the server-side
    progressNotifier.client.sendMessage = function (message) {
        // update progress
        UpdateProgress(message);
    };

    // establish the connection to the server and start server-side operation
    $.connection.hub.start().done(function () {
        // call the method loadData defined in the Hub
        progressNotifier.server.loadData();
    });

});

function UpdateProgress(message) {
    // get result div
    var result = $("#resultmessage");
    // set message
    result.html(message.Message);

    var percetage = $("#percentage");
    percetage.html(message.Progress + "%");

    //var progress = $("#pbar").attr('aria-valuenow');
    var n = parseInt(message.Progress); // parseInt(progress)+1;
    $("#pbar").css("width", n + "%").attr('aria-valuenow', n);
}