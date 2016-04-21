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
    ///   GZip compressed encoded <see cref="HttpContent"/>.
    /// </summary>
    /// <seealso cref="CompressionHandler"/>
    /// <seealso cref="GZipStream"/>
    public class GZipCompressedContent : HttpContent
    {
        readonly HttpContent content;

        /// <summary>
        ///   Creates a new instance of the <see cref="GZipCompressedContent"/> from the
        ///   specified <see cref="HttpContent"/>.
        /// </summary>
        /// <param name="content">
        ///   The compressed <see cref="HttpContent"/>.
        /// </param>
        /// <remarks>
        ///   All <see cref="HttpContent.Headers"/> from the <paramref name="content"/> are copied 
        ///   except 'Content-Encoding'.
        /// </remarks>
        public GZipCompressedContent(HttpContent content)
        {
            this.content = content;
            foreach (var header in content.Headers)
            {
                this.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            Headers.ContentEncoding.Remove("gzip");
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
            {
                var compressedStream = await content.ReadAsStreamAsync();
                using (var uncompressedStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                {
                    await uncompressedStream.CopyToAsync(stream);
                }
            }
        }

    }
}
