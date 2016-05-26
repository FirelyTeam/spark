using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Spark.Engine.Store.Interfaces;

namespace Spark.Engine.Storage
{
    public class ExtendableWith<T> : IExtendableWith<T>, IEnumerable<T>
    {
        private Dictionary<Type, T> extensions; 
        public ExtendableWith()
        {
            extensions = new Dictionary<Type, T>();
        }
        public void AddExtension<TV>(TV extension) where TV : T
        {
            foreach (var interfaceType in extension.GetType().GetInterfaces().Where(i => typeof(T).IsAssignableFrom(i)))
            {
                extensions[interfaceType] = extension;
            }
        }

        public void RemoveExtension<TV>() where TV : T
        {
            extensions.Remove(typeof (TV));
        }

        public void RemoveExtension(Type type)
        {
            extensions.Remove(type);
        }

        public T FindExtension(Type type)
        {
            var key = extensions.Keys.SingleOrDefault(k =>type.IsAssignableFrom(k));
            if (key != null)
                return extensions[key];

            return default(T);
        }

        public TV FindExtension<TV>() where TV : T
        {
            if (extensions.ContainsKey(typeof (TV)))
                return (TV)extensions[typeof (TV)];
            return default(TV);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return extensions.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}