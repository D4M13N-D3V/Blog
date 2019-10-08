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
            if (!context.Roles.Any(r => r.Name == "User")) roleManager.Create(new IdentityRole { Name = "User" });
            if (!context.Users.Any(u => u.UserName == "damien"))
            {
                var user = new ApplicationUser {
                    UserName = "root",
                    Email = "root@damiensblog.com",
                    FirstName ="Root", LastName="User",
                    DisplayName ="Root", Muted=false,};

                userManager.Create(user, "demoPassword!");
                userManager.AddToRole(user.Id, "Admin");
            }
        }
    }
}
