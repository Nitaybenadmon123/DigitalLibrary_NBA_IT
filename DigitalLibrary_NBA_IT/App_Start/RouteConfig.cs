using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace DigitalLibrary_NBA_IT
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Welcome", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "UserProfile",
                url: "UserProfile/{action}/{id}",
                defaults: new { controller = "UserProfile", action = "Profile", id = UrlParameter.Optional }
            );



        }
    }
}
