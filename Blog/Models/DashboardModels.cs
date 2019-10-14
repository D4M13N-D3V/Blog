using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Blog.Models
{
    public class DashboardViewModel
    {
        public DashboardViewModel()
        {
            Users = new HashSet<ApplicationUser>();
        }
        public ICollection<ApplicationUser> Users;
    }
}