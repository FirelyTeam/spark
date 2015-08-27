using Spark.Service;
using System;
using Spark.Engine.Core;

namespace Spark.Core
{
    public static class InfrastructureExtensions
    {

        public static Infrastructure AddLocalhost(this Infrastructure infrastructure, ILocalhost localhost)
        {
            infrastructure.Localhost = localhost;
            return infrastructure;
        }

        public static Infrastructure AddLocalhost(this Infrastructure infrastructure, Uri url)
        {
            return infrastructure.AddLocalhost(new Localhost(url));
        }

        public static FhirService CreateService(this Infrastructure infrastructure)
        {
            return new FhirService(infrastructure);
        }

        public static Infrastructure AddListener(this Infrastructure infrastructure, IServiceListener listener)
        {
            if (infrastructure.ServiceListener == null)
            {
                infrastructure.ServiceListener = new ServiceListener();
            }

            (infrastructure.ServiceListener as ServiceListener).Add(listener);

            return infrastructure;
        }

        public static Infrastructure ClearListeners(this Infrastructure infrastructure, IServiceListener listener)
        {
            if (infrastructure.ServiceListener == null)
            {
                infrastructure.ServiceListener = new ServiceListener();
            }

            if (infrastructure.ServiceListener is ServiceListener)
            {
                (infrastructure.ServiceListener as ServiceListener).Clear();
            }

            return infrastructure;
        }

       
    }

   
}
