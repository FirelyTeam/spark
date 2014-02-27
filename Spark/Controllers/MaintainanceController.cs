using Hl7.Fhir.Model;
using Spark.Config;
using Spark.Service;
using Spark.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Spark.Controllers
{
    [RoutePrefix("maintainance")]
    public class MaintainanceController : ApiController
    {
        FhirMaintainanceService maintainance;
        public MaintainanceController()
        {
            maintainance = Factory.GetFhirMaintainceService();
        }

        [HttpGet, Route("initialize")]
        public OperationOutcome Initialize()
        {
            return maintainance.Initialize();
        }

    }
}
