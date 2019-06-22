using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spark.Engine;
using Spark.Engine.Extensions;
using Spark.Mongo;
using System;

namespace Spark.NetCore
{
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
            SparkSettings sparkSettings = new SparkSettings();
            Configuration.Bind("SparkSettings", sparkSettings);
            MongoStoreSettings storeSettings = new MongoStoreSettings();
            Configuration.Bind("MongoStoreSettings", storeSettings);

            services.AddMongoFhirStore(storeSettings);
            services.AddFhir(sparkSettings).SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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

            app.UseHttpsRedirection();
            app.UseFhir(r => r.MapRoute(name: "default", template: "{controller}/{action}/{id?}", defaults: new { controller = "Home", action = "Index" }));
        }
    }
}
