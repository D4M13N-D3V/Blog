using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Blog.Models;
using Blog.Utilities;
using Microsoft.AspNet.Identity;
using PagedList;
using Utilities;

namespace Blog.Controllers
{ [RequireHttps]
    public class BlogPostsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index(int? page)
        {
            int pageSize = 5;// display three blog posts at a time on this page
            int pageNumber = (page ?? 1);
            var blogPosts = db.BlogPosts.Include(b => b.Author).OrderByDescending(x=>x.CreateDate).ToPagedList(pageNumber, pageSize);
            return View(blogPosts);
        }
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
            //SqlParameter param1 = new SqlParameter("@blogpostid", blogPost.Id);
            //List<Comment> result = db.Database.SqlQuery<Comment>("exec SelectCommentsByBlogpost  @blogpostid", param1).Where(x=>x.BlogPostId==blogPost.Id).Reverse().ToList();

            var result = db.Comments.Where(x => x.BlogPostId == blogPost.Id).OrderBy(x=>x.ParentID).ToList();//OrderByDescending(x => x.CreateDate)
            var root = new TreeNode<Comment>(new Comment { Id = 0 });
            var currentNode = root;

            while (result.Any())
            {
                for(int i=0; i<result.Count; i++)
                {
                    if (currentNode.Children.Any(x => x.Data.Id == result[i].ParentID) )
                    {
                        currentNode.FirstOrDefault(x => x.Data.Id == result[i].ParentID).AddChild(result[i]);
                        //currentNode.FirstOrDefault(x => x.Data.Id == result[i].ParentID).Children = currentNode.FirstOrDefault(x => x.Data.Id == result[i].ParentID).OrderByDescending(x => x.Data.CreateDate).ToList();
                        result.Remove(result[i]);
                    }
                    else if(currentNode.Data.Id == result[i].ParentID)
                    {
                        currentNode.AddChild(result[i]);
                        //currentNode.Children = currentNode.Children.OrderByDescending(x => x.Data.CreateDate).ToList();
                        result.Remove(result[i]);
                    }
                }
            }

            var viewmodel = new BlogPostDetailsViewModel
            {
                BlogPost = blogPost,
                CommentTree = root,
                CommentCount = result.Count
            };

            blogPost.ViewCount++;
            db.SaveChanges();
            return View(viewmodel);

        }
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
                    return RedirectToAction("Error", "Home", new { errorText = "You must be a administrator or the writer to modify this post." });
                }
            }
            else
            {
                return RedirectToAction("Error", "Home", new { errorText = "You must be logged in to continue." });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(BlogPost blogPost, HttpPostedFileBase uploadImage)
        {
            if (ModelState.IsValid)
            {
                if (User.Identity.IsAuthenticated)
                {
                    if (User.IsInRole("Admin") || User.IsInRole("Writer"))
                    {
                        if (ImageUploadValidator.IsWebFriendlyImage(uploadImage)) {
                            var fileName = DateTime.Now.Ticks + ".png";
                            var path = Path.Combine(Server.MapPath("~/Uploads/"), fileName);
                            uploadImage.SaveAs(path);
                            blogPost.MediaLink = "/Uploads/" + fileName;
                        }
                        var Slug = Utilities.Utilities.URLFriendly(blogPost.Title); if (String.IsNullOrWhiteSpace(Slug)) { ModelState.AddModelError("Title", "Invalid title"); return View(blogPost); }
                        if (db.BlogPosts.Any(p => p.Slug == Slug)) { ModelState.AddModelError("Title", "The title must be unique"); return View(blogPost); }
                        blogPost.AuthorId = User.Identity.GetUserId();
                        blogPost.Slug = Slug;
                        blogPost.ViewCount = 0;
                        blogPost.Reputation = 0;
                        blogPost.CreateDate = DateTime.Now;
                        db.BlogPosts.Add(blogPost);
                        db.SaveChanges();
                        var mailingList = db.Subscriptions;
                        string html = "";
                        if (blogPost.MediaLink == null)
                        {
                            html = "<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Strict//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd'><html xmlns='http://www.w3.org/1999/xhtml'> <head> <meta http-equiv='Content-Type' content='text/html; charset=utf-8'> <style type='text/css'> .ExternalClass{width:100%}.ExternalClass,.ExternalClass p,.ExternalClass span,.ExternalClass font,.ExternalClass td,.ExternalClass div{line-height:150%}a{text-decoration:none}@media screen and (max-width: 600px){table.row th.col-lg-1,table.row th.col-lg-2,table.row th.col-lg-3,table.row th.col-lg-4,table.row th.col-lg-5,table.row th.col-lg-6,table.row th.col-lg-7,table.row th.col-lg-8,table.row th.col-lg-9,table.row th.col-lg-10,table.row th.col-lg-11,table.row th.col-lg-12{display:block;width:100% !important}.d-mobile{display:block !important}.d-desktop{display:none !important}.w-lg-25{width:auto !important}.w-lg-25>tbody>tr>td{width:auto !important}.w-lg-50{width:auto !important}.w-lg-50>tbody>tr>td{width:auto !important}.w-lg-75{width:auto !important}.w-lg-75>tbody>tr>td{width:auto !important}.w-lg-100{width:auto !important}.w-lg-100>tbody>tr>td{width:auto !important}.w-lg-auto{width:auto !important}.w-lg-auto>tbody>tr>td{width:auto !important}.w-25{width:25% !important}.w-25>tbody>tr>td{width:25% !important}.w-50{width:50% !important}.w-50>tbody>tr>td{width:50% !important}.w-75{width:75% !important}.w-75>tbody>tr>td{width:75% !important}.w-100{width:100% !important}.w-100>tbody>tr>td{width:100% !important}.w-auto{width:auto !important}.w-auto>tbody>tr>td{width:auto !important}.p-lg-0>tbody>tr>td{padding:0 !important}.pt-lg-0>tbody>tr>td,.py-lg-0>tbody>tr>td{padding-top:0 !important}.pr-lg-0>tbody>tr>td,.px-lg-0>tbody>tr>td{padding-right:0 !important}.pb-lg-0>tbody>tr>td,.py-lg-0>tbody>tr>td{padding-bottom:0 !important}.pl-lg-0>tbody>tr>td,.px-lg-0>tbody>tr>td{padding-left:0 !important}.p-lg-1>tbody>tr>td{padding:0 !important}.pt-lg-1>tbody>tr>td,.py-lg-1>tbody>tr>td{padding-top:0 !important}.pr-lg-1>tbody>tr>td,.px-lg-1>tbody>tr>td{padding-right:0 !important}.pb-lg-1>tbody>tr>td,.py-lg-1>tbody>tr>td{padding-bottom:0 !important}.pl-lg-1>tbody>tr>td,.px-lg-1>tbody>tr>td{padding-left:0 !important}.p-lg-2>tbody>tr>td{padding:0 !important}.pt-lg-2>tbody>tr>td,.py-lg-2>tbody>tr>td{padding-top:0 !important}.pr-lg-2>tbody>tr>td,.px-lg-2>tbody>tr>td{padding-right:0 !important}.pb-lg-2>tbody>tr>td,.py-lg-2>tbody>tr>td{padding-bottom:0 !important}.pl-lg-2>tbody>tr>td,.px-lg-2>tbody>tr>td{padding-left:0 !important}.p-lg-3>tbody>tr>td{padding:0 !important}.pt-lg-3>tbody>tr>td,.py-lg-3>tbody>tr>td{padding-top:0 !important}.pr-lg-3>tbody>tr>td,.px-lg-3>tbody>tr>td{padding-right:0 !important}.pb-lg-3>tbody>tr>td,.py-lg-3>tbody>tr>td{padding-bottom:0 !important}.pl-lg-3>tbody>tr>td,.px-lg-3>tbody>tr>td{padding-left:0 !important}.p-lg-4>tbody>tr>td{padding:0 !important}.pt-lg-4>tbody>tr>td,.py-lg-4>tbody>tr>td{padding-top:0 !important}.pr-lg-4>tbody>tr>td,.px-lg-4>tbody>tr>td{padding-right:0 !important}.pb-lg-4>tbody>tr>td,.py-lg-4>tbody>tr>td{padding-bottom:0 !important}.pl-lg-4>tbody>tr>td,.px-lg-4>tbody>tr>td{padding-left:0 !important}.p-lg-5>tbody>tr>td{padding:0 !important}.pt-lg-5>tbody>tr>td,.py-lg-5>tbody>tr>td{padding-top:0 !important}.pr-lg-5>tbody>tr>td,.px-lg-5>tbody>tr>td{padding-right:0 !important}.pb-lg-5>tbody>tr>td,.py-lg-5>tbody>tr>td{padding-bottom:0 !important}.pl-lg-5>tbody>tr>td,.px-lg-5>tbody>tr>td{padding-left:0 !important}.p-0>tbody>tr>td{padding:0 !important}.pt-0>tbody>tr>td,.py-0>tbody>tr>td{padding-top:0 !important}.pr-0>tbody>tr>td,.px-0>tbody>tr>td{padding-right:0 !important}.pb-0>tbody>tr>td,.py-0>tbody>tr>td{padding-bottom:0 !important}.pl-0>tbody>tr>td,.px-0>tbody>tr>td{padding-left:0 !important}.p-1>tbody>tr>td{padding:4px !important}.pt-1>tbody>tr>td,.py-1>tbody>tr>td{padding-top:4px !important}.pr-1>tbody>tr>td,.px-1>tbody>tr>td{padding-right:4px !important}.pb-1>tbody>tr>td,.py-1>tbody>tr>td{padding-bottom:4px !important}.pl-1>tbody>tr>td,.px-1>tbody>tr>td{padding-left:4px !important}.p-2>tbody>tr>td{padding:8px !important}.pt-2>tbody>tr>td,.py-2>tbody>tr>td{padding-top:8px !important}.pr-2>tbody>tr>td,.px-2>tbody>tr>td{padding-right:8px !important}.pb-2>tbody>tr>td,.py-2>tbody>tr>td{padding-bottom:8px !important}.pl-2>tbody>tr>td,.px-2>tbody>tr>td{padding-left:8px !important}.p-3>tbody>tr>td{padding:16px !important}.pt-3>tbody>tr>td,.py-3>tbody>tr>td{padding-top:16px !important}.pr-3>tbody>tr>td,.px-3>tbody>tr>td{padding-right:16px !important}.pb-3>tbody>tr>td,.py-3>tbody>tr>td{padding-bottom:16px !important}.pl-3>tbody>tr>td,.px-3>tbody>tr>td{padding-left:16px !important}.p-4>tbody>tr>td{padding:24px !important}.pt-4>tbody>tr>td,.py-4>tbody>tr>td{padding-top:24px !important}.pr-4>tbody>tr>td,.px-4>tbody>tr>td{padding-right:24px !important}.pb-4>tbody>tr>td,.py-4>tbody>tr>td{padding-bottom:24px !important}.pl-4>tbody>tr>td,.px-4>tbody>tr>td{padding-left:24px !important}.p-5>tbody>tr>td{padding:48px !important}.pt-5>tbody>tr>td,.py-5>tbody>tr>td{padding-top:48px !important}.pr-5>tbody>tr>td,.px-5>tbody>tr>td{padding-right:48px !important}.pb-5>tbody>tr>td,.py-5>tbody>tr>td{padding-bottom:48px !important}.pl-5>tbody>tr>td,.px-5>tbody>tr>td{padding-left:48px !important}.s-lg-1>tbody>tr>td,.s-lg-2>tbody>tr>td,.s-lg-3>tbody>tr>td,.s-lg-4>tbody>tr>td,.s-lg-5>tbody>tr>td{font-size:0 !important;line-height:0 !important;height:0 !important}.s-0>tbody>tr>td{font-size:0 !important;line-height:0 !important;height:0 !important}.s-1>tbody>tr>td{font-size:4px !important;line-height:4px !important;height:4px !important}.s-2>tbody>tr>td{font-size:8px !important;line-height:8px !important;height:8px !important}.s-3>tbody>tr>td{font-size:16px !important;line-height:16px !important;height:16px !important}.s-4>tbody>tr>td{font-size:24px !important;line-height:24px !important;height:24px !important}.s-5>tbody>tr>td{font-size:48px !important;line-height:48px !important;height:48px !important}}@media yahoo{.d-mobile{display:none !important}.d-desktop{display:block !important}.w-lg-25{width:25% !important}.w-lg-25>tbody>tr>td{width:25% !important}.w-lg-50{width:50% !important}.w-lg-50>tbody>tr>td{width:50% !important}.w-lg-75{width:75% !important}.w-lg-75>tbody>tr>td{width:75% !important}.w-lg-100{width:100% !important}.w-lg-100>tbody>tr>td{width:100% !important}.w-lg-auto{width:auto !important}.w-lg-auto>tbody>tr>td{width:auto !important}.p-lg-0>tbody>tr>td{padding:0 !important}.pt-lg-0>tbody>tr>td,.py-lg-0>tbody>tr>td{padding-top:0 !important}.pr-lg-0>tbody>tr>td,.px-lg-0>tbody>tr>td{padding-right:0 !important}.pb-lg-0>tbody>tr>td,.py-lg-0>tbody>tr>td{padding-bottom:0 !important}.pl-lg-0>tbody>tr>td,.px-lg-0>tbody>tr>td{padding-left:0 !important}.p-lg-1>tbody>tr>td{padding:4px !important}.pt-lg-1>tbody>tr>td,.py-lg-1>tbody>tr>td{padding-top:4px !important}.pr-lg-1>tbody>tr>td,.px-lg-1>tbody>tr>td{padding-right:4px !important}.pb-lg-1>tbody>tr>td,.py-lg-1>tbody>tr>td{padding-bottom:4px !important}.pl-lg-1>tbody>tr>td,.px-lg-1>tbody>tr>td{padding-left:4px !important}.p-lg-2>tbody>tr>td{padding:8px !important}.pt-lg-2>tbody>tr>td,.py-lg-2>tbody>tr>td{padding-top:8px !important}.pr-lg-2>tbody>tr>td,.px-lg-2>tbody>tr>td{padding-right:8px !important}.pb-lg-2>tbody>tr>td,.py-lg-2>tbody>tr>td{padding-bottom:8px !important}.pl-lg-2>tbody>tr>td,.px-lg-2>tbody>tr>td{padding-left:8px !important}.p-lg-3>tbody>tr>td{padding:16px !important}.pt-lg-3>tbody>tr>td,.py-lg-3>tbody>tr>td{padding-top:16px !important}.pr-lg-3>tbody>tr>td,.px-lg-3>tbody>tr>td{padding-right:16px !important}.pb-lg-3>tbody>tr>td,.py-lg-3>tbody>tr>td{padding-bottom:16px !important}.pl-lg-3>tbody>tr>td,.px-lg-3>tbody>tr>td{padding-left:16px !important}.p-lg-4>tbody>tr>td{padding:24px !important}.pt-lg-4>tbody>tr>td,.py-lg-4>tbody>tr>td{padding-top:24px !important}.pr-lg-4>tbody>tr>td,.px-lg-4>tbody>tr>td{padding-right:24px !important}.pb-lg-4>tbody>tr>td,.py-lg-4>tbody>tr>td{padding-bottom:24px !important}.pl-lg-4>tbody>tr>td,.px-lg-4>tbody>tr>td{padding-left:24px !important}.p-lg-5>tbody>tr>td{padding:48px !important}.pt-lg-5>tbody>tr>td,.py-lg-5>tbody>tr>td{padding-top:48px !important}.pr-lg-5>tbody>tr>td,.px-lg-5>tbody>tr>td{padding-right:48px !important}.pb-lg-5>tbody>tr>td,.py-lg-5>tbody>tr>td{padding-bottom:48px !important}.pl-lg-5>tbody>tr>td,.px-lg-5>tbody>tr>td{padding-left:48px !important}.s-lg-0>tbody>tr>td{font-size:0 !important;line-height:0 !important;height:0 !important}.s-lg-1>tbody>tr>td{font-size:4px !important;line-height:4px !important;height:4px !important}.s-lg-2>tbody>tr>td{font-size:8px !important;line-height:8px !important;height:8px !important}.s-lg-3>tbody>tr>td{font-size:16px !important;line-height:16px !important;height:16px !important}.s-lg-4>tbody>tr>td{font-size:24px !important;line-height:24px !important;height:24px !important}.s-lg-5>tbody>tr>td{font-size:48px !important;line-height:48px !important;height:48px !important}}</style></head> <body style='outline: 0; width: 100%; min-width: 100%; height: 100%; -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%; font-family: Helvetica, Arial, sans-serif; line-height: 24px; font-weight: normal; font-size: 16px; -moz-box-sizing: border-box; -webkit-box-sizing: border-box; box-sizing: border-box; margin: 0; padding: 0; border: 0;'><div class='preview' style='display: none; max-height: 0px; overflow: hidden;'> A new article has been posted on the website!                                                                </div><table valign='top' class='bg-dark body' style='outline: 0; width: 100%; min-width: 100%; height: 100%; -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%; font-family: Helvetica, Arial, sans-serif; line-height: 24px; font-weight: normal; font-size: 16px; -moz-box-sizing: border-box; -webkit-box-sizing: border-box; box-sizing: border-box; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: collapse; margin: 0; padding: 0; border: 0;' bgcolor='#343a40'> <tbody> <tr> <td valign='top' style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; margin: 0;' align='left' bgcolor='#343a40'> <table class='container' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: collapse; width: 100%;'> <tbody> <tr> <td align='center' style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; margin: 0; padding: 0 16px;'><!--[if (gte mso 9)|(IE)]> <table align='center'> <tbody> <tr> <td width='600'><![endif]--> <table align='center' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: collapse; width: 100%; max-width: 600px; margin: 0 auto;'> <tbody> <tr> <td style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; margin: 0;' align='left'> <table class='mx-auto' align='center' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: collapse; margin: 0 auto;'> <tbody> <tr> <td style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; margin: 0;' align='left'> <table class='s-4 w-100' border='0' cellpadding='0' cellspacing='0' style='width: 100%;'> <tbody> <tr> <td height='24' style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 24px; width: 100%; height: 24px; margin: 0;' align='left'>   </td></tr></tbody></table><img class=' ' src='https://damiensblog.azurewebsites.net/Images/white-text.png' style='height: auto; line-height: 100%; outline: none; text-decoration: none; border: 0 none;'><table class='s-3 w-100' border='0' cellpadding='0' cellspacing='0' style='width: 100%;'> <tbody> <tr> <td height='16' style='border-spacing: 0px; border-collapse: collapse; line-height: 16px; font-size: 16px; width: 100%; height: 16px; margin: 0;' align='left'>   </td></tr></tbody></table> </td></tr></tbody></table> <table class='card ' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: separate !important; border-radius: 4px; width: 100%; overflow: hidden; border: 1px solid #dee2e6;' bgcolor='#ffffff'> <tbody> <tr> <td style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; width: 100%; margin: 0;' align='left'> <div style='border-top-width: 5px; border-top-color: #007bff; border-top-style: solid;'> <table class='card-body' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: collapse; width: 100%;'> <tbody> <tr> <td style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; width: 100%; margin: 0; padding: 20px;' align='left'> <div> <h4 class='text-center' style='margin-top: 0; margin-bottom: 0; font-weight: 500; color: inherit; vertical-align: baseline; font-size: 24px; line-height: 28.8px;' align='center'>A new article has been posted!</h4> <h5 class='text-muted text-center' style='margin-top: 0; margin-bottom: 0; font-weight: 500; color: #636c72; vertical-align: baseline; font-size: 20px; line-height: 24px;' align='center'>" + blogPost.CreateDate.Month + "/" + blogPost.CreateDate.Day + "/" + blogPost.CreateDate.Year + " at " + blogPost.CreateDate.Hour + ":" + blogPost.CreateDate.Minute + "</h5> </div></td></tr></tbody></table> </div></td></tr></tbody></table><table class='s-4 w-100' border='0' cellpadding='0' cellspacing='0' style='width: 100%;'> <tbody> <tr> <td height='24' style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 24px; width: 100%; height: 24px; margin: 0;' align='left'>   </td></tr></tbody></table> <table class='card w-100 ' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: separate !important; border-radius: 4px; width: 100%; overflow: hidden; border: 1px solid #dee2e6;' bgcolor='#ffffff'> <tbody> <tr> <td style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; width: 100%; margin: 0;' align='left'> <div> <table class='card-body' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: collapse; width: 100%;'> <tbody> <tr> <td style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; width: 100%; margin: 0; padding: 20px;' align='left'> <div> <table class='mx-auto' align='center' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: collapse; margin: 0 auto;'> <tbody> <tr> <td style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; margin: 0;' align='left'> </td></tr></tbody></table> <h4 class='text-center' style='margin-top: 0; margin-bottom: 0; font-weight: 500; color: inherit; vertical-align: baseline; font-size: 24px; line-height: 28.8px;' align='center'>" + blogPost.Title + "</h4> <p class='text-center' style='line-height: 24px; font-size: 16px; margin: 0;' align='center'>" + blogPost.Summary + "</p><table class='s-2 w-100' border='0' cellpadding='0' cellspacing='0' style='width: 100%;'> <tbody> <tr> <td height='8' style='border-spacing: 0px; border-collapse: collapse; line-height: 8px; font-size: 8px; width: 100%; height: 8px; margin: 0;' align='left'>   </td></tr></tbody></table><table class='btn btn-primary btn-lg mx-auto ' align='center' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: separate !important; border-radius: 4px; margin: 0 auto;'> <tbody> <tr> <td style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; border-radius: 4px; margin: 0;' align='center' bgcolor='#007bff'> <a href='" + Utilities.Utilities.GetBaseUrl() + "" + Url.Action("Details", "BlogPosts", new { slug = blogPost.Slug }) + "' style='font-size: 20px; font-family: Helvetica, Arial, sans-serif; text-decoration: none; border-radius: 4.8px; line-height: 30px; display: inline-block; font-weight: normal; white-space: nowrap; background-color: #007bff; color: #ffffff; padding: 8px 16px; border: 1px solid #007bff;'>Read More</a> </td></tr></tbody></table> </div></td></tr></tbody></table> </div></td></tr></tbody></table><table class='s-4 w-100' border='0' cellpadding='0' cellspacing='0' style='width: 100%;'> <tbody> <tr> <td height='24' style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 24px; width: 100%; height: 24px; margin: 0;' align='left'>   </td></tr></tbody></table><table class='s-4 w-100' border='0' cellpadding='0' cellspacing='0' style='width: 100%;'> <tbody> <tr> <td height='24' style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 24px; width: 100%; height: 24px; margin: 0;' align='left'>   </td></tr></tbody></table> </td></tr></tbody> </table><!--[if (gte mso 9)|(IE)]> </td></tr></tbody> </table><![endif]--> </td></tr></tbody></table> </td></tr></tbody></table></body></html>";
                        }
                        else
                        {
                            html = "<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Strict//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd'><html xmlns='http://www.w3.org/1999/xhtml'><head> <meta http-equiv='Content-Type' content='text/html; charset=utf-8'> <style type='text/css'> .ExternalClass{width: 100%}.ExternalClass, .ExternalClass p, .ExternalClass span, .ExternalClass font, .ExternalClass td, .ExternalClass div{line-height: 150%}a{text-decoration: none}@media screen and (max-width: 600px){table.row th.col-lg-1, table.row th.col-lg-2, table.row th.col-lg-3, table.row th.col-lg-4, table.row th.col-lg-5, table.row th.col-lg-6, table.row th.col-lg-7, table.row th.col-lg-8, table.row th.col-lg-9, table.row th.col-lg-10, table.row th.col-lg-11, table.row th.col-lg-12{display: block; width: 100% !important}.d-mobile{display: block !important}.d-desktop{display: none !important}.w-lg-25{width: auto !important}.w-lg-25>tbody>tr>td{width: auto !important}.w-lg-50{width: auto !important}.w-lg-50>tbody>tr>td{width: auto !important}.w-lg-75{width: auto !important}.w-lg-75>tbody>tr>td{width: auto !important}.w-lg-100{width: auto !important}.w-lg-100>tbody>tr>td{width: auto !important}.w-lg-auto{width: auto !important}.w-lg-auto>tbody>tr>td{width: auto !important}.w-25{width: 25% !important}.w-25>tbody>tr>td{width: 25% !important}.w-50{width: 50% !important}.w-50>tbody>tr>td{width: 50% !important}.w-75{width: 75% !important}.w-75>tbody>tr>td{width: 75% !important}.w-100{width: 100% !important}.w-100>tbody>tr>td{width: 100% !important}.w-auto{width: auto !important}.w-auto>tbody>tr>td{width: auto !important}.p-lg-0>tbody>tr>td{padding: 0 !important}.pt-lg-0>tbody>tr>td, .py-lg-0>tbody>tr>td{padding-top: 0 !important}.pr-lg-0>tbody>tr>td, .px-lg-0>tbody>tr>td{padding-right: 0 !important}.pb-lg-0>tbody>tr>td, .py-lg-0>tbody>tr>td{padding-bottom: 0 !important}.pl-lg-0>tbody>tr>td, .px-lg-0>tbody>tr>td{padding-left: 0 !important}.p-lg-1>tbody>tr>td{padding: 0 !important}.pt-lg-1>tbody>tr>td, .py-lg-1>tbody>tr>td{padding-top: 0 !important}.pr-lg-1>tbody>tr>td, .px-lg-1>tbody>tr>td{padding-right: 0 !important}.pb-lg-1>tbody>tr>td, .py-lg-1>tbody>tr>td{padding-bottom: 0 !important}.pl-lg-1>tbody>tr>td, .px-lg-1>tbody>tr>td{padding-left: 0 !important}.p-lg-2>tbody>tr>td{padding: 0 !important}.pt-lg-2>tbody>tr>td, .py-lg-2>tbody>tr>td{padding-top: 0 !important}.pr-lg-2>tbody>tr>td, .px-lg-2>tbody>tr>td{padding-right: 0 !important}.pb-lg-2>tbody>tr>td, .py-lg-2>tbody>tr>td{padding-bottom: 0 !important}.pl-lg-2>tbody>tr>td, .px-lg-2>tbody>tr>td{padding-left: 0 !important}.p-lg-3>tbody>tr>td{padding: 0 !important}.pt-lg-3>tbody>tr>td, .py-lg-3>tbody>tr>td{padding-top: 0 !important}.pr-lg-3>tbody>tr>td, .px-lg-3>tbody>tr>td{padding-right: 0 !important}.pb-lg-3>tbody>tr>td, .py-lg-3>tbody>tr>td{padding-bottom: 0 !important}.pl-lg-3>tbody>tr>td, .px-lg-3>tbody>tr>td{padding-left: 0 !important}.p-lg-4>tbody>tr>td{padding: 0 !important}.pt-lg-4>tbody>tr>td, .py-lg-4>tbody>tr>td{padding-top: 0 !important}.pr-lg-4>tbody>tr>td, .px-lg-4>tbody>tr>td{padding-right: 0 !important}.pb-lg-4>tbody>tr>td, .py-lg-4>tbody>tr>td{padding-bottom: 0 !important}.pl-lg-4>tbody>tr>td, .px-lg-4>tbody>tr>td{padding-left: 0 !important}.p-lg-5>tbody>tr>td{padding: 0 !important}.pt-lg-5>tbody>tr>td, .py-lg-5>tbody>tr>td{padding-top: 0 !important}.pr-lg-5>tbody>tr>td, .px-lg-5>tbody>tr>td{padding-right: 0 !important}.pb-lg-5>tbody>tr>td, .py-lg-5>tbody>tr>td{padding-bottom: 0 !important}.pl-lg-5>tbody>tr>td, .px-lg-5>tbody>tr>td{padding-left: 0 !important}.p-0>tbody>tr>td{padding: 0 !important}.pt-0>tbody>tr>td, .py-0>tbody>tr>td{padding-top: 0 !important}.pr-0>tbody>tr>td, .px-0>tbody>tr>td{padding-right: 0 !important}.pb-0>tbody>tr>td, .py-0>tbody>tr>td{padding-bottom: 0 !important}.pl-0>tbody>tr>td, .px-0>tbody>tr>td{padding-left: 0 !important}.p-1>tbody>tr>td{padding: 4px !important}.pt-1>tbody>tr>td, .py-1>tbody>tr>td{padding-top: 4px !important}.pr-1>tbody>tr>td, .px-1>tbody>tr>td{padding-right: 4px !important}.pb-1>tbody>tr>td, .py-1>tbody>tr>td{padding-bottom: 4px !important}.pl-1>tbody>tr>td, .px-1>tbody>tr>td{padding-left: 4px !important}.p-2>tbody>tr>td{padding: 8px !important}.pt-2>tbody>tr>td, .py-2>tbody>tr>td{padding-top: 8px !important}.pr-2>tbody>tr>td, .px-2>tbody>tr>td{padding-right: 8px !important}.pb-2>tbody>tr>td, .py-2>tbody>tr>td{padding-bottom: 8px !important}.pl-2>tbody>tr>td, .px-2>tbody>tr>td{padding-left: 8px !important}.p-3>tbody>tr>td{padding: 16px !important}.pt-3>tbody>tr>td, .py-3>tbody>tr>td{padding-top: 16px !important}.pr-3>tbody>tr>td, .px-3>tbody>tr>td{padding-right: 16px !important}.pb-3>tbody>tr>td, .py-3>tbody>tr>td{padding-bottom: 16px !important}.pl-3>tbody>tr>td, .px-3>tbody>tr>td{padding-left: 16px !important}.p-4>tbody>tr>td{padding: 24px !important}.pt-4>tbody>tr>td, .py-4>tbody>tr>td{padding-top: 24px !important}.pr-4>tbody>tr>td, .px-4>tbody>tr>td{padding-right: 24px !important}.pb-4>tbody>tr>td, .py-4>tbody>tr>td{padding-bottom: 24px !important}.pl-4>tbody>tr>td, .px-4>tbody>tr>td{padding-left: 24px !important}.p-5>tbody>tr>td{padding: 48px !important}.pt-5>tbody>tr>td, .py-5>tbody>tr>td{padding-top: 48px !important}.pr-5>tbody>tr>td, .px-5>tbody>tr>td{padding-right: 48px !important}.pb-5>tbody>tr>td, .py-5>tbody>tr>td{padding-bottom: 48px !important}.pl-5>tbody>tr>td, .px-5>tbody>tr>td{padding-left: 48px !important}.s-lg-1>tbody>tr>td, .s-lg-2>tbody>tr>td, .s-lg-3>tbody>tr>td, .s-lg-4>tbody>tr>td, .s-lg-5>tbody>tr>td{font-size: 0 !important; line-height: 0 !important; height: 0 !important}.s-0>tbody>tr>td{font-size: 0 !important; line-height: 0 !important; height: 0 !important}.s-1>tbody>tr>td{font-size: 4px !important; line-height: 4px !important; height: 4px !important}.s-2>tbody>tr>td{font-size: 8px !important; line-height: 8px !important; height: 8px !important}.s-3>tbody>tr>td{font-size: 16px !important; line-height: 16px !important; height: 16px !important}.s-4>tbody>tr>td{font-size: 24px !important; line-height: 24px !important; height: 24px !important}.s-5>tbody>tr>td{font-size: 48px !important; line-height: 48px !important; height: 48px !important}}@media yahoo{.d-mobile{display: none !important}.d-desktop{display: block !important}.w-lg-25{width: 25% !important}.w-lg-25>tbody>tr>td{width: 25% !important}.w-lg-50{width: 50% !important}.w-lg-50>tbody>tr>td{width: 50% !important}.w-lg-75{width: 75% !important}.w-lg-75>tbody>tr>td{width: 75% !important}.w-lg-100{width: 100% !important}.w-lg-100>tbody>tr>td{width: 100% !important}.w-lg-auto{width: auto !important}.w-lg-auto>tbody>tr>td{width: auto !important}.p-lg-0>tbody>tr>td{padding: 0 !important}.pt-lg-0>tbody>tr>td, .py-lg-0>tbody>tr>td{padding-top: 0 !important}.pr-lg-0>tbody>tr>td, .px-lg-0>tbody>tr>td{padding-right: 0 !important}.pb-lg-0>tbody>tr>td, .py-lg-0>tbody>tr>td{padding-bottom: 0 !important}.pl-lg-0>tbody>tr>td, .px-lg-0>tbody>tr>td{padding-left: 0 !important}.p-lg-1>tbody>tr>td{padding: 4px !important}.pt-lg-1>tbody>tr>td, .py-lg-1>tbody>tr>td{padding-top: 4px !important}.pr-lg-1>tbody>tr>td, .px-lg-1>tbody>tr>td{padding-right: 4px !important}.pb-lg-1>tbody>tr>td, .py-lg-1>tbody>tr>td{padding-bottom: 4px !important}.pl-lg-1>tbody>tr>td, .px-lg-1>tbody>tr>td{padding-left: 4px !important}.p-lg-2>tbody>tr>td{padding: 8px !important}.pt-lg-2>tbody>tr>td, .py-lg-2>tbody>tr>td{padding-top: 8px !important}.pr-lg-2>tbody>tr>td, .px-lg-2>tbody>tr>td{padding-right: 8px !important}.pb-lg-2>tbody>tr>td, .py-lg-2>tbody>tr>td{padding-bottom: 8px !important}.pl-lg-2>tbody>tr>td, .px-lg-2>tbody>tr>td{padding-left: 8px !important}.p-lg-3>tbody>tr>td{padding: 16px !important}.pt-lg-3>tbody>tr>td, .py-lg-3>tbody>tr>td{padding-top: 16px !important}.pr-lg-3>tbody>tr>td, .px-lg-3>tbody>tr>td{padding-right: 16px !important}.pb-lg-3>tbody>tr>td, .py-lg-3>tbody>tr>td{padding-bottom: 16px !important}.pl-lg-3>tbody>tr>td, .px-lg-3>tbody>tr>td{padding-left: 16px !important}.p-lg-4>tbody>tr>td{padding: 24px !important}.pt-lg-4>tbody>tr>td, .py-lg-4>tbody>tr>td{padding-top: 24px !important}.pr-lg-4>tbody>tr>td, .px-lg-4>tbody>tr>td{padding-right: 24px !important}.pb-lg-4>tbody>tr>td, .py-lg-4>tbody>tr>td{padding-bottom: 24px !important}.pl-lg-4>tbody>tr>td, .px-lg-4>tbody>tr>td{padding-left: 24px !important}.p-lg-5>tbody>tr>td{padding: 48px !important}.pt-lg-5>tbody>tr>td, .py-lg-5>tbody>tr>td{padding-top: 48px !important}.pr-lg-5>tbody>tr>td, .px-lg-5>tbody>tr>td{padding-right: 48px !important}.pb-lg-5>tbody>tr>td, .py-lg-5>tbody>tr>td{padding-bottom: 48px !important}.pl-lg-5>tbody>tr>td, .px-lg-5>tbody>tr>td{padding-left: 48px !important}.s-lg-0>tbody>tr>td{font-size: 0 !important; line-height: 0 !important; height: 0 !important}.s-lg-1>tbody>tr>td{font-size: 4px !important; line-height: 4px !important; height: 4px !important}.s-lg-2>tbody>tr>td{font-size: 8px !important; line-height: 8px !important; height: 8px !important}.s-lg-3>tbody>tr>td{font-size: 16px !important; line-height: 16px !important; height: 16px !important}.s-lg-4>tbody>tr>td{font-size: 24px !important; line-height: 24px !important; height: 24px !important}.s-lg-5>tbody>tr>td{font-size: 48px !important; line-height: 48px !important; height: 48px !important}}</style></head><body style='outline: 0; width: 100%; min-width: 100%; height: 100%; -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%; font-family: Helvetica, Arial, sans-serif; line-height: 24px; font-weight: normal; font-size: 16px; -moz-box-sizing: border-box; -webkit-box-sizing: border-box; box-sizing: border-box; margin: 0; padding: 0; border: 0;'> <div class='preview' style='display: none; max-height: 0px; overflow: hidden;'> A new article has been posted on the website!                                                                </div><table valign='top' class='bg-dark body' style='outline: 0; width: 100%; min-width: 100%; height: 100%; -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%; font-family: Helvetica, Arial, sans-serif; line-height: 24px; font-weight: normal; font-size: 16px; -moz-box-sizing: border-box; -webkit-box-sizing: border-box; box-sizing: border-box; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: collapse; margin: 0; padding: 0; border: 0;' bgcolor='#343a40'> <tbody> <tr> <td valign='top' style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; margin: 0;' align='left' bgcolor='#343a40'> <table class='container' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: collapse; width: 100%;'> <tbody> <tr> <td align='center' style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; margin: 0; padding: 0 16px;'> <table align='center' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: collapse; width: 100%; max-width: 600px; margin: 0 auto;'> <tbody> <tr> <td style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; margin: 0;' align='left'> <table class='mx-auto' align='center' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: collapse; margin: 0 auto;'> <tbody> <tr> <td style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; margin: 0;' align='left'> <table class='s-4 w-100' border='0' cellpadding='0' cellspacing='0' style='width: 100%;'> <tbody> <tr> <td height='24' style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 24px; width: 100%; height: 24px; margin: 0;' align='left'>   </td></tr></tbody> </table><img class=' ' src='https://damiensblog.azurewebsites.net/Images/white-text.png' style='height: auto; line-height: 100%; outline: none; text-decoration: none; border: 0 none;'> <table class='s-3 w-100' border='0' cellpadding='0' cellspacing='0' style='width: 100%;'> <tbody> <tr> <td height='16' style='border-spacing: 0px; border-collapse: collapse; line-height: 16px; font-size: 16px; width: 100%; height: 16px; margin: 0;' align='left'>   </td></tr></tbody> </table> </td></tr></tbody> </table> <table class='card ' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: separate !important; border-radius: 4px; width: 100%; overflow: hidden; border: 1px solid #dee2e6;' bgcolor='#ffffff'> <tbody> <tr> <td style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; width: 100%; margin: 0;' align='left'> <div style='border-top-width: 5px; border-top-color: #007bff; border-top-style: solid;'> <table class='card-body' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: collapse; width: 100%;'> <tbody> <tr> <td style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; width: 100%; margin: 0; padding: 20px;' align='left'> <div> <h4 class='text-center' style='margin-top: 0; margin-bottom: 0; font-weight: 500; color: inherit; vertical-align: baseline; font-size: 24px; line-height: 28.8px;' align='center'>A new article has been posted!</h4> <h5 class='text-muted text-center' style='margin-top: 0; margin-bottom: 0; font-weight: 500; color: #636c72; vertical-align: baseline; font-size: 20px; line-height: 24px;' align='center'>" + blogPost.CreateDate.Month + "/" + blogPost.CreateDate.Day + "/" + blogPost.CreateDate.Year + " at " + blogPost.CreateDate.Hour + ":" + blogPost.CreateDate.Minute + "</h5> </div></td></tr></tbody> </table> </div></td></tr></tbody> </table> <table class='s-4 w-100' border='0' cellpadding='0' cellspacing='0' style='width: 100%;'> <tbody> <tr> <td height='24' style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 24px; width: 100%; height: 24px; margin: 0;' align='left'>   </td></tr></tbody> </table> <table class='card w-100 ' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: separate !important; border-radius: 4px; width: 100%; overflow: hidden; border: 1px solid #dee2e6;' bgcolor='#ffffff'> <tbody> <tr> <td style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; width: 100%; margin: 0;' align='left'> <div> <table class='card-body' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: collapse; width: 100%;'> <tbody> <tr> <td style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; width: 100%; margin: 0; padding: 20px;' align='left'> <div> <table class='mx-auto' align='center' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: collapse; margin: 0 auto;'> <tbody> <tr> <td style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; margin: 0;' align='left'> <img class='' src='" + Utilities.Utilities.GetBaseUrl() + "" + blogPost.MediaLink + "' style='height: 250px; line-height: 100%; outline: none; text-decoration: none; border: 0 none;'> </td></tr></tbody> </table> <h4 class='text-center' style='margin-top: 0; margin-bottom: 0; font-weight: 500; color: inherit; vertical-align: baseline; font-size: 24px; line-height: 28.8px;' align='center'>" + blogPost.Title + "</h4> <p class='text-center' style='line-height: 24px; font-size: 16px; margin: 0;' align='center'>" + blogPost.Summary + "</p><table class='s-2 w-100' border='0' cellpadding='0' cellspacing='0' style='width: 100%;'> <tbody> <tr> <td height='8' style='border-spacing: 0px; border-collapse: collapse; line-height: 8px; font-size: 8px; width: 100%; height: 8px; margin: 0;' align='left'>   </td></tr></tbody> </table> <table class='btn btn-primary btn-lg mx-auto ' align='center' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: separate !important; border-radius: 4px; margin: 0 auto;'> <tbody> <tr> <td style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; border-radius: 4px; margin: 0;' align='center' bgcolor='#007bff'> <a href='" + Utilities.Utilities.GetBaseUrl() + "" + Url.Action("Details", "BlogPosts", new { slug = blogPost.Slug }) + "' style='font-size: 20px; font-family: Helvetica, Arial, sans-serif; text-decoration: none; border-radius: 4.8px; line-height: 30px; display: inline-block; font-weight: normal; white-space: nowrap; background-color: #007bff; color: #ffffff; padding: 8px 16px; border: 1px solid #007bff;'>Read More</a> </td></tr></tbody> </table> </div></td></tr></tbody> </table> </div></td></tr></tbody> </table> <table class='s-4 w-100' border='0' cellpadding='0' cellspacing='0' style='width: 100%;'> <tbody> <tr> <td height='24' style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 24px; width: 100%; height: 24px; margin: 0;' align='left'>   </td></tr></tbody> </table> <table class='s-4 w-100' border='0' cellpadding='0' cellspacing='0' style='width: 100%;'> <tbody> <tr> <td height='24' style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 24px; width: 100%; height: 24px; margin: 0;' align='left'>   </td></tr></tbody> </table> </td></tr></tbody> </table> </td></tr></tbody> </table> </td></tr></tbody> </table></body></html>";
                        }
                        
                        List<string> Emails = new List<string>();
                        foreach (var sub in mailingList)
                        {
                            Emails.Add(sub.Email);
                        }
                        Utilities.Utilities.SendEmail(new EmailInformation
                        {
                            Reciepents = Emails,
                            Body = html,
                            Title = ConfigurationManager.AppSettings.Get("ApplicationName") + "-New Article For You!"
                        });

                        return RedirectToAction("Index");
                    }
                    else
                    {
                        return RedirectToAction("Error", "Home", new { errorText = "You must be a administrator or the writer to modify this post." });
                    }
                }
                else
                {
                    return RedirectToAction("Error", "Home", new { errorText = "You must be logged in to continue." });
                }
            }
            else
            {
                return RedirectToAction("Error", "Home", new { errorText = "There was a error with this action, let a system administartor know." });
            }
        }   
        public ActionResult Edit(string slug)
        {

            if (User.Identity.IsAuthenticated)
            {
                BlogPost blogPost = db.BlogPosts.FirstOrDefault(x=>x.Slug==slug);
                if(blogPost==null) return RedirectToAction("Error", "Home", new { errorText = "You must be a administrator or the writer to modify this post." });
                if  ( User.IsInRole("Admin") || User.Identity.GetUserId()==blogPost.AuthorId)
                {
                    return View(blogPost);
                }
                else
                {
                    return RedirectToAction("Error", "Home", new { errorText = "You must be a administrator or the writer to modify this post." });
                }
            }
            else
            {
                return RedirectToAction("Error", "Home", new { errorText = "You must be logged in to continue." });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Title,Body,MediaLink,UpdateReason,Listed")] BlogPost blogPost, string slug, HttpPostedFileBase uploadImage)
        {

            if (User.Identity.IsAuthenticated)
            {
                BlogPost post = db.BlogPosts.FirstOrDefault(x=>x.Slug==slug);
                if (post == null) return RedirectToAction("Error", "Home",new { errorText="Invalid post"});
                if (User.IsInRole("Admin") || User.Identity.GetUserId() == post.AuthorId)
                {
                    post.Body = blogPost.Body;
                    post.Title = blogPost.Title;
                    if (ImageUploadValidator.IsWebFriendlyImage(uploadImage))
                    {
                        var fileName = DateTime.Now.Ticks + ".png";
                        var path = Path.Combine(Server.MapPath("~/Uploads/"), fileName);
                        uploadImage.SaveAs(path);
                        post.MediaLink = "/Uploads/" + fileName;
                    }
                    else
                    {
                        post.MediaLink = blogPost.MediaLink;
                    }
                    post.UpdateReason = blogPost.UpdateReason;
                    post.UpdateDate = DateTime.Now;
                    post.Listed = blogPost.Listed;
                    db.Entry(post).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Details", "BlogPosts", new { slug=post.Slug });
                }
                else
                {
                    return RedirectToAction("Error", "Home", new { errorText = "You must be a administrator or the writer to modify this post." });
                }
            }
            else
            {
                return RedirectToAction("Error", "Home", new { errorText = "You must be logged in to continue." });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string slug)
        {

            if (User.Identity.IsAuthenticated)
            {
                BlogPost post = db.BlogPosts.FirstOrDefault(x => x.Slug == slug);
                if (post == null) return RedirectToAction("Error", "Home", new { errorText = "Invalid post" });
                if (User.IsInRole("Admin") || User.Identity.GetUserId() == post.AuthorId)
                {
                    BlogPost blogPost = db.BlogPosts.FirstOrDefault(x=>x.Slug==slug);
                    db.BlogPosts.Remove(blogPost);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    return RedirectToAction("Error", "Home", new { errorText = "You must be a administrator or the writer to modify this post." });
                }
            }
            else
            {
                return RedirectToAction("Error", "Home", new { errorText = "You must be logged in to continue." });
            }
        }
        public ActionResult Search(string searchText, int? page)
        {
            ViewBag.Search = searchText;
            int pageSize = 6;// display three blog posts at a time on this page
            int pageNumber = (page ?? 1);
            var comments = db.Comments.Where(x => x.Content.Contains(searchText) || 
                                                  x.Author.FirstName.Contains(searchText) ||
                                                  x.Author.LastName.Contains(searchText) ||
                                                  x.Author.DisplayName.Contains(searchText)).ToList();
            var blogposts = db.BlogPosts.Where(x => x.Title.Contains(searchText) || 
                                                    x.Body.Contains(searchText) ||
                                                    x.Author.FirstName.Contains(searchText) ||
                                                    x.Author.LastName.Contains(searchText) ||
                                                    x.Author.DisplayName.Contains(searchText)).ToList();
            List<object> results = new List<object>();
            results.AddRange(comments);
            results.AddRange(blogposts);
            return View("Results",results.ToPagedList(pageNumber,pageSize));
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public ActionResult ProfilePosts(string id, int? page)
        {
            int pageSize = 2;// display three blog posts at a time on this page
            int pageNumber = (page ?? 1);
            return PartialView(db.BlogPosts.Where(x => x.AuthorId == id).OrderByDescending(x=>x.CreateDate).ToPagedList(pageNumber, pageSize));
        }
        public ActionResult ProfileComments(string id, int? page)
        {
            int pageSize = 5;// display three blog posts at a time on this page
            int pageNumber = (page ?? 1);
            return PartialView(db.Comments.Where(x => x.AuthorId == id).OrderByDescending(x => x.CreateDate).ToPagedList(pageNumber,pageSize));
        }
    }
}
