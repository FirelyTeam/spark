/*
 * Copyright (c) 2019-2024, Incendi (info@incendi.no)
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Spark.Web.Data
{
    public static class ApplicationDbInitializer
    {
        public static void SeedAdmin(ApplicationDbContext context, UserManager<IdentityUser> userManager, IConfiguration config)
        {
            context.Database.Migrate();

            string admin_email = config.GetValue<string>("Admin:Email");
            string admin_password = config.GetValue<string>("Admin:Password");

            if (userManager.FindByEmailAsync(admin_email).Result == null)
            {
                IdentityUser user = new IdentityUser
                {
                    UserName = admin_email,
                    Email = admin_email
                };

                IdentityResult result = userManager.CreateAsync(user, admin_password).Result;

                if (result.Succeeded)
                {
                    userManager.AddToRoleAsync(user, "Admin").Wait();
                }
            }
        }
    }
}