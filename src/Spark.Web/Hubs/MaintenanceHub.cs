/*
 * Copyright (c) 2019-2024, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Spark.Core;
using Spark.Engine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spark.Engine.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Spark.Web.Models.Config;
using Spark.Web.Utilities;
using System.IO;
using Tasks = System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Spark.Engine.Service;
using Spark.Engine.Service.FhirServiceExtensions;

namespace Spark.Web.Hubs
{
    //[Authorize(Policy = "RequireAdministratorRole")]
    public class MaintenanceHub : Hub
    {
        private List<Resource> _resources = null;

        private IFhirService _fhirService;
        private ILocalhost _localhost;
        private IFhirStoreAdministration _fhirStoreAdministration;
        private IFhirIndex _fhirIndex;
        private ExamplesSettings _examplesSettings;
        private IIndexRebuildService _indexRebuildService;
        private readonly ILogger<MaintenanceHub> _logger;
        private readonly IHubContext<MaintenanceHub> _hubContext;

        private int _resourceCount;

        public MaintenanceHub(
            IFhirService fhirService,
            ILocalhost localhost,
            IFhirStoreAdministration fhirStoreAdministration,
            IFhirIndex fhirIndex,
            ExamplesSettings examplesSettings,
            IIndexRebuildService indexRebuildService,
            ILogger<MaintenanceHub> logger,
            IHubContext<MaintenanceHub> hubContext)
        {
            _localhost = localhost;
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
            var list = new List<Resource>();
            string examplePath = Path.Combine(AppContext.BaseDirectory, _examplesSettings.FilePath);

            Bundle data;
            data = FhirFileImport.ImportEmbeddedZip(examplePath).ToBundle();

            if (data.Entry != null && data.Entry.Count() != 0)
            {
                foreach (var entry in data.Entry)
                {
                    if (entry.Resource != null)
                    {
                        list.Add((Resource)entry.Resource);
                    }
                }
            }
            return list;
        }

        public async void ClearStore()
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

        public async void RebuildIndex()
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("UpdateProgress", "Rebuilding index...");
                await _indexRebuildService.RebuildIndexAsync()
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to rebuild index");

                await _hubContext.Clients.All.SendAsync("UpdateProgress", "ERROR REBUILDING INDEX :( ")
                    .ConfigureAwait(false);
            }
            await _hubContext.Clients.All.SendAsync("UpdateProgress", "Index rebuilt!");
        }

        public async void LoadExamplesToStore()
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("UpdateProgress", "Loading examples");
                _resources = GetExampleData();

                var resarray = _resources.ToArray();
                _resourceCount = resarray.Count();

                for (int x = 0; x <= _resourceCount - 1; x++)
                {
                    var res = resarray[x];
                    var msg = $"Importing {res.TypeName}, id {res.Id} ...";
                    await _hubContext.Clients.All.SendAsync("UpdateProgress", msg);

                    try
                    {
                        Key key = res.ExtractKey();

                        if (res.Id != null && res.Id != "")
                        {
                            await _fhirService.PutAsync(key, res);
                        }
                        else
                        {
                            await _fhirService.CreateAsync(key, res);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Failed when loading example.");
                        var msgError = $"ERROR Importing {res.TypeName.ToString()}, id {res.Id}...";
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
}
