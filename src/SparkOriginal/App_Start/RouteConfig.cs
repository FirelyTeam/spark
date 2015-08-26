/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System.Web.Mvc;
using System.Web.Routing;

namespace Spark.App_Start
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute(name: "Default", url: "", defaults: new { controller = "Home", action = "Index" });
            routes.MapRoute(name: "Site", url: "Home/{action}", defaults: new { controller = "Home", action = "Index" });
            routes.MapRoute(name: "Initialize", url: "Initialize/{action}", defaults: new { controller = "Initialize", action = "Index" });
        }
    }
}
