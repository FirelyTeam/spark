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

        private IAsyncFhirService _fhirService;
        private ILocalhost _localhost;
        private IFhirStoreAdministration _fhirStoreAdministration;
        private IFhirIndex _fhirIndex;
        private ExamplesSettings _examplesSettings;
        private IIndexRebuildService _indexRebuildService;
        private readonly ILogger<MaintenanceHub> _logger;
        private readonly IHubContext<MaintenanceHub> _hubContext;

        private int _resourceCount;

        public MaintenanceHub(
            IAsyncFhirService fhirService,
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

        private ImportProgressMessage Message(string message, int idx)
        {
            var msg = new ImportProgressMessage
            {
                Message = message,
                Progress = (int)10 + (idx + 1) * 90 / _resourceCount
            };
            return msg;
        }

        public async void ClearStore()
        {
            var notifier = new HubContextProgressNotifier(_hubContext, _logger);
            try
            {
                await notifier.SendProgressUpdate(0, "Clearing the database...");
                await _fhirStoreAdministration.CleanAsync().ConfigureAwait(false);
                await _fhirIndex.CleanAsync().ConfigureAwait(false);
                await notifier.SendProgressUpdate(100, "Database cleared");
            }
            catch (Exception e)
            {
                await notifier.SendProgressUpdate(100, "ERROR CLEARING :( " + e.InnerException);
            }

        }

        public async void RebuildIndex()
        {
            var notifier = new HubContextProgressNotifier(_hubContext, _logger);
            try
            {
                await _indexRebuildService.RebuildIndexAsync(notifier)
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to rebuild index");

                await notifier.SendProgressUpdate(100, "ERROR REBUILDING INDEX :( " + e.InnerException)
                    .ConfigureAwait(false);
            }
        }

        public async Tasks.Task LoadExamplesToStore()
        {
            var messages = new StringBuilder();
            var notifier = new HubContextProgressNotifier(_hubContext, _logger);
            try
            {
                await notifier.SendProgressUpdate(1, "Loading examples data...");
                _resources = GetExampleData();

                var resarray = _resources.ToArray();
                _resourceCount = resarray.Count();

                for (int x = 0; x <= _resourceCount - 1; x++)
                {
                    var res = resarray[x];
                    // Sending message:
                    var msg = Message("Importing " + res.TypeName + " " + res.Id + "...", x);
                    await notifier.SendProgressUpdate(msg.Progress, msg.Message);

                    try
                    {
                        Key key = res.ExtractKey();

                        if (res.Id != null && res.Id != "")
                        {
                            await _fhirService.PutAsync(key, res).ConfigureAwait(false);
                        }
                        else
                        {
                            await _fhirService.CreateAsync(key, res).ConfigureAwait(false);
                        }
                    }
                    catch (Exception e)
                    {
                        // Sending message:
                        var msgError = Message("ERROR Importing " + res.TypeName + " " + res.Id + "... ", x);
                        await Clients.All.SendAsync("Error", msg);
                        messages.AppendLine(msgError.Message + ": " + e.Message);
                    }


                }

                await notifier.SendProgressUpdate(100, messages.ToString());
            }
            catch (Exception e)
            {
                await notifier.Progress("Error: " + e.Message);
            }
        }
    }

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

    internal class ImportProgressMessage
    {
        public int Progress;
        public string Message;
    }
}
