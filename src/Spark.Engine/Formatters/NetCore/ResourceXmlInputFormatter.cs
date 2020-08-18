#if NETSTANDARD2_0
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.WebUtilities;
using Spark.Core;
using Spark.Engine.Extensions;
using Spark.Engine.IO;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Spark.Engine.Formatters
{
    public class ResourceXmlInputFormatter : TextInputFormatter
    {
        private readonly FhirXmlParser _parser;

        public ResourceXmlInputFormatter(FhirXmlParser parser)
        {
            _parser = parser;

            SupportedEncodings.Clear();
            SupportedEncodings.Add(Encoding.UTF8);

            SupportedMediaTypes.Add("application/xml");
            SupportedMediaTypes.Add("application/fhir+xml");
            SupportedMediaTypes.Add("application/xml+fhir");
            SupportedMediaTypes.Add("text/xml");
            SupportedMediaTypes.Add("text/xml+fhir");
        }

        [Obsolete("This constructor is obsolete and will be removed in a future version.")]
        public ResourceXmlInputFormatter()
        {
            _parser = new FhirXmlParser();

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
                Debug.Assert(request.Body.CanSeek);

                await request.Body.DrainAsync(context.HttpContext.RequestAborted);
                request.Body.Seek(0L, SeekOrigin.Begin);
            }

            try
            {
                // Using a NonDisposableStream so that we do not close or dispose of HttpRequest.Body stream that we do not own.
                using (var xmlReader = XmlDictionaryReader.CreateTextReader(new NonDisposableStream(request.Body), encoding, XmlDictionaryReaderQuotas.Max, onClose: null))
                {
                    var resource = _parser.Parse(xmlReader);
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