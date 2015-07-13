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
            this.resources = ExampleData();
        }

        public List<Resource> ExampleData()
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


        public void LoadData()
        {

            var resarray = resources.ToArray();
            var rescount = resarray.Count();

            //cleans store and index
            store.Clean();
            index.Clean();

            for (int x = 0; x <= rescount - 1; x++)
            {
                //Thread.Sleep(1000);
                var res = resarray[x];
                Key key = res.ExtractKey();

                if(res.Id != null && res.Id != "")
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

            var done = new ImportProgressMessage
            {
                Message = "Import completed!",
                Progress = 100
            };

            Clients.Caller.sendMessage(done);
        }
    }
}