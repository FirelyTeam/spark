#if NETSTANDARD2_0
using Newtonsoft.Json;
using System;
using System.Buffers;

namespace Spark.Engine.Formatters
{
    internal class JsonArrayPool : IArrayPool<char>
    {
        private readonly ArrayPool<char> _inner;

        public JsonArrayPool(ArrayPool<char> inner)
        {
            if (inner == null) throw new ArgumentNullException(nameof(inner));

            _inner = inner;
        }

        public char[] Rent(int minimumLength)
        {
            return _inner.Rent(minimumLength);
        }

        public void Return(char[] array)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));

            _inner.Return(array);
        }
    }
}
#endif