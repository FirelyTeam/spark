using System;
using System.Collections.Generic;
using System.Linq;
using Spark.Engine.Interfaces;

namespace Spark.Engine.Service
{
    public class BaseExtendableFhirStore : IExtendableFhirStore
    {
        private Dictionary<Type, IFhirStoreExtension> extensions; 
        public BaseExtendableFhirStore()
        {
            extensions = new Dictionary<Type, IFhirStoreExtension>();
        }
        public void AddExtension<T>(T extension) where T : IFhirStoreExtension
        {
            extensions[typeof (T)] = extension;
        }

        public void RemoveExtension<T>() where T : IFhirStoreExtension
        {
            extensions.Remove(typeof (T));
        }

        public T FindExtension<T>() where T : IFhirStoreExtension
        {
            if (extensions.Keys.Contains(typeof (T)))
                return (T)extensions[typeof (T)];
            return default(T);
        }
    }
}