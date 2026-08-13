/*
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.AspNetCore.SignalR;
using Spark.Engine.Service.FhirServiceExtensions;
using System;
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

    private int _progress;

    public HubContextProgressNotifier(IHubContext<MaintenanceHub> hubContext)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    public Task ReportProgressAsync(int progress, string message)
    {
        return SendProgressUpdate(progress, message);
    }

    public Task ReportErrorAsync(string message)
    {
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
