using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Engine.Auxiliary
{
    public class LimitedStream : Stream
    {

        /// <summary>
        /// Creates a write limit on the underlying <paramref name="stream"/> of <paramref name="sizeLimitInBytes"/>, which has a default of 2048 (2kB).
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="SizeLimitInBytes"></param>
        public LimitedStream (Stream stream, long sizeLimitInBytes = 2048)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream cannot be null");
            }

            innerStream = stream;
            this.sizeLimitInBytes = sizeLimitInBytes;
        }

        private Stream innerStream = null;

        private long sizeLimitInBytes = 2048;
        public long SizeLimitInBytes
        {
            get
            {
                return sizeLimitInBytes;
            }
        }

        public override bool CanRead
        {
            get
            {
                return innerStream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return innerStream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return innerStream.CanWrite && innerStream.Length < sizeLimitInBytes;
            }
        }

        public override long Length
        {
            get
            {
                return innerStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return innerStream.Position;
            }

            set
            {
                innerStream.Position = value;
            }
        }

        public override void Flush()
        {
            innerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return innerStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            int bytesToBeAdded = Math.Min(buffer.Length - offset, count);
            if (Length + bytesToBeAdded <= sizeLimitInBytes)
            {
                innerStream.Write(buffer, offset, count);
            }
            else
            {
                throw new ArgumentOutOfRangeException("buffer", String.Format("Adding {0} bytes to the stream would exceed the size limit of {1} bytes.", bytesToBeAdded, sizeLimitInBytes));
            }
        }
    }
}
