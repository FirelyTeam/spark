using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Spark.Core;

namespace Spark.Core
{
    // Intermediate solution. Eventually replace with real resolver.

    public delegate object Instantiator();

    public static class DependencyCoupler
    {
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

        public static T Inject<T>()
        {
            T instance = default(T);
            Type key = typeof(T);
            Type type = null;
            
            Instantiator instanciator = null;
            if (instanciators.TryGetValue(key, out instanciator))
            {
                instance = (T)(object)instanciator();
            }
            else if (types.TryGetValue(key, out type))
            {
                instance = (T)Activator.CreateInstance(type);
            }
            else
                throw new KeyNotFoundException("The dependancy type you try to instanciate for is not registered");
            return instance;
        }
    }

   
}