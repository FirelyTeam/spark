using System;
using System.Web.Http.Controllers;
using System.Web.Http.ValueProviders;
using System.Web.Http.ValueProviders.Providers;

namespace Spark.Infrastructure
{
    //Inspiration: http://www.strathweb.com/2013/04/asp-net-web-api-and-greedy-query-string-parameter-binding/
    public class RouteDataValuesOnlyAttribute : Attribute, IControllerConfiguration
    {
        public void Initialize(HttpControllerSettings controllerSettings, HttpControllerDescriptor controllerDescriptor)
        {
            controllerSettings.Services.Replace(typeof(ValueProviderFactory), new RouteDataValueProviderFactory());
        }
    }
}