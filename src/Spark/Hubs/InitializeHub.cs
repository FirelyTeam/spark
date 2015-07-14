using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Spark.Service;
using Hl7.Fhir.Model;
using Spark.Configuration;
using Hl7.Fhir.Serialization;
using Spark.Embedded;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Support;
using Spark.Engine.Extensions;

namespace Spark.Hubs
{
    public class ImportProgressMessage
    {
        public int Progress;
        public string Message;
    }

    public class InitializeHub : Hub
    {
        private FhirService service;
        private List<Resource> resources;
        private ILocalhost localhost;
        private IFhirStore store;
        private IFhirIndex index;

        public InitializeHub()
        {
            this.localhost = Infra.Mongo.Localhost;
            this.service = Infra.Mongo.CreateService();
            this.store = Infra.Mongo.Store;
            this.index = Infra.Mongo.Index;
            this.resources = null;
        }

        public List<Resource> GetExampleData()
        {
            var list = new List<Resource>();


            var parser = new FhirParser();
            //string xml = Resources.Wards;
            Bundle data = Examples.ImportEmbeddedZip().LimitPerType(5).ToBundle(localhost.Base); //(Bundle)FhirParser.ParseFromXml(xml);

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

        public void LoadData()
        {
            try
            {
                //cleans store and index
                Progress("Cleaning", 0);
                store.Clean();
                index.Clean();

                Progress("Loading data...");
                this.resources = GetExampleData();

                var resarray = resources.ToArray();
                var rescount = resarray.Count();

                for (int x = 0; x <= rescount - 1; x++)
                {
                    //Thread.Sleep(1000);
                    var res = resarray[x];
                    Key key = res.ExtractKey();

                    if (res.Id != null && res.Id != "")
                    {

                        service.Put(key, res);
                    }
                    else
                    {
                        service.Create(key, res);
                    }


                    // Sending message:

                    var msg = new ImportProgressMessage
                    {
                        Message = "Importing " + res.ResourceType.ToString() + " " + res.Id + "...",
                        Progress = (int)(x + 1) * 100 / rescount
                    };

                    Clients.Caller.sendMessage(msg);
                }

                Progress("Import completed!", 100);
            }
            catch (Exception e)
            {
                Progress("Error: " + e.Message);
            }
        }
    }
}