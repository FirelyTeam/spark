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
        private readonly IArrayPool<char> _charPool;

        public ResourceJsonInputFormatter(FhirJsonParser parser, ArrayPool<char> charPool)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));
            if (charPool == null) throw new ArgumentNullException(nameof(charPool));

            _parser = parser;
            _charPool = new JsonArrayPool(charPool);

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
            _charPool = new JsonArrayPool(ArrayPool<char>.Shared);

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

            var request = context.HttpContext.Request;
            if (!request.Body.CanSeek)
            {
                request.EnableBuffering();
                Debug.Assert(request.Body.CanSeek);

                await request.Body.DrainAsync(context.HttpContext.RequestAborted);
                request.Body.Seek(0L, SeekOrigin.Begin);
            }

            try
            {
                using (var streamReader = context.ReaderFactory(request.Body, encoding))
                {
                    using (var jsonReader = new JsonTextReader(streamReader))
                    {
                        jsonReader.DateParseHandling = DateParseHandling.None;
                        jsonReader.FloatParseHandling = FloatParseHandling.Decimal;
                        jsonReader.ArrayPool = _charPool;
                        jsonReader.CloseInput = false;

                        var resource = _parser.Parse<Resource>(jsonReader);
                        context.HttpContext.AddResourceType(resource.GetType());

                        return await InputFormatterResult.SuccessAsync(resource);
                    }
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