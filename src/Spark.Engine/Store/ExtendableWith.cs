/* 
 * Copyright (c) 2016-2018, Firely (info@fire.ly)
 * Copyright (c) 2021-2024, Incendi (info@incendi.no)
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Spark.Engine.Store.Interfaces;

namespace Spark.Engine.Storage
{
    public class ExtendableWith<T> : IExtendableWith<T>, IEnumerable<T>
    {
        private readonly Dictionary<Type, T> _extensions; 

        public ExtendableWith()
        {
            _extensions = new Dictionary<Type, T>();
        }

        public void AddExtension<TV>(TV extension) where TV : T
        {
            foreach (var interfaceType in extension.GetType().GetInterfaces().Where(i => typeof(T).IsAssignableFrom(i)))
            {
                _extensions[interfaceType] = extension;
            }
        }

        public void RemoveExtension<TV>() where TV : T
        {
            _extensions.Remove(typeof (TV));
        }

        public void RemoveExtension(Type type)
        {
            _extensions.Remove(type);
        }

        public T FindExtension(Type type)
        {
            var key = _extensions.Keys.SingleOrDefault(k =>type.IsAssignableFrom(k));
            if (key != null)
                return _extensions[key];

            return default;
        }

        public TV FindExtension<TV>() where TV : T
        {
            if (_extensions.ContainsKey(typeof (TV)))
                return (TV)_extensions[typeof (TV)];
            
            return default;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _extensions.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}