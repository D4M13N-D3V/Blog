using Blog.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Utilities;

namespace Blog.Models
{
    public class BlogPostDetailsViewModel
    {
        public BlogPost BlogPost { get; set; }
        public TreeNode<Comment> CommentTree { get; set; }
        public int CommentCount { get; set; }
    }

}