/* 
 * Copyright (c) 2016, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Spark.Engine.Auxiliary;
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

namespace Spark.Filters 
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
        public GZipCompressedContent(HttpContent content, long? maxDecompressedBodySizeInBytes = null)
        {
            this.maxDecompressedBodySizeInBytes = maxDecompressedBodySizeInBytes;
            this.content = content;
            foreach (var header in content.Headers)
            {
                this.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            Headers.ContentEncoding.Remove("gzip");
        }

        private long? maxDecompressedBodySizeInBytes = null;

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
                    if (maxDecompressedBodySizeInBytes.HasValue)
                    {
                        var limitedStream = new LimitedStream(stream, maxDecompressedBodySizeInBytes.Value);
                        await uncompressedStream.CopyToAsync(limitedStream);
                    }
                    else
                    {
                        await uncompressedStream.CopyToAsync(stream);
                    }
                }
            }
        }

    }
}
