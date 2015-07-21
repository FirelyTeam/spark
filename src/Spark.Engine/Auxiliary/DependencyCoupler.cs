/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */
using System;
using System.Collections.Generic;
using System.Net;
using Spark.Core;

namespace Spark.Engine.Auxiliary
{
    // Intermediate solution. Eventually replace with real resolver.

    public delegate object Instantiator();

    public static class DependencyCoupler
    {
        static Dictionary<Type, Object> instances = new Dictionary<Type, object>();
        static Dictionary<Type, Instantiator> instanciators = new Dictionary<Type, Instantiator>();
        static Dictionary<Type, Type> types = new Dictionary<Type, Type>();

        public static void Register<I>(Instantiator instanciator)
        {
            instanciators.Add(typeof(I), instanciator);
        }

        public static void Register<I, T>()
        {
            types.Add(typeof(I), typeof(T));
        }

        public static void Register<I>(object instance)
        {
            instances.Add(typeof(I), instance);
        }

        public static T Inject<T>()
        {
            T instance = default(T);
            Type key = typeof(T);
            Type type = null;
            Instantiator instanciator = null;


            object value;
            if (instances.TryGetValue(key, out value))
            {
                instance = (T)value;
            }
            else if (instanciators.TryGetValue(key, out instanciator))
            {
                instance = (T)(object)instanciator();
            }
            else if (types.TryGetValue(key, out type))
            {
                instance = (T)Activator.CreateInstance(type);
            }
            else
            {
                throw Error.Create(HttpStatusCode.InternalServerError, "Dependency injection error: The type ({0}) you try to instanciate is not registered", key.Name);
            }
            
            return instance;
        }
        
        private static volatile object access = new object();

        private static bool registered = false;

        public static void Configure(Action configure)
        {
            lock (access)
            {
                if (!registered)
                {
                    registered = true;
                    configure();
                }
            }
        }
    }
   
}