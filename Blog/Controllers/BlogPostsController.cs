using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Blog.Models;
using Blog.Utilities;
using Microsoft.AspNet.Identity;

namespace Blog.Controllers
{
    public class BlogPostsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: BlogPosts
        public ActionResult Index()
        {
            var blogPosts = db.BlogPosts.Include(b => b.Author);
            return View(blogPosts.ToList());
        }

        // GET: BlogPosts/Details/5
        //public ActionResult Details(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    BlogPost blogPost = db.BlogPosts.Find(id);
        //    return View(blogPost);
        //}
        public ActionResult Details(string slug)
        {
            if (String.IsNullOrWhiteSpace(slug))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BlogPost blogPost = db.BlogPosts.FirstOrDefault(p => p.Slug == slug);
            if (blogPost == null)
            {
                return HttpNotFound();
            }
            return View(blogPost);

        }

        // GET: BlogPosts/Create
        public ActionResult Create()
        {
            if (User.Identity.IsAuthenticated)
            {
                if(User.IsInRole("Admin") || User.IsInRole("Writer"))
                {
                    ViewBag.AuthorId = new SelectList(db.Users, "Id", "FirstName");
                    return View();
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                return RedirectToAction("Login","Account");
            }
        }

        // POST: BlogPosts/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,AuthorId,Title,Body,Summary,Slug,CreateDate,UpdateDate,UpdateReason,MediaLink,Listed")] BlogPost blogPost)
        {
            if (ModelState.IsValid)
            {
                if (User.Identity.IsAuthenticated)
                {
                    if (User.IsInRole("Admin") || User.IsInRole("Writer"))
                    {
                        var Slug = StringUtilities.URLFriendly(blogPost.Title); if (String.IsNullOrWhiteSpace(Slug)) { ModelState.AddModelError("Title", "Invalid title"); return View(blogPost); }
                        if (db.BlogPosts.Any(p => p.Slug == Slug)) { ModelState.AddModelError("Title", "The title must be unique"); return View(blogPost); }
                        blogPost.AuthorId = User.Identity.GetUserId();
                        blogPost.Slug = Slug;
                        blogPost.CreateDate = DateTime.Now;
                        db.BlogPosts.Add(blogPost);
                        db.SaveChanges();
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    return RedirectToAction("Login", "Account");
                }
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }


        // GET: BlogPosts/Edit/5
        public ActionResult Edit(int? id)
        {
            if (User.Identity.IsAuthenticated)
            {
                BlogPost blogPost = db.BlogPosts.Find(id);
                if (blogPost != null && User.IsInRole("Admin") || User.Identity.GetUserId()==blogPost.AuthorId)
                {
                    return View(blogPost);
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }

        // POST: BlogPosts/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Title,Body,MediaLink,UpdateReason")] BlogPost blogPost, int Id)
        {

            if (User.Identity.IsAuthenticated)
            {
                BlogPost post = db.BlogPosts.Find(Id);
                post.Body = blogPost.Body;
                post.Title = blogPost.Title;
                post.MediaLink = blogPost.MediaLink;
                post.UpdateReason = blogPost.UpdateReason;
                if (User.IsInRole("Admin") || User.Identity.GetUserId() == post.AuthorId)   
                {
                    db.Entry(post).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Details", "BlogPosts", new { slug=post.Slug });
                }
                else
                {
                    return View("Index", "Home");
                }
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
            ViewBag.AuthorId = new SelectList(db.Users, "Id", "FirstName", blogPost.AuthorId);
            return View(blogPost);
        }

        // POST: BlogPosts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int blogPostToDelete)
        {
            BlogPost blogPost = db.BlogPosts.Find(blogPostToDelete);
            db.BlogPosts.Remove(blogPost);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult Search(string searchText)
        {
            var viewModel = new SearchResultsViewModel();
            viewModel.Comments = db.Comments.Where(x => x.Content.Contains(searchText)).ToList();
            viewModel.BlogPosts = db.BlogPosts.Where(x => x.Title.Contains(searchText) || x.Body.Contains(searchText)).ToList();
            return View("Results",viewModel);
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
