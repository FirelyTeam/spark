/*
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Spark.Engine.Service.FhirServiceExtensions;
using System.Threading.Tasks;

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

    public Task ReportProgressAsync(int progress, string message)
    {
        _logger.LogInformation("[{Progress}%] {Message}", progress, message);
        return SendProgressUpdate(progress, message);
    }

    public Task ReportErrorAsync(string message)
    {
        _logger.LogError("[{Progress}%] {Message}", _progress, message);
        return SendProgressUpdate(_progress, message);
    }

    private Task SendProgressUpdate(int progress, string message)
    {
        _progress = progress;

        var msg = new ProgressMessage
        {
            Message = message,
            Progress = progress
        };

        return _hubContext.Clients.All.SendAsync("UpdateProgress", msg);
    }
}
