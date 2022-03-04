using Hl7.Fhir.Model;
using Microsoft.AspNet.SignalR;
using Spark.Core;
using Spark.Engine.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Tasks = System.Threading.Tasks;
using Spark.Engine.Interfaces;
using Spark.Engine.Service;
using Spark.Engine.Service.FhirServiceExtensions;

namespace Spark.Import
{
    public class ImportProgressMessage
    {
        public int Progress;
        public string Message;
    }

    public class InitializerHub : Hub, IIndexBuildProgressReporter
    {
        private int _limitPerType = 50; //0 for no limit at all.

        private List<Resource> _resources;

        private readonly IAsyncFhirService _fhirService;
        private readonly ILocalhost _localhost;
        private readonly IFhirStoreAdministration _fhirStoreAdministration;
        private readonly IAsyncFhirIndex _fhirIndex;
        private readonly IIndexRebuildService _indexRebuildService;

        private int _resourceCount;

        public InitializerHub(
            IAsyncFhirService fhirService, 
            ILocalhost localhost, 
            IFhirStoreAdministration fhirStoreAdministration, 
            IAsyncFhirIndex fhirIndex,
            IIndexRebuildService indexRebuildService)
        {
            _localhost = localhost;
            _fhirService = fhirService;
            _fhirStoreAdministration = fhirStoreAdministration;
            _fhirIndex = fhirIndex;
            _indexRebuildService = indexRebuildService;
            _resources = null;
        }

        public List<Resource> GetExampleData()
        {
            var list = new List<Resource>();

            Bundle data;
            if (_limitPerType == 0)
            {
                data = Examples.ImportEmbeddedZip(Settings.ExamplesFilePath).ToBundle();
            }
            else
            {
                data = Examples.ImportEmbeddedZip(Settings.ExamplesFilePath).LimitPerType(_limitPerType).ToBundle();
            }

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

        private int _progress = 0;

        private void Progress(string message, int progress)
        {
            Trace.TraceInformation($"[{progress}%] {message}");

            _progress = progress;

            var msg = new ImportProgressMessage
            {
                Message = message,
                Progress = progress
            };

            Clients.Caller.sendMessage(msg);
        }

        private void Progress(string message)
        {
            Progress(message, _progress);
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
        public async Tasks.Task LoadData()
        {
            var messages = new StringBuilder();
            messages.AppendLine("Import completed!");
            try
            {
                //cleans store and index
                Progress("Clearing the database...", 0);
                await _fhirStoreAdministration.CleanAsync().ConfigureAwait(false);
                await _fhirIndex.CleanAsync().ConfigureAwait(false);

                Progress("Loading examples data...", 5);
                _resources = GetExampleData();

                var resarray = _resources.ToArray();
                _resourceCount = resarray.Count();

                for (int x = 0; x <= _resourceCount - 1; x++)
                {
                    var res = resarray[x];
                    // Sending message:
                    var msg = Message("Importing " + res.TypeName + " " + res.Id + "...", x);
                    Clients.Caller.sendMessage(msg);

                    try
                    {
                        //Thread.Sleep(1000);
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
                        Clients.Caller.sendMessage(msg);
                        messages.AppendLine(msgError.Message + ": " + e.Message);
                    }


                }

                Progress(messages.ToString(), 100);
            }
            catch (Exception e)
            {
                Progress("Error: " + e.Message);
            }
        }

        public async Tasks.Task RebuildIndex()
        {
            try
            {
                _progress = 0;
                await _indexRebuildService.RebuildIndexAsync(this);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());

                Clients.Caller.sendMessage(new ImportProgressMessage
                {
                    Message = "Failed to rebuild index",
                    Progress = 100
                });
            }
        }

        public Tasks.Task ReportProgressAsync(int progress, string message)
        {
            Progress(message, progress);
            return Tasks.Task.CompletedTask;
        }

        public Tasks.Task ReportErrorAsync(string message)
        {
            return ReportProgressAsync(_progress, message);
        }
    }
}
