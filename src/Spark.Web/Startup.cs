/*
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Spark.Engine;
using Spark.Engine.Extensions;
using Spark.Mongo.Extensions;
using Spark.Web.Hubs;
using Spark.Web.Models.Config;
using Spark.Web.Services;
using System;
using System.Linq;

namespace Spark.Web;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        // Retain previous behavior of DateTime values defaulting to local time.
        // FIXME: Remove this in the future.
        services.AddControllersWithViews(options =>
            options.ModelBinderProviders.RemoveType<DateTimeModelBinderProvider>());

        // Bind to Spark and store settings from appSettings.json
        SparkSettings sparkSettings = new SparkSettings();
        Configuration.Bind("SparkSettings", sparkSettings);
        services.AddSingleton<SparkSettings>(sparkSettings);

        StoreSettings storeSettings = new StoreSettings();
        Configuration.Bind("StoreSettings", storeSettings);

        // Read examples settings from config
        ExamplesSettings examplesSettings = new ExamplesSettings();
        Configuration.Bind("ExamplesSettings", examplesSettings);
        services.Configure<ExamplesSettings>(options => Configuration.GetSection("ExamplesSettings").Bind(options));
        services.AddSingleton<ExamplesSettings>(examplesSettings);

        // Configure cookie policy
        services.Configure<CookiePolicyOptions>(options =>
        {
            // This lambda determines whether user consent for non-essential cookies is needed for a given request.
            options.CheckConsentNeeded = context => true;
            options.MinimumSameSitePolicy = SameSiteMode.None;
        });

        services.AddResponseCompression(options =>
        {
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                new[] { "application/fhir+json", "application/fhir+xml" });
        });

        // GitHub OAuth authentication (optional)
        var gitHubClientId = Configuration["GitHub:ClientId"];
        var gitHubClientSecret = Configuration["GitHub:ClientSecret"];
        var gitHubEnabled = !string.IsNullOrEmpty(gitHubClientId) && !string.IsNullOrEmpty(gitHubClientSecret);

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            if (gitHubEnabled)
            {
                options.DefaultChallengeScheme = "GitHub";
            }
        })
        .AddCookie(options =>
        {
            options.LoginPath = "/api/auth/login";
            options.LogoutPath = "/api/auth/logout";
            // Mark auth cookie as essential so it is not blocked by cookie consent,
            // and configure SameSite/SecurePolicy appropriately for OAuth flows.
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });

        if (gitHubEnabled)
        {
            authBuilder.AddGitHub(options =>
            {
                options.ClientId = gitHubClientId!;
                options.ClientSecret = gitHubClientSecret!;
                options.Scope.Add("user:email");
                options.SaveTokens = true;
            });
        }

        // Admin authorization based on allowlist
        services.AddTransient<IClaimsTransformation, AdminClaimsTransformation>();
        services.AddAuthorization();

        // Set up a default policy for CORS that accepts any origin, method and header.
        // only for test purposes.
        services.AddCors(options =>
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin();
                policy.AllowAnyMethod();
                policy.AllowAnyHeader();
            }));

        // Sets up the MongoDB store
        services.AddMongoFhirStore(storeSettings);

        // AddFhir also calls AddMvcCore
        services.AddFhir(sparkSettings);

        services.AddTransient<ServerMetadata>();

        // AddMvc needs to be called since we are using a Home page that is reliant on the full MVC framework
        services.AddMvc(options =>
        {
            options.EnableEndpointRouting = false;
        });

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Spark API", Version = "v1" });
        });

        services.AddSignalR();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IConfiguration configuration)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseDefaultFiles();
        app.UseStaticFiles();

        SeedUserDatabase(app.ApplicationServices, configuration);

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Spark API");
        });

        app.UseCors();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<MaintenanceHub>("/maintenanceHub").RequireAuthorization();
        });

        // UseFhir also calls UseMvc
        app.UseFhir(r =>
        {
            r.MapRoute(name: "default", template: "{controller}/{action}/{id?}");
            // SPA fallback - serve index.html for unmatched routes (client-side routing)
            r.MapRoute(name: "spa-fallback", template: "{*url}", defaults: new { controller = "Spa", action = "Index" });
        });
    }

    private static void SeedUserDatabase(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            ApplicationDbInitializer.SeedAdmin(context, userManager, configuration);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}
