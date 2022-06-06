using Autofac;
using Autofac.Integration.Mvc;
using Autofac.Integration.WebApi;
using Hl7.Fhir.Serialization;
using Microsoft.AspNet.SignalR;
using System.Diagnostics.Tracing;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Spark.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Formatters;
using Spark.Engine;
using Spark.Import;
using Spark.Infrastructure;
using Spark.Mongo;

namespace Spark
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            var builder = new ContainerBuilder();
            builder.AddMongoFhirStore(new StoreSettings { ConnectionString = Settings.MongoUrl, });
            builder.AddFhir(new SparkSettings
            {
                Endpoint = Settings.Endpoint,
                ParserSettings = new ParserSettings { PermissiveParsing = Settings.PermissiveParsing, },
                IndexSettings = new IndexSettings
                {
                    ClearIndexOnRebuild = Settings.ClearIndexOnRebuild, 
                    ReindexBatchSize = Settings.ReindexBatchSize
                },
            }, typeof(WebApiApplication).Assembly);

            builder.RegisterType<InitializerHub>().ExternallyOwned();

            var container = builder.Build();

            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
            GlobalConfiguration.Configuration.DependencyResolver = new AutofacWebApiDependencyResolver(container);
            GlobalHost.DependencyResolver = new Autofac.Integration.SignalR.AutofacDependencyResolver(container);

            //ConfigureLogging();
            GlobalConfiguration.Configure(this.Configure);
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        public void Configure(HttpConfiguration config)
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            config.AddFhir(Settings.PermissiveParsing);
        }

        private ObservableEventListener eventListener;
        private void ConfigureLogging()
        {
            eventListener = new ObservableEventListener();
            eventListener.EnableEvents(SparkEngineEventSource.Log, EventLevel.LogAlways,
                Keywords.All);
            eventListener.EnableEvents(SparkMongoEventSource.Log, EventLevel.LogAlways,
                Keywords.All);
            eventListener.EnableEvents(SemanticLoggingEventSource.Log, EventLevel.LogAlways, Keywords.All);
            var formatter = new JsonEventTextFormatter(EventTextFormatting.Indented);
            eventListener.LogToFlatFile(@".\spark.log", formatter);
        }


        protected void Application_End()
        {
            if (eventListener != null)
            {
                eventListener.DisableEvents(SemanticLoggingEventSource.Log);
                eventListener.DisableEvents(SparkMongoEventSource.Log);
                eventListener.DisableEvents(SparkEngineEventSource.Log);
                eventListener.Dispose();
            }
        }
    }

}
