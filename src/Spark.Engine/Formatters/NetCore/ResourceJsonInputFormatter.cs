#if NETSTANDARD2_0
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Spark.Core;
using Spark.Engine.Extensions;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Spark.Engine.Formatters
{
    public class ResourceJsonInputFormatter : TextInputFormatter
    {
        private readonly FhirJsonParser _parser;

        public ResourceJsonInputFormatter(FhirJsonParser parser, ArrayPool<char> charPool)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));
            if (charPool == null) throw new ArgumentNullException(nameof(charPool));

            _parser = parser;

            SupportedEncodings.Clear();
            SupportedEncodings.Add(Encoding.UTF8);

            SupportedMediaTypes.Add("application/json");
            SupportedMediaTypes.Add("application/fhir+json");
            SupportedMediaTypes.Add("application/json+fhir");
            SupportedMediaTypes.Add("text/json");
        }

        [Obsolete("This constructor is obsolete and will be removed in a future version.")]
        public ResourceJsonInputFormatter()
        {
            _parser = new FhirJsonParser();

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