using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Blog.Models;

namespace Blog.Controllers
{
    [RequireHttps]
    public class SubscriptionsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // POST: Subscriptions/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Email")]Subscription subscription)
        {
            if (ModelState.IsValid)
            {
                if (db.Subscriptions.FirstOrDefault(x => x.Email == subscription.Email) != null) return View("Exists");
                db.Subscriptions.Add(subscription);
                db.SaveChanges();
                return View("SubscriptionSuccess");
            }
            return RedirectToAction("Error", "Home", new { errorText="Invalid email provided!" });
        }

        // POST: Subscriptions/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            Subscription subscription = db.Subscriptions.Find(id);
            db.Subscriptions.Remove(subscription);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
