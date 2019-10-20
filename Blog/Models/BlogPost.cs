using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Blog.Models
{
    public class BlogPost
    {
        //Constructor is setting a interface to the type of HashSet since HashSet implements the ICollection interface.
        public BlogPost()
        {
            //instantiate the blogpost with a new hashset of comment as the comments becaues hashsets are the most effecient for
            // working with data. could use list of comments .
            Comments = new HashSet<Comment>();
        }
        public int Id { get; set; }
        public string AuthorId { get; set; }
        public string Title { get; set; }
        [AllowHtml]
        public string Body { get; set; }
        public string Summary { get; set; }
        public string Slug { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string UpdateReason { get; set; }
        public string MediaLink { get; set; }
        public int ViewCount { get; set; }
        public int Reputation { get; set; }
        public bool Listed { get; set; }
        //Virtual Navigation  ( make sure there are references to the parents and children types and are able to store it )
        public virtual ICollection<Comment> Comments { get; set; }
        public virtual ApplicationUser Author { get; set; }
    }
}