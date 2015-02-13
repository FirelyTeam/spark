using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Spark.Sandbox;

namespace Spark.Controllers
{
    public class TestController : ApiController
    {
        [Route("{controller}/food")]
        public string Post(IFood food)
        {
            string value = food.ToString();
            return string.Format("Food: ({0}), Response: ({1})", food.Name, value);
            // Moet de return value hiervan ook een Bundle worden?
        }

        [Route("{controller}/beverage")]
        public string Post(IBeverage bev)
        {
            string value = bev.ToString();
            return string.Format("Beverage: {0}, Response: ({1})", bev.Name, value);
            // Moet de return value hiervan ook een Bundle worden?
        }
    }
}
