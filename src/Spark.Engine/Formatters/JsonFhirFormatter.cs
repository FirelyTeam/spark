﻿/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Newtonsoft.Json;
using Hl7.Fhir.Rest;
using Spark.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Core;

namespace Spark.Formatters
{
    public class JsonFhirFormatter : FhirMediaTypeFormatter
    {
        private readonly FhirJsonParser _parser;
        private readonly FhirJsonSerializer _serializer;

        public JsonFhirFormatter(FhirJsonParser parser, FhirJsonSerializer serializer) : base()
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

            foreach (var mediaType in ContentType.JSON_CONTENT_HEADERS)
                SupportedMediaTypes.Add(new MediaTypeHeaderValue(mediaType));
        }
        
        public override void SetDefaultContentHeaders(Type type, HttpContentHeaders headers, MediaTypeHeaderValue mediaType)
        {
            base.SetDefaultContentHeaders(type, headers, mediaType);
            headers.ContentType = FhirMediaType.GetMediaTypeHeaderValue(type, ResourceFormat.Json);
        }

        public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
        {
            try
            {
                var body = base.ReadBodyFromStream(readStream, content);

                if (typeof(Resource).IsAssignableFrom(type))
                {
                    Resource resource = _parser.Parse<Resource>(body);
                    return Task.FromResult<object>(resource);
                }
                else
                {
                    throw Error.Internal("Cannot read unsupported type {0} from body", type.Name);
                }
            }
            catch (FormatException exception)
            {
                throw Error.BadRequest("Body parsing failed: " + exception.Message);
            }
        }

        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext)
        {
            using(StreamWriter streamwriter = new StreamWriter(writeStream))
            using (JsonWriter writer = new JsonTextWriter(streamwriter))
            {
                SummaryType summary = requestMessage.RequestSummary();

                if (type == typeof(OperationOutcome))
                {
                    Resource resource = (Resource)value;
                    _serializer.Serialize(resource, writer, summary);
                }
                else if (typeof(Resource).IsAssignableFrom(type))
                {
                    Resource resource = (Resource)value;
                    _serializer.Serialize(resource, writer, summary);
                }
                else if (typeof(FhirResponse).IsAssignableFrom(type))
                {
                    FhirResponse response = (value as FhirResponse);
                    if (response.HasBody)
                    {
                        _serializer.Serialize(response.Resource, writer, summary);
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
