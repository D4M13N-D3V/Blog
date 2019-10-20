using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Blog.Models
{
    public class Subscription
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Your email address is required!")]
        public string Email { get; set; }
    }
}