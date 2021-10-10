// unset

using Spark.Engine.Service;
using Spark.Service;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Spark.Engine.Extensions
{
    public class FhirServiceDictionary : IDictionary<Type, Type>
    {
        private readonly IDictionary<Type, Type> _innerDictionary = new Dictionary<Type, Type>();

        public bool TryAdd<TService, TImplementation>()
            where TService : class, IAsyncFhirService
            where TImplementation : class, TService
        {
            var containsKey = ContainsKey(typeof(TService));
            if (!containsKey)
            {
                Add(typeof(TService), typeof(TImplementation));
            }

            return containsKey;
        }

        public bool TryAdd<TImplementation>()
            where TImplementation : class, IAsyncFhirService
        {
            var containsKey = ContainsKey(typeof(TImplementation));
            if (!containsKey)
            {
                Add(typeof(TImplementation), typeof(TImplementation));
            }

            return containsKey;
        }

        public void Add(KeyValuePair<Type, Type> item)
        {
            if (!typeof(IAsyncFhirService).IsAssignableFrom(item.Key))
                throw new ArgumentException($"Key must be assignable to '{typeof(IAsyncFhirService).Name}'.", nameof(item));
            if (!typeof(IAsyncFhirService).IsAssignableFrom(item.Value))
                throw new ArgumentException($"Value must be assignable to '{typeof(IAsyncFhirService).Name}'.", nameof(item));

            _innerDictionary.Add(item);
        }

        public void Add(Type key, Type value)
        {
            if (!typeof(IFhirService).IsAssignableFrom(key) && !typeof(IAsyncFhirService).IsAssignableFrom(key))
                throw new ArgumentException($"Argument '{nameof(key)}' must be assignable to '{typeof(IFhirService).Name}' or '{typeof(IAsyncFhirService).Name}'.", nameof(key));
            if (!typeof(IFhirService).IsAssignableFrom(value) && !typeof(IAsyncFhirService).IsAssignableFrom(value))
                throw new ArgumentException($"Argument {nameof(value)} must be assignable to '{typeof(IFhirService).Name}' or '{typeof(IAsyncFhirService).Name}'.", nameof(value));

            _innerDictionary.Add(key, value);
        }

        public IEnumerator<KeyValuePair<Type, Type>> GetEnumerator() => _innerDictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Clear() => _innerDictionary.Clear();

        public bool Contains(KeyValuePair<Type, Type> item) => _innerDictionary.Contains(item);

        public void CopyTo(KeyValuePair<Type, Type>[] array, int arrayIndex) => _innerDictionary.CopyTo(array, arrayIndex);

        public bool Remove(KeyValuePair<Type, Type> item) => _innerDictionary.Remove(item);

        public bool ContainsKey(Type key) => _innerDictionary.ContainsKey(key);

        public bool Remove(Type key) => _innerDictionary.Remove(key);

        public bool TryGetValue(Type key, out Type value) => _innerDictionary.TryGetValue(key, out value);

        public int Count { get => _innerDictionary.Count; }
        public bool IsReadOnly { get => _innerDictionary.IsReadOnly; }

        public ICollection<Type> Keys { get => _innerDictionary.Keys; }
        public ICollection<Type> Values { get => _innerDictionary.Values; }

        public Type this[Type key]
        {
            get => _innerDictionary[key];
            set => _innerDictionary[key] = value;
        }
    }
}