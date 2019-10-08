using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Blog.Models
{
    public enum UpdateTypes { BLOG,COMMENT }

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
        public string Body { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public string UpdateReason { get; set; }

        //Virtual Navigation  ( make sure there are references to the parents and children types and are able to store it )
        public virtual ICollection<Comment> Comments { get; set; }
        public virtual ApplicationUser Author { get; set; }
    }

    public class Comment
    {
        public int Id { get; set; }
        public int BlogPostId { get; set; }
        public string AuthorId { get; set; }
        public DateTime CreateDate { get; set; }
        public string Content { get; set; }
        public string MediaUrl { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string UpdateReason { get; set; }
        public bool Published { get; set; }

        //Virtual Navigation  ( make sure there are references to the parents and children types and are able to store it )
        public virtual BlogPost BlogPost { get; set; }
        public virtual ApplicationUser Author { get; set; }
    }

    public class UpdateLog
    {
        public int Id { get; set; }
        public UpdateTypes Type { get; set; }
        public string AuthorId { get; set; }
        public int BlogPostId { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string UpdateReason { get; set; }

        //Virtual Navigation  ( make sure there are references to the parents and children types and are able to store it )
        public virtual BlogPost BlogPost { get; set; }
        public virtual ApplicationUser Author { get; set; }
    }

    public class MuteLog
    {
        public int Id { get; set; }
        public UpdateTypes Type { get; set; }
        public string ModId { get; set; }
        public string UserId { get; set; }
        public DateTime? MuteDate { get; set; }
        public string MuteReason { get; set; }

        //Virtual Navigation  ( make sure there are references to the parents and children types and are able to store it )
        public virtual BlogPost BlogPost { get; set; }
        public virtual ApplicationUser Mod { get; set; }
        public virtual ApplicationUser User { get; set; }
    }
}