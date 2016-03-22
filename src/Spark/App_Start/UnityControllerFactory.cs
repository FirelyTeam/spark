using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Spark
{
    /// <summary>
    /// Needed for injection in MVC controllers. DefaultControllerFactory does not take Unity into account.
    /// (ApiControllers are also resolved correctly without this class.)
    /// Based on this article: http://www.codeproject.com/Articles/560798/ASP-NET-MVC-controller-dependency-injection-for-be
    /// </summary>
    public class UnityControllerFactory : DefaultControllerFactory
    {
        private readonly UnityContainer _container;
        public UnityControllerFactory(UnityContainer container)
        {
            _container = container;
        }
        protected override IController GetControllerInstance(System.Web.Routing.RequestContext requestContext, Type controllerType)
        {
            IController result;
            try
            {
                result = (IController)(_container.Resolve(controllerType));
            }
            catch (ResolutionFailedException)
            {
                //Doesn't matter, we'll leave it to the DefaultControllerFactory then.
                result = null;
            }

            return null == result
                                ? base.GetControllerInstance(requestContext, controllerType)
                                : (IController)result;
        }
        public override void ReleaseController(IController controller)
        {
            ((IDisposable)controller).Dispose();
        }
    }
}
