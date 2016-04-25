#region Copyright (c) Orion Health Asia Pacific Limited and the Orion Health Group of companies (2001 - 2013).

// Original author: Richard Schneider (makaretu@gmail.com)

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Filters // Original: Orchestral.Fhir.Http
{
    /// <summary>
    ///   GZip encoded <see cref="HttpContent"/>.
    /// </summary>
    /// <seealso cref="CompressionHandler"/>
    /// <seealso cref="GZipStream"/>
    public class GZipContent : HttpContent
    {
        readonly HttpContent content;

        /// <summary>
        ///   Creates a new instance of the <see cref="GZipContent"/> from the
        ///   specified <see cref="HttpContent"/>.
        /// </summary>
        /// <param name="content">
        ///   The unencoded <see cref="HttpContent"/>.
        /// </param>
        /// <remarks>
        ///   All <see cref="HttpContent.Headers"/> from the <paramref name="content"/> are copied 
        ///   and the <see cref="HttpContentHeaders.ContentEncoding"/> header is added.
        /// </remarks>
        public GZipContent(HttpContent content)
        {
            this.content = content;
            foreach (var header in content.Headers)
            {
                this.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            Headers.ContentEncoding.Add("gzip");
        }

        /// <inheritdoc />
        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }

        /// <inheritdoc />
        protected async override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            using (content)
            using (var compressedStream = new GZipStream(stream, CompressionMode.Compress, leaveOpen: true))
            {
                await content.CopyToAsync(compressedStream);
            }
        }

    }
}
