using System;
using System.Collections.Generic;
using System.Linq;
using Spark.Engine.Interfaces;

namespace Spark.Engine.Service
{
//    public class BaseExtensibleObject<T> : IExtensibleObject<T>
//        where T:IExtensibleObject<T>
//    {
//        private Dictionary<Type, IExtension<T>> extensions; 
//        public BaseExtensibleObject()
//        {
//            extensions = new Dictionary<Type, IExtension<T>>();
//        }
//        public void AddExtension<TV>(TV extension) where TV : IExtension<T>
//        {
//            extensions[typeof (TV)] = extension;
//        }

//        public void RemoveExtension<TV>() where TV : IExtension<T>
//        {
//            extensions.Remove(typeof (TV));
//        }

//        public TV FindExtension<TV>() where TV : IExtension<T>
//        {
//            if (extensions.Keys.Contains(typeof (TV)))
//                return (TV)extensions[typeof (TV)];
//            return default(TV);
//        }
    //}
}