/*
 * Copyright (c) 2021-2024, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.AspNetCore.SignalR;
using Tasks = System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Spark.Engine.Service.FhirServiceExtensions;

namespace Spark.Web.Hubs;

/// <summary>
/// SignalR hub is a short-living object while
/// hub context lives longer and can be used for
/// accessing Clients collection between requests.
/// </summary>
internal class HubContextProgressNotifier : IIndexBuildProgressReporter
{
    private readonly IHubContext<MaintenanceHub> _hubContext;
    private readonly ILogger<MaintenanceHub> _logger;

    private int _progress;

    public HubContextProgressNotifier(
        IHubContext<MaintenanceHub> hubContext,
        ILogger<MaintenanceHub> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Tasks.Task SendProgressUpdate(int progress, string message)
    {
        _logger.LogInformation($"[{progress}%] {message}");

        _progress = progress;

        var msg = new ImportProgressMessage
        {
            Message = message,
            Progress = progress
        };

        await _hubContext.Clients.All.SendAsync("UpdateProgress", msg);
    }

    public async Tasks.Task Progress(string message)
    {
        await SendProgressUpdate(_progress, message);
    }

    public async Tasks.Task ReportProgressAsync(int progress, string message)
    {
        await SendProgressUpdate(progress, message)
            .ConfigureAwait(false);
    }

    public async Tasks.Task ReportErrorAsync(string message)
    {
        await Progress(message)
            .ConfigureAwait(false);
    }
}