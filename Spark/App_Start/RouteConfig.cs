using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Spark
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute(name: "Default", url: "", defaults: new { controller = "Home", action = "Index" });
            routes.MapRoute(name: "Site", url: "Home/{action}", defaults: new { controller = "Home", action = "Index" });
        }
    }
}
