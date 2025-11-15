/*
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Spark.Web.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Add stuff here:
        modelBuilder.Entity<IdentityRole>().HasData(
            new IdentityRole
            {
                Id = "C7D34E41-7AAB-454D-AC9D-3E4AD3D72342",
                Name = "Admin",
                NormalizedName = "ADMIN",
                ConcurrencyStamp = "2D93347D-4137-4115-839B-3A4DA27E7059",
            });
    }
}
