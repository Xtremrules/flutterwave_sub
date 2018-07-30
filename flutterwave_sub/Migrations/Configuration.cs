namespace flutterwave_sub.Migrations
{
    using flutterwave_sub.Models;
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<flutterwave_sub.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(flutterwave_sub.Models.ApplicationDbContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data.
            ApplicationRoleManager roleMgr = new ApplicationRoleManager(new RoleStore<IdentityRole>(context));
            ApplicationUserManager userMgr = new ApplicationUserManager(new UserStore<ApplicationUser>(context));

            if (!roleMgr.RoleExists("admin"))
                roleMgr.Create(new IdentityRole("admin"));
            if (!roleMgr.RoleExists("manager"))
                roleMgr.Create(new IdentityRole("manager"));
            if (!roleMgr.RoleExists("tenat"))
                roleMgr.Create(new IdentityRole("tenat"));

            ApplicationUser user = userMgr.FindByName("admin@email.com");
            if (user == null)
            {
                userMgr.Create(new ApplicationUser()
                {
                    Email = "admin@email.com",
                    UserName = "admin@email.com",
                    FirstName = "admin_firstname",
                    LastName = "admin_lastname",
                    PhoneNumber = "080ADMIN",
                }, "adminstrongPassword");
                user = userMgr.FindByName("admin@email.com");
            }

            if (!userMgr.IsInRole(user.Id, "admin"))
                userMgr.AddToRole(user.Id, "admin");

            context.SaveChanges();
        }
    }
}
