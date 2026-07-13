/*
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Interfaces;
using Spark.Engine.Service;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Web.Models.Config;
using Spark.Web.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Task = System.Threading.Tasks.Task;

namespace Spark.Web.Hubs;

[Authorize(Roles = "Admin")]
public class MaintenanceHub : Hub<IMaintenanceHub>
{
    private readonly IFhirService _fhirService;
    private readonly IFhirStoreAdministration _fhirStoreAdministration;
    private readonly IFhirIndex _fhirIndex;
    private readonly ExamplesSettings _examplesSettings;
    private readonly IIndexRebuildService _indexRebuildService;
    private readonly ILogger<MaintenanceHub> _logger;
    private readonly IHubContext<MaintenanceHub> _hubContext;

    public MaintenanceHub(
        IFhirService fhirService,
        IFhirStoreAdministration fhirStoreAdministration,
        IFhirIndex fhirIndex,
        ExamplesSettings examplesSettings,
        IIndexRebuildService indexRebuildService,
        ILogger<MaintenanceHub> logger,
        IHubContext<MaintenanceHub> hubContext)
    {
        _fhirService = fhirService;
        _fhirStoreAdministration = fhirStoreAdministration;
        _fhirIndex = fhirIndex;
        _examplesSettings = examplesSettings;
        _indexRebuildService = indexRebuildService;
        _logger = logger;
        _hubContext = hubContext;
    }

    public List<Resource> GetExampleData()
    {
        string examplePath = Path.Combine(AppContext.BaseDirectory, _examplesSettings.FilePath);
        Bundle data = FhirFileImport.ImportEmbeddedZip(examplePath).ToBundle();

        if (data.Entry.Count == 0) return [];

        return (
                from entry in data.Entry
                where entry.Resource != null
                select entry.Resource
            ).ToList();
    }

    public async Task ClearStore()
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("UpdateProgress", "Starting clearing database...");
            await _fhirStoreAdministration.CleanAsync();

            await _hubContext.Clients.All.SendAsync("UpdateProgress", "... and cleaning indexes...");
            await _fhirIndex.CleanAsync();
            await _hubContext.Clients.All.SendAsync("UpdateProgress", "Database cleared");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to clear store.");
            await _hubContext.Clients.All.SendAsync("UpdateProgress", $"ERROR CLEARING :(");
        }
    }

    public async Task RebuildIndex()
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("UpdateProgress", "Rebuilding index...");
            await _indexRebuildService.RebuildIndexAsync();
            await _hubContext.Clients.All.SendAsync("UpdateProgress", "Index rebuilt!");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to rebuild index");
            await _hubContext.Clients.All.SendAsync("UpdateProgress", "ERROR REBUILDING INDEX :(");
        }
    }

    public async Task LoadExamplesToStore()
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("UpdateProgress", "Loading examples");

            List<Resource> resources = GetExampleData();
            foreach (Resource resource in resources)
            {
                var msg = $"Importing {resource.TypeName}, id {resource.Id} ...";
                await _hubContext.Clients.All.SendAsync("UpdateProgress", msg);

                try
                {
                    Key key = resource.ExtractKey();

                    _ = string.IsNullOrWhiteSpace(resource.Id)
                        ? await _fhirService.CreateAsync(key, resource)
                        : await _fhirService.PutAsync(key, resource);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed when loading example.");
                    var msgError = $"ERROR Importing {resource.TypeName}, id {resource.Id}...";
                    await _hubContext.Clients.All.SendAsync("UpdateProgress", msgError);
                }
            }

            await _hubContext.Clients.All.SendAsync("UpdateProgress", "Finished loading examples");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to load examples.");
            await _hubContext.Clients.All.SendAsync("UpdateProgress", "Error: " + e.Message);
        }
    }
}
