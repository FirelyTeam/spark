/* 
 * Copyright (c) 2020-2024, Incendi (info@incendi.no)
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Spark.Engine.IO
{
    internal class NonDisposableStream : Stream
    {
        private readonly Stream _innerStream;

        public NonDisposableStream(Stream innerStream)
        {
            _innerStream = innerStream;
        }

        /// <inheritdoc/>
        public override bool CanRead => _innerStream.CanRead;

        /// <inheritdoc/>
        public override bool CanSeek => _innerStream.CanSeek;

        /// <inheritdoc/>
        public override bool CanWrite => _innerStream.CanWrite;

        /// <inheritdoc/>
        public override long Length => _innerStream.Length;

        /// <inheritdoc/>
        public override long Position { get => _innerStream.Position; set => _innerStream.Position = value; }

        /// <inheritdoc/>
        public override void Flush()
        {
            
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return _innerStream.Read(buffer, offset, count);
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return _innerStream.Seek(offset, origin);
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            _innerStream.SetLength(value);
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            _innerStream.Write(buffer, offset, count);
        }

        /// <inheritdoc/>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        /// <inheritdoc/>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _innerStream.BeginRead(buffer, offset, count, callback, state);
        }

        /// <inheritdoc/>
        public override int EndRead(IAsyncResult asyncResult)
        {
            return _innerStream.EndRead(asyncResult);
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _innerStream.BeginWrite(buffer, offset, count, callback, state);
        }

        /// <inheritdoc/>
        public override void EndWrite(IAsyncResult asyncResult)
        {
            _innerStream.EndWrite(asyncResult);
        }

        /// <inheritdoc/>
        public override bool CanTimeout => _innerStream.CanTimeout;

        /// <inheritdoc/>
        public override void Close()
        {
            // Don't want to close the underlying stream, therefore we not doing anything here
        }

        /// <inheritdoc/>
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return _innerStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            // Don't want to close the underlying stream, therefore we not doing anything here
        }

        /// <inheritdoc/>
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _innerStream.FlushAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public override int ReadByte()
        {
            return _innerStream.ReadByte();
        }

        /// <inheritdoc/>
        public override void WriteByte(byte value)
        {
            _innerStream.WriteByte(value);
        }

        /// <inheritdoc/>
        public override int ReadTimeout { get => _innerStream.ReadTimeout; set => _innerStream.ReadTimeout = value; }
    }
}
