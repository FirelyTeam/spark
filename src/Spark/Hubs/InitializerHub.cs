using Hl7.Fhir.Model;
using Microsoft.AspNet.SignalR;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Service;
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
        private readonly int limitPerType = 50; //0 for no limit at all.

        private List<Resource> resources;

        private readonly IAsyncFhirService fhirService;
        private readonly ILocalhost localhost;
        private readonly IFhirStoreAdministration fhirStoreAdministration;
        private readonly IFhirIndex fhirIndex;
        private readonly IIndexRebuildService indexRebuildService;

        private int ResourceCount;

        public InitializerHub(
            IAsyncFhirService fhirService, 
            ILocalhost localhost, 
            IFhirStoreAdministration fhirStoreAdministration, 
            IFhirIndex fhirIndex,
            IIndexRebuildService indexRebuildService)
        {
            this.localhost = localhost;
            this.fhirService = fhirService;
            this.fhirStoreAdministration = fhirStoreAdministration;
            this.fhirIndex = fhirIndex;
            this.indexRebuildService = indexRebuildService;
            this.resources = null;
        }

        public List<Resource> GetExampleData()
        {
            var list = new List<Resource>();

            Bundle data;
            if (limitPerType == 0)
            {
                data = Examples.ImportEmbeddedZip(Settings.ExamplesFilePath).ToBundle();
            }
            else
            {
                data = Examples.ImportEmbeddedZip(Settings.ExamplesFilePath).LimitPerType(limitPerType).ToBundle();
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
                Progress = (int)10 + (idx + 1) * 90 / ResourceCount
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
                await fhirStoreAdministration.CleanAsync().ConfigureAwait(false);
                await fhirIndex.CleanAsync().ConfigureAwait(false);

                Progress("Loading examples data...", 5);
                this.resources = GetExampleData();

                var resarray = resources.ToArray();
                ResourceCount = resarray.Count();

                for (int x = 0; x <= ResourceCount - 1; x++)
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

                            await fhirService.PutAsync(key, res).ConfigureAwait(false);
                        }
                        else
                        {
                            await fhirService.CreateAsync(key, res).ConfigureAwait(false);
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
                await indexRebuildService.RebuildIndexAsync(this);
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
