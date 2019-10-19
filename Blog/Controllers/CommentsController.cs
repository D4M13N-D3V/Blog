using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Blog.Models;
using Microsoft.AspNet.Identity;

namespace Blog.Controllers
{
    [RequireHttps]
    public class CommentsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // POST: Comments/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Create( Comment comment, string newCommentContent)
        {
            if (newCommentContent.Length < 5) return RedirectToAction("Error", "Home", new { errorText = "Comment must be longer then 5 characters" });
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Error", "Home", new { errorText = "You must be logged in to continue." });
            }
            else
            {
                comment.AuthorId = User.Identity.GetUserId();
                comment.Content = newCommentContent;
                comment.CreateDate = DateTime.Now;
                comment.Reputation = 0;
                db.Comments.Add(comment);
                db.SaveChanges();
                return RedirectToAction("Details", "BlogPosts", new { slug = db.BlogPosts.FirstOrDefault(x => x.Id == comment.BlogPostId).Slug });
            }
        }

        // POST: Comments/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Content,UpdateDate,UpdateReason")] Comment comment, int commentId)
        {
            if (User.Identity.IsAuthenticated)
            {
                var updatedComment = db.Comments.Find(commentId);
                if (User.IsInRole("Moderator") || User.IsInRole("Admin") || User.Identity.GetUserId()==comment.AuthorId)
                {
                    updatedComment.Content = comment.Content;
                    updatedComment.UpdateDate = DateTime.Now;
                    updatedComment.UpdateReason = comment.UpdateReason;
                    db.Entry(updatedComment).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Details", "BlogPosts", new { slug = updatedComment.BlogPost.Slug });
                }
                else
                {
                    return RedirectToAction("Error", "Home", new { errorText = "You must be a administrator, moderator, or the writer to modify this post." });
                }
            }
            else
            {
                return RedirectToAction("Error", "Home", new { errorText = "You must be logged in to continue." });
            }
        }

        // POST: Comments/Delete/5
        [HttpPost]
        public ActionResult Delete(int deletedCommentId)
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Moderator") || User.IsInRole("Admin"))
                {
                    Comment comment = db.Comments.Find(deletedCommentId);
                    var blogSlug = comment.BlogPost.Slug;
                    db.Comments.Remove(comment);
                    db.SaveChanges();
                    return RedirectToAction("Details", "BlogPosts", new { slug = blogSlug });
                }
                else
                {
                    return RedirectToAction("Error", "Home", new { errorText = "You must be a administrator, moderator, or the writer to modify this post." });
                }
            }
            else
            {
                return RedirectToAction("Error", "Home", new { errorText = "You must be logged in to continue." });
            }
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
