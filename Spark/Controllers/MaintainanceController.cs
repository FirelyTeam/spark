/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

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
