using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Spark.Controllers
{
    // This is a dummy controller to test if a controller in a different project than the start-up project is published.

    [RoutePrefix("drink")]
    public class DrinkController : ApiController
    {
        [HttpGet, Route("coffee")]
        public Resource Coffee()
        {
            Patient patient = new Patient();
            patient.BirthDate = DateTimeOffset.Now.ToString();
            return patient;
        }

        [HttpGet, Route("tea")]
        public Resource Tea()
        {
            Organization org = new Organization();
            org.Name = "Furore";
            return org;
        }

    }
}
