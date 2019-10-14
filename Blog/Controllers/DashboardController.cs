using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Blog.Models;
using Blog.Utilities;
using Microsoft.AspNet.Identity;

namespace Blog.Controllers
{
    [RequireHttps]
    public class DashboardController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult UserManagement()
        {
            var model = new DashboardViewModel()
            {
                Users = db.Users.ToList()
            };
            return View();
        }
    }
}
