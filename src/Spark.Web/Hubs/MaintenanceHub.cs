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
      data = FhirFileImport.ImportEmbeddedZip(examplePath).ToBundle(_localhost.DefaultBase);

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

    public async Task ClearStore()
    {
      try
      {
        await Clients.All.SendAsync("UpdateProgress", "Starting clearing database...");
        _fhirStoreAdministration.Clean();
        _fhirIndex.Clean();
        await Clients.All.SendAsync("UpdateProgress", "Database cleared");
      }
      catch (Exception e)
      {
        await Clients.All.SendAsync("UpdateProgress", "ERROR CLEARING :( " + e.InnerException.Message);
      }

    }

    public async Task RebuildIndex()
    {
      try
      {
        await Clients.All.SendAsync("UpdateProgress", "Rebuilding index...");
        await _indexRebuildService.RebuildIndexAsync()
            .ConfigureAwait(false);
      }
      catch (Exception e)
      {
        _logger.LogError(e, "Failed to rebuild index");

        await Clients.All.SendAsync("UpdateProgress", "ERROR REBUILDING INDEX :( " + e.InnerException.Message)
            .ConfigureAwait(false);
      }
    }

    public async Task LoadExamplesToStore()
    {
      try
      {
        await Clients.All.SendAsync("UpdateProgress", "Loading examples");
        _resources = GetExampleData();

        var resarray = _resources.ToArray();
        _resourceCount = resarray.Count();

        for (int x = 0; x <= _resourceCount - 1; x++)
        {
          var res = resarray[x];
          // Sending message:
          var msg = $"Importing {res.ResourceType.ToString()} {res.Id} ...";
          await Clients.All.SendAsync("UpdateProgress", msg);

          try
          {
            Key key = res.ExtractKey();

            if (res.Id != null && res.Id != "")
            {
              _fhirService.Put(key, res);
            }
            else
            {
              _fhirService.Create(key, res);
            }
          }
          catch (Exception e)
          {
            var msgError = $"ERROR Importing {res.ResourceType.ToString()} {res.Id}... {e.InnerException.Message}";
            await Clients.All.SendAsync("UpdateProgress", msgError);
          }
        }
      }
      catch (Exception e)
      {
        await Clients.All.SendAsync("UpdateProgress", "Error: " + e.Message);
      }
    }
  }
}
