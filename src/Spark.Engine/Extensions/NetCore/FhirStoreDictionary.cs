/* 
 * Copyright (c) 2021-2024, Incendi (info@incendi.no)
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Store.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Spark.Engine.Extensions
{
    public class FhirStoreDictionary : IDictionary<Type, Type>
    {
        private readonly IDictionary<Type, Type> _innerDictionary = new Dictionary<Type, Type>();

        public bool TryAdd<TService, TImplementation>()
            where TService : class, IFhirStore
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
            where TImplementation : class, IFhirStore
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
            if (!typeof(IFhirStore).IsAssignableFrom(item.Key))
                throw new ArgumentException($"Key must be assignable to '{typeof(IFhirStore).Name}'.", nameof(item));
            if (!typeof(IFhirStore).IsAssignableFrom(item.Value))
                throw new ArgumentException($"Value must be assignable to '{typeof(IFhirStore).Name}'.", nameof(item));

            _innerDictionary.Add(item);
        }

        public void Add(Type key, Type value)
        {
            if (!typeof(IFhirStore).IsAssignableFrom(key))
                throw new ArgumentException($"Key must be assignable to '{typeof(IFhirStore).Name}'.", nameof(key));
            if (!typeof(IFhirStore).IsAssignableFrom(value))
                throw new ArgumentException($"Key must be assignable to '{typeof(IFhirStore).Name}'.", nameof(value));

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