using System.Web.Http;
using System.Web.Http.Cors;
using Spark.Core;
using Spark.Engine.Interfaces;

namespace Spark.Controllers
{
    [RoutePrefix("MaintenanceApi"), EnableCors("*", "*", "*", "*")]
    public class MaintenanceApiController : ApiController
    {
        private IFhirStoreAdministration fhirStoreAdministration;
        private IFhirIndex fhirIndex;

        public MaintenanceApiController(IFhirStoreAdministration fhirStoreAdministration, IFhirIndex fhirIndex)
        {
            this.fhirStoreAdministration = fhirStoreAdministration;
            this.fhirIndex = fhirIndex;
        }
        [HttpDelete, Route("ClearAll")]
        public void ClearAll()
        {
            fhirStoreAdministration.Clean();
            fhirIndex.Clean();
        }
    }
}