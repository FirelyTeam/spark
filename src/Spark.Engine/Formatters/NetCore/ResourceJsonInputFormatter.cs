/* 
 * Copyright (c) 2019-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

#if NETSTANDARD2_0 || NET6_0
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;
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

            foreach (var mediaType in FhirMediaType.JsonMimeTypes)
            {
                SupportedMediaTypes.Add(mediaType);
            }
        }

        [Obsolete("This constructor is obsolete. Please use constructor with signature ctor(FhirJsonParser, ArrayPool<char>)")]
        public ResourceJsonInputFormatter()
        {
            _parser = new FhirJsonParser();
            _charPool = new JsonArrayPool(ArrayPool<char>.Shared);

            SupportedEncodings.Clear();
            SupportedEncodings.Add(Encoding.UTF8);

            foreach (var mediaType in FhirMediaType.JsonMimeTypes)
            {
                SupportedMediaTypes.Add(mediaType);
            }
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
                using TextReader streamReader = context.ReaderFactory(request.Body, encoding);
                using JsonTextReader jsonReader = new JsonTextReader(streamReader)
                {
                    DateParseHandling = DateParseHandling.None,
                    FloatParseHandling = FloatParseHandling.Decimal,
                    ArrayPool = _charPool,
                    CloseInput = false
                };

                var resource = _parser.Parse<Resource>(jsonReader);
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