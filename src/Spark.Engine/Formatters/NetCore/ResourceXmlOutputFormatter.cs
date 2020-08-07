#if NETSTANDARD2_0
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Spark.Engine.Formatters
{
    public class ResourceXmlOutputFormatter : TextOutputFormatter
    {
        public ResourceXmlOutputFormatter()
        {
            SupportedEncodings.Clear();
            SupportedEncodings.Add(Encoding.UTF8);

            SupportedMediaTypes.Add("application/xml");
            SupportedMediaTypes.Add("application/fhir+xml");
            SupportedMediaTypes.Add("application/xml+fhir");
            SupportedMediaTypes.Add("text/xml");
            SupportedMediaTypes.Add("text/xml+fhir");
            SupportedMediaTypes.Add("application/problem+xml");
        }

        protected override bool CanWriteType(Type type)
        {
            return 
                typeof(Resource).IsAssignableFrom(type) 
                || typeof(FhirResponse).IsAssignableFrom(type)
                || typeof(ValidationProblemDetails).IsAssignableFrom(type);
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (selectedEncoding == null) throw new ArgumentNullException(nameof(selectedEncoding));
            if (selectedEncoding != Encoding.UTF8) throw Error.BadRequest($"FHIR supports UTF-8 encoding exclusively, not {selectedEncoding.WebName}");

            context.HttpContext.AllowSynchronousIO();

            using (TextWriter writer = context.WriterFactory(context.HttpContext.Response.Body, selectedEncoding))
            using (XmlWriter xmlWriter = new XmlTextWriter(writer))
            {
                if (!(context.HttpContext.RequestServices.GetService(typeof(FhirXmlSerializer)) is FhirXmlSerializer serializer))
                    throw Error.Internal($"Missing required dependency '{nameof(FhirXmlSerializer)}'");

                SummaryType summaryType = context.HttpContext.Request.RequestSummary();
                if (typeof(FhirResponse).IsAssignableFrom(context.ObjectType))
                {
                    FhirResponse response = context.Object as FhirResponse;

                    context.HttpContext.Response.AcquireHeaders(response);
                    context.HttpContext.Response.StatusCode = (int)response.StatusCode;

                    if (response.Resource != null)
                        serializer.Serialize(response.Resource, xmlWriter, summaryType);
                }
                else if (context.ObjectType == typeof(OperationOutcome) || typeof(Resource).IsAssignableFrom(context.ObjectType))
                {
                    if (context.Object != null)
                        serializer.Serialize(context.Object as Resource, xmlWriter, summaryType);
                }
                else if(context.Object is ValidationProblemDetails validationProblems)
                {
                    OperationOutcome outcome = new OperationOutcome();
                    outcome.AddValidationProblems(context.HttpContext.GetResourceType(), (HttpStatusCode)context.HttpContext.Response.StatusCode, validationProblems);
                    serializer.Serialize(outcome, xmlWriter, summaryType);
                }
            }

            return Task.CompletedTask;
        }
    }
}
#endif