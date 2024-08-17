/* 
 * Copyright (c) 2014-2018, Furore (info@furore.com) and contributors
 * Copyright (c) 2021-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Spark.Filters
{
    /// <summary>
    ///   GZip encoded <see cref="HttpContent"/>.
    /// </summary>
    /// <seealso cref="CompressionHandler"/>
    /// <seealso cref="GZipStream"/>
    public class GZipContent : HttpContent
    {
        private readonly HttpContent _content;

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
            _content = content;
            foreach (var header in content.Headers)
            {
                Headers.TryAddWithoutValidation(header.Key, header.Value);
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
            using (_content)
            using (var compressedStream = new GZipStream(stream, CompressionMode.Compress, leaveOpen: true))
            {
                await _content.CopyToAsync(compressedStream);
            }
        }
    }
}
