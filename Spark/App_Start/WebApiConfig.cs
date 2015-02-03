/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Net.Http.Formatting;
using Spark.Filters;
using System.Web.Mvc;
using System.Web.Http.Validation.Providers;
using System.Web.Http.Validation;


namespace Spark
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.MessageHandlers.Add(new InterceptBodyHandler());
            config.MessageHandlers.Add(new MediaTypeHandler());
            
            config.Filters.Add(new FhirExceptionFilter());
            config.MapHttpAttributeRoutes();
            
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
