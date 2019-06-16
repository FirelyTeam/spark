using Microsoft.AspNetCore.Mvc;
using Spark.Engine.Core;
using System;
using System.Text;

namespace Spark.NetCore.Controllers
{
    public class HomeController : ControllerBase
    {
        private readonly ILocalhost _localhost;

        public HomeController(ILocalhost localhost)
        {
            _localhost = localhost;
        }

        public ActionResult<string> Index()
        {
            string response = $"Endpoint: {_localhost.DefaultBase.ToString()}/fhir";

            return new ActionResult<string>(response);
        }
    }
}
