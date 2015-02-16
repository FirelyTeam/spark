using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Validation;
using System.Net.Http;
using System.Net.Http.Formatting;

using Spark.Filters;
using Spark.Handlers;
using Spark.Formatters;

namespace Spark.WebApi
{
    public class SparkWebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();

            config.MessageHandlers.Add(new InterceptBodyHandler());
            config.MessageHandlers.Add(new MediaTypeHandler());

            config.Filters.Add(new FhirExceptionFilter());

            // remove existing formatters
            config.Formatters.Clear();

            // Hook custom formatters            
            config.Formatters.Add(new XmlFhirFormatter());
            config.Formatters.Add(new JsonFhirFormatter());
            config.Formatters.Add(new BinaryFhirFormatter());
            config.Formatters.Add(new HtmlFhirFormatter());

            // Add these formatters in case our own throw exceptions, at least you
            // get a decent error message from the default formatters then.
            config.Formatters.Add(new JsonMediaTypeFormatter());
            config.Formatters.Add(new XmlMediaTypeFormatter());

            config.EnableCors();

            // EK: Remove the default BodyModel validator. We don't need it,
            // and it makes the Validation framework throw a null reference exception
            // when the body empty. This only surfaces when calling a DELETE with no body,
            // while the action method has a parameter for the body.
            config.Services.Replace(typeof(IBodyModelValidator), null);
        }


    }
}
