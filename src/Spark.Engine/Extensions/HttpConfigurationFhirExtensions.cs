/* 
 * Copyright (c) 2014-2018, Firely (info@fire.ly)
 * Copyright (c) 2019-2024, Incendi (info@incendi.no)
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

#if NET462
using System.Web.Http;
using System.Web.Http.Validation;
using System.Net.Http.Formatting;
using System.Web.Http.ExceptionHandling;
using Spark.Filters;
using Spark.Handlers;
using Spark.Formatters;
using Spark.Engine.Maintenance;
using Spark.Core;
using Spark.Engine.ExceptionHandling;
using Hl7.Fhir.Model;
using static Hl7.Fhir.Model.ModelInfo;
using Hl7.Fhir.Serialization;
using System.Collections.Generic;

namespace Spark.Engine.Extensions
{
    public static class HttpConfigurationFhirExtensions
    {
        public static void AddFhirFormatters(this HttpConfiguration config, bool clean = true, bool permissiveParsing = true)
        {
            // remove existing formatters
            if (clean) config.Formatters.Clear();

            // Hook custom formatters
            ParserSettings parserSettings = new ParserSettings { PermissiveParsing = permissiveParsing };
            config.Formatters.Add(new XmlFhirFormatter(new FhirXmlParser(parserSettings), new FhirXmlSerializer()));
            config.Formatters.Add(new JsonFhirFormatter(new FhirJsonParser(parserSettings), new FhirJsonSerializer()));
            config.Formatters.Add(new BinaryFhirFormatter());
            config.Formatters.Add(new HtmlFhirFormatter(new FhirXmlSerializer()));

            // Add these formatters in case our own throw exceptions, at least you
            // get a decent error message from the default formatters then.
            config.Formatters.Add(new JsonMediaTypeFormatter());
            config.Formatters.Add(new XmlMediaTypeFormatter());
        }

        public static void AddFhirExceptionHandling(this HttpConfiguration config)
        {
            config.Filters.Add(new FhirExceptionFilter(new ExceptionResponseMessageFactory()));
            config.Services.Replace(typeof(IExceptionHandler), new FhirGlobalExceptionHandler(new ExceptionResponseMessageFactory()));
        }
        
        public static void AddFhirMessageHandlers(this HttpConfiguration config)
        {
            // TODO: Should compression handler be before InterceptBodyHandler.  Have not checked.
            config.MessageHandlers.Add((new CompressionHandler()));
            config.MessageHandlers.Add(new FhirMediaTypeHandler());
            config.MessageHandlers.Add(new FhirResponseHandler());
            config.MessageHandlers.Add(new FhirErrorMessageHandler());
            config.MessageHandlers.Add(new MaintenanceModeNetHttpHandler());
        }

        public static void AddCustomSearchParameters(this HttpConfiguration configuration, IEnumerable<SearchParamDefinition> searchParameters)
        {
            // Add any user-supplied SearchParameters
            SearchParameters.AddRange(searchParameters);
        }

        public static void AddFhirHttpSearchParameters(this HttpConfiguration configuration)
        {
            SearchParameters.AddRange(new []
            {
                new SearchParamDefinition { Resource = "Resource", Name = "_id", Type = SearchParamType.String, Path = new string[] { "Resource.id" } }
                , new SearchParamDefinition { Resource = "Resource", Name = "_lastUpdated", Type = SearchParamType.Date, Path = new string[] { "Resource.meta.lastUpdated" } }
                , new SearchParamDefinition { Resource = "Resource", Name = "_tag", Type = SearchParamType.Token, Path = new string[] { "Resource.meta.tag" } }
                , new SearchParamDefinition { Resource = "Resource", Name = "_profile", Type = SearchParamType.Uri, Path = new string[] { "Resource.meta.profile" } }
                , new SearchParamDefinition { Resource = "Resource", Name = "_security", Type = SearchParamType.Token, Path = new string[] { "Resource.meta.security" } }
            });
        }

        public static void AddFhir(this HttpConfiguration config, bool permissiveParsing = true, params string[] endpoints)
        {
            config.AddFhirMessageHandlers();
            config.AddFhirExceptionHandling();
            config.AddFhirHttpSearchParameters();

            // Hook custom formatters
            config.AddFhirFormatters(permissiveParsing: permissiveParsing);

            // KM: Replace DefaultContentNegotiator with FhirContentNegotiator
            // this enables serving Binaries according to specification
            // http://hl7.org/fhir/binary.html#rest
            config.Services.Replace(typeof(IContentNegotiator), new FhirContentNegotiator());

            // EK: Remove the default BodyModel validator. We don't need it,
            // and it makes the Validation framework throw a null reference exception
            // when the body empty. This only surfaces when calling a DELETE with no body,
            // while the action method has a parameter for the body.
            config.Services.Replace(typeof(IBodyModelValidator), null);
        }

    }
}
#endif