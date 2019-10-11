namespace Blog.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using Microsoft.AspNet.Identity;
    using System.Linq;
    using Microsoft.AspNet.Identity.EntityFramework;
    using Blog.Models;

    internal sealed class Configuration : DbMigrationsConfiguration<Blog.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
        }

        protected override void Seed(Blog.Models.ApplicationDbContext context)
        {
            //adds intiial data to get the program started;
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));
            if (!context.Roles.Any(r => r.Name == "Admin")) roleManager.Create(new IdentityRole { Name = "Admin" });
            if (!context.Roles.Any(r => r.Name == "Writer")) roleManager.Create(new IdentityRole { Name = "Writer" });
            if (!context.Roles.Any(r => r.Name == "Moderator")) roleManager.Create(new IdentityRole { Name = "Moderator" });
            if (!context.Users.Any(u => u.UserName == "root@mailinator.com"))
            {
                var user = new ApplicationUser
                {
                    UserName = "root@mailinator.com",
                    Email = "root@mailinator.com",
                    FirstName = "Root",
                    LastName = "User",
                    DisplayName = "Root User",
                    Muted = false,
                };

                userManager.Create(user, "rootDemo1!");
                userManager.AddToRole(user.Id, "Admin");
            }
            if (!context.Users.Any(u => u.UserName == "moderator@coderfoundry.com"))
            {
                var user = new ApplicationUser
                {
                    UserName = "moderator@coderfoundry.com",
                    Email = "moderator@coderfoundry.com",
                    FirstName = "Coder",
                    LastName = "Foundry",
                    DisplayName = "Coder Foundry",
                    Muted = false,
                };

                userManager.Create(user, "rootDemo1!");
                userManager.AddToRole(user.Id, "Moderator");
            }
            if (!context.Users.Any(u => u.UserName == "writer@mailinator.com"))
            {
                var user = new ApplicationUser
                {
                    UserName = "writer@mailinator.com",
                    Email = "writer@mailinator.com",
                    FirstName = "Default",
                    LastName = "Writer",
                    DisplayName = "Default Writer",
                    Muted = false,
                };

                userManager.Create(user, "rootDemo1!");
                userManager.AddToRole(user.Id, "Writer");
            }
        }
    }
}
