using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Web.Http;
using Spark.Core;
using Spark.Engine.Interfaces;

namespace Spark.Controllers
{
    [RoutePrefix("MaintenanceApi")]
    public class MaintenanceApiController : ApiController
    {
        private readonly IFhirStoreAdministration _fhirStoreAdministration;
        private readonly IFhirIndex _fhirIndex;

        public MaintenanceApiController(IFhirStoreAdministration fhirStoreAdministration, IFhirIndex fhirIndex)
        {
            _fhirStoreAdministration = fhirStoreAdministration;
            _fhirIndex = fhirIndex;
        }

        [HttpDelete, Route("All")]
        public async Task ClearAll(Guid access)
        {
            string code = ConfigurationManager.AppSettings.Get("clearAllCode");
            if (!string.IsNullOrEmpty(code) && access.ToString() == code)
            {
                await _fhirStoreAdministration.CleanAsync().ConfigureAwait(false);
                await _fhirIndex.CleanAsync().ConfigureAwait(false);
            }
        }
    }
}