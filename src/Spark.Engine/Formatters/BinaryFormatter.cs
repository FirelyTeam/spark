/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Spark.Core;
using Spark.Engine.Core;

namespace Spark.Formatters
{
    //public class MatchBinaryPathTypeMapping : MediaTypeMapping
    //{
    //    public MatchBinaryPathTypeMapping() : base("text/plain") { }

    //    private bool isBinaryRequest(HttpRequestMessage request)
    //    {
    //        return request.RequestUri.AbsolutePath.Contains("Binary"); // todo: replace quick hack by solid solution.
    //    }

    //    public override double TryMatchMediaType(HttpRequestMessage request)
    //    {
    //        return isBinaryRequest(request) ? 1.0f : 0.0f;
    //    }
    //}

    public class BinaryFhirFormatter : FhirMediaTypeFormatter
    {
        public BinaryFhirFormatter() : base()
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue(FhirMediaType.BinaryResource));
       //     MediaTypeMappings.Add(new MatchBinaryPathTypeMapping());
        }

        public override bool CanReadType(Type type)
        {
            return type == typeof(Resource);
        }

        public override bool CanWriteType(Type type)
        {
            return type == typeof(Binary);
        }

        public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
        {
            return Task.Factory.StartNew(() =>
            {
                MemoryStream stream = new MemoryStream();
                readStream.CopyTo(stream);

                IEnumerable<string> xContentHeader;
                var success = content.Headers.TryGetValues("X-Content-Type", out xContentHeader);

                if (!success)
                {
                    throw Error.BadRequest("POST to binary must provide a Content-Type header");
                }

                string contentType = xContentHeader.FirstOrDefault();

                Binary binary = new Binary();
                binary.Content = stream.ToArray();
                binary.ContentType = contentType;

                //ResourceEntry entry = ResourceEntry.Create(binary);
                //entry.Tags = content.Headers.GetFhirTags();
                return (object)binary;
            });
        }

        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, System.Net.TransportContext transportContext)
        {
            return Task.Factory.StartNew(() =>
            {
                
                Binary binary = (Binary)value;
                //Binary binary = (Binary)entry.Resource;

                //content.Headers.ContentType = new MediaTypeHeaderValue(binary.ContentType);
                //content.Headers.Replace("Content-Type", binary.ContentType);  // todo: HACK on Binary content Type!!!

                var stream = new MemoryStream(binary.Content);

                stream.CopyTo(writeStream);

                stream.Flush();
            });
        }
    }
}