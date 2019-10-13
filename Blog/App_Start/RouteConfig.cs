using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Blog
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "BlogpostDetails",
                url: "Articles/Details/{slug}",
                defaults: new
                {
                    controller = "BlogPosts",
                    action = "Details",
                    slug = UrlParameter.Optional
                });

            routes.MapRoute(
                name: "ArticlesHome",
                url: "Articles",
                defaults: new
                {
                    controller = "BlogPosts",
                    action = "Index",
                    slug = UrlParameter.Optional
                });

            routes.MapRoute(
                name: "search",
                url: "Content/Search/{searchText}",
                defaults: new
                {
                    controller = "BlogPosts",
                    action = "Search",
                    searchText = UrlParameter.Optional
                });
            routes.MapRoute(
                name: "BlogPostEdit",
                url: "Articles/Edit/{slug}",
                defaults: new
                {
                    controller = "BlogPosts",
                    action = "Edit",
                    slug = UrlParameter.Optional
                });
            routes.MapRoute(
                name: "BlogPostDelete",
                url: "Articles/Delete/{slug}",
                defaults: new
                {
                    controller = "BlogPosts",
                    action = "Delete",
                    slug = UrlParameter.Optional
                });
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
            routes.MapRoute(
                "404-PageNotFound",
                "{*url}",
                new { controller = "Home", action = "Error", errorText="Error 404, sorry about that couldnt find the page you were looking for.." }
            );
        }
    }
}
