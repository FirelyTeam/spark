using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Engine.Storage
{
    public abstract class ExtendableWith<T> : IEnumerable<T>
    {
        private readonly Dictionary<Type, T> extensions = new Dictionary<Type, T>();

        protected void AddExtension<TV>(TV extension)
            where TV : T
        {
            foreach (var interfaceType in extension.GetType().GetInterfaces().Where(i => typeof(T).IsAssignableFrom(i)))
            {
                extensions[interfaceType] = extension;
            }
        }

        protected TV FindExtension<TV>()
            where TV : T
        {
            if (extensions.ContainsKey(typeof(TV)))
                return (TV) extensions[typeof(TV)];
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
