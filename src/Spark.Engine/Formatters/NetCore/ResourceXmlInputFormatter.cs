#if NETSTANDARD2_0
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Spark.Core;
using Spark.Engine.Extensions;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Spark.Engine.Formatters
{
    public class ResourceXmlInputFormatter : TextInputFormatter
    {
        public ResourceXmlInputFormatter()
        {
            SupportedEncodings.Clear();
            SupportedEncodings.Add(Encoding.UTF8);

            SupportedMediaTypes.Add("application/xml");
            SupportedMediaTypes.Add("application/fhir+xml");
            SupportedMediaTypes.Add("application/xml+fhir");
            SupportedMediaTypes.Add("text/xml");
            SupportedMediaTypes.Add("text/xml+fhir");
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

            HttpRequest request = context.HttpContext.Request;

            if (!request.Body.CanSeek)
            {
                request.EnableBuffering();
                await request.Body.DrainAsync(context.HttpContext.RequestAborted);
                request.Body.Seek(0L, SeekOrigin.Begin);
            }

            try
            {
                using (XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(request.Body, encoding, XmlDictionaryReaderQuotas.Max, onClose: null))
                {
                    FhirXmlParser parser = context.HttpContext.RequestServices.GetRequiredService<FhirXmlParser>();
                    var resource = parser.Parse(reader);
                    context.HttpContext.AddResourceType(resource.GetType());
                    return InputFormatterResult.Success(resource);
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