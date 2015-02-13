/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using Spark.Config;
using Spark.Core;
using Spark.Service;
using Spark.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace Spark.Controllers
{
    [RoutePrefix("maintenance")]
    public class MaintenanceController : ApiController
    {
        FhirMaintenanceService maintenance;
        public MaintenanceController()
        {
            maintenance = Factory.GetFhirMaintenanceService();
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

                string message = maintenance.Initialize(Settings.ExamplesFile, true);
                return Respond(message);
            }
            catch (Exception e)
            {
                return Respond("Initialization failed.\n" + e.Message);

            }
        }

        [HttpGet, Route("clean")]
        public HttpResponseMessage Clean()
        {
            try
            {
                string message = maintenance.Clean();
                return Respond(message);
            }
            catch (Exception e)
            {
                return Respond("Initialization failed.\n" + e.Message);

            }
        }

        [HttpGet, Route("reset")]
        public HttpResponseMessage Initialize2()
        {
            try
            {
                string message = maintenance.Initialize(Settings.ExamplesFile, false);
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
            return new OperationOutcome().Message("Binary test completed.");
        }
    }
}
