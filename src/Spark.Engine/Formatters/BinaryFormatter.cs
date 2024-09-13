/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using Tasks = System.Threading.Tasks;
using Spark.Core;
using Spark.Engine.Core;

namespace Spark.Formatters;

public class BinaryFhirFormatter : FhirMediaTypeFormatter
{
    public BinaryFhirFormatter() : base()
    {
        SupportedMediaTypes.Add(new MediaTypeHeaderValue(FhirMediaType.OctetStreamMimeType));
    }

    public override bool CanReadType(Type type)
    {
        return type == typeof(Resource);
    }

    public override bool CanWriteType(Type type)
    {
        return type == typeof(Binary) || type == typeof(FhirResponse);
    }

    public override Tasks.Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
    {
        var success = content.Headers.TryGetValues("X-Content-Type", out IEnumerable<string> contentHeaders);
        if (!success)
        {
            throw Error.BadRequest("POST to binary must provide a Content-Type header");
        }

        string contentType = contentHeaders.FirstOrDefault();
        MemoryStream stream = new MemoryStream();
        readStream.CopyTo(stream);
        Binary binary = new Binary
        {
            Data = stream.ToArray(),
            ContentType = contentType
        };

        return Tasks.Task.FromResult((object)binary);
    }

    public override Tasks.Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, System.Net.TransportContext transportContext)
    {
        Binary binary = (Binary)value;
        var stream = new MemoryStream(binary.Data);
        content.Headers.ContentType = new MediaTypeHeaderValue(binary.ContentType);
        stream.CopyTo(writeStream);
        stream.Flush();

        return Tasks.Task.CompletedTask;
    }
}
