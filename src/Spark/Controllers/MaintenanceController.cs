/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using Spark.Configuration;
using Spark.Core;
using Spark.Service;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using Spark.MetaStore;
using Spark.Engine.Extensions;
using Spark.Engine.Auxiliary;

namespace Spark.Controllers
{
    [RoutePrefix("maintenance")]
    public class MaintenanceController : ApiController
    {
        MaintenanceService service;

        public MaintenanceController()
        {
            FhirService fhirservice = Infra.Mongo.CreateService();
            service = new MaintenanceService(Infra.Mongo, fhirservice);
        }

        public HttpResponseMessage Respond(string message)
        {
            var response = new HttpResponseMessage
            {
                Content = new StringContent(
                        message,
                        Encoding.UTF8,
                        "application/json"
                    )
            };
            return response;
        }

        [HttpGet, Route("initialize")]
        public HttpResponseMessage Initialize()
        {
            try
            {
                string message = service.Initialize();
                return Respond(message);
            }
            catch (Exception e)
            {
                return Respond("Initialization failed.\n" + e.Message);

            }
        }

        [HttpGet, Route("init/{type}")]
        public HttpResponseMessage Init(string type)
        {
            string message = service.Init(type);
            return Respond(message);
        }

        [HttpGet, Route("clean")]
        public HttpResponseMessage Clean()
        {
            try
            {
                string message = service.Clean();
                return Respond(message);
            }
            catch (Exception e)
            {
                return Respond("Initialization failed.\n" + e.Message);

            }
        }

        [HttpGet, Route("status")]
        public HttpResponseMessage Status()
        {
            return Respond("Spark Initializer Controller is online");            
        }

        [HttpGet, Route("bintest")]
        public OperationOutcome BinTest()
        {
            IBlobStorage store = DependencyCoupler.Inject<IBlobStorage>();
            byte[] byteArray = Encoding.UTF8.GetBytes("Hello world!");
            MemoryStream stream = new MemoryStream(byteArray);
            store.Open();
            store.Store("maintenanceblob", stream);
            store.Close();
            return new OperationOutcome().AddMessage("Binary test completed. Blob storage is online.");
        }
    }
}
