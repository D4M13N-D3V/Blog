using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Blog.Models
{
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
}