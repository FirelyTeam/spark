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
    public class AsyncResourceXmlInputFormatter : TextInputFormatter
    {
        private readonly FhirXmlParser _parser;

        public AsyncResourceXmlInputFormatter(FhirXmlParser parser)
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
        public AsyncResourceXmlInputFormatter()
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

            try
            {
                using var reader = new StreamReader(context.HttpContext.Request.Body, Encoding.UTF8);
                var body = await reader.ReadToEndAsync();
                var resource = _parser.Parse<Resource>(body);
                context.HttpContext.AddResourceType(resource.GetType());

                return await InputFormatterResult.SuccessAsync(resource);
            }
            catch (FormatException exception)
            {
                throw Error.BadRequest($"Body parsing failed: {exception.Message}");
            }
        }
    }
}
#endif