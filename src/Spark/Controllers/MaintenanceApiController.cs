using System;
using System.Configuration;
using System.Web.Http;
using System.Web.Http.Cors;
using Spark.Core;
using Spark.Engine.Interfaces;

namespace Spark.Controllers
{
    [RoutePrefix("MaintenanceApi")]
    public class MaintenanceApiController : ApiController
    {
        private IFhirStoreAdministration fhirStoreAdministration;
        private IFhirIndex fhirIndex;

        public MaintenanceApiController(IFhirStoreAdministration fhirStoreAdministration, IFhirIndex fhirIndex)
        {
            this.fhirStoreAdministration = fhirStoreAdministration;
            this.fhirIndex = fhirIndex;
        }

        [HttpDelete, Route("All")]
        public void ClearAll(Guid access)
        {
            string code = ConfigurationManager.AppSettings.Get("clearAllCode");
            if (!string.IsNullOrEmpty(code) && access.ToString() == code)
            {
                fhirStoreAdministration.Clean();
                fhirIndex.Clean();
            }
        }
    }
}