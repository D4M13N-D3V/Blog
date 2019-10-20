using Blog.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Blog.Models
{
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