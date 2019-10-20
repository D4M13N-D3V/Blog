using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Blog.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public int ParentID { get; set; }
        public int BlogPostId { get; set; }
        public string AuthorId { get; set; }
        public DateTime CreateDate { get; set; }
        [AllowHtml]
        public string Content { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string UpdateReason { get; set; }

        //Virtual Navigation  ( make sure there are references to the parents and children types and are able to store it )
        public virtual BlogPost BlogPost { get; set; }
        public virtual ApplicationUser Author { get; set; }
    }
}