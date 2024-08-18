/* 
 * Copyright (c) 2014-2018, Firely (info@fire.ly) and contributors
 * Copyright (c) 2021-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Spark.Engine.Auxiliary;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
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
        private readonly HttpContent _content;
        private readonly long? _maxDecompressedBodySizeInBytes = null;

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
            _maxDecompressedBodySizeInBytes = maxDecompressedBodySizeInBytes;
            _content = content;
            foreach (var header in content.Headers)
            {
                Headers.TryAddWithoutValidation(header.Key, header.Value);
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
            using (_content)
            {
                var compressedStream = await _content.ReadAsStreamAsync();
                using var uncompressedStream = new GZipStream(compressedStream, CompressionMode.Decompress);
                if (_maxDecompressedBodySizeInBytes.HasValue)
                {
                    var limitedStream = new LimitedStream(stream, _maxDecompressedBodySizeInBytes.Value);
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
