using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR;
using Microsoft.Practices.Unity;

namespace Spark
{
    public class SignalRUnityDependencyResolver :  DefaultDependencyResolver
    {
        private IUnityContainer _container;

        public SignalRUnityDependencyResolver(IUnityContainer container)
        {
            _container = container;
        }

        public override object GetService(Type serviceType)
        {
            try
            {
                return _container.Resolve(serviceType);
            }
            catch (Exception)
            {
                return base.GetService(serviceType);
            }
        }

        public override IEnumerable<object> GetServices(Type serviceType)
        {
            return _container.ResolveAll(serviceType).Concat(base.GetServices(serviceType));
        }
    }
}