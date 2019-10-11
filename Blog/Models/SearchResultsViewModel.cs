using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Blog.Models
{
    public class SearchResultsViewModel
    {
        public SearchResultsViewModel()
        {
            BlogPosts = new HashSet<BlogPost>();
            Comments = new HashSet<Comment>();
        }
        public ICollection<BlogPost> BlogPosts;
        public ICollection<Comment> Comments;
    }
}