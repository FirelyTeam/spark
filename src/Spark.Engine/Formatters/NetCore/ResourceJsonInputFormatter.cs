#if NETSTANDARD2_0
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Spark.Core;
using Spark.Engine.Extensions;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Engine.Formatters
{
    public class ResourceJsonInputFormatter : TextInputFormatter
    {
        public ResourceJsonInputFormatter()
        {
            SupportedEncodings.Clear();
            SupportedEncodings.Add(Encoding.UTF8);

            SupportedMediaTypes.Add("application/json");
            SupportedMediaTypes.Add("application/fhir+json");
            SupportedMediaTypes.Add("application/json+fhir");
            SupportedMediaTypes.Add("text/json");
        }

        protected override bool CanReadType(Type type)
        {
            return typeof(Resource).IsAssignableFrom(type);
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (encoding == null) throw new ArgumentNullException(nameof(encoding));
            if (encoding != Encoding.UTF8)
                throw Error.BadRequest("FHIR supports UTF-8 encoding exclusively, not " + encoding.WebName);

            context.HttpContext.AllowSynchronousIO();

            try
            {
                using (TextReader reader = context.ReaderFactory(context.HttpContext.Request.Body, encoding))
                {
                    FhirJsonParser parser = context.HttpContext.RequestServices.GetRequiredService<FhirJsonParser>();
                    Resource resource = parser.Parse(await reader.ReadToEndAsync()) as Resource;
                    context.HttpContext.AddResourceType(resource.GetType());
                    return await InputFormatterResult.SuccessAsync(resource);
                }
            }
            catch (FormatException exception)
            {
                throw Error.BadRequest($"Body parsing failed: {exception.Message}");
            }
        }
    }
}
#endif