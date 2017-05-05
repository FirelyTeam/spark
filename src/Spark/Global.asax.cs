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
using Spark.Mock;
//using Spark.Mongo;

namespace Spark
{
	public class WebApiApplication : System.Web.HttpApplication
	{
		protected void Application_Start()
		{
			ConfigureLogging();
			GlobalConfiguration.Configure( this.Configure );
			AreaRegistration.RegisterAllAreas();
			FilterConfig.RegisterGlobalFilters( GlobalFilters.Filters );
			RouteConfig.RegisterRoutes( RouteTable.Routes );
			BundleConfig.RegisterBundles( BundleTable.Bundles );
		}

		public void Configure( HttpConfiguration config )
		{
			UnityConfig.RegisterComponents( config );
			GlobalConfiguration.Configure( WebApiConfig.Register );
			config.AddFhir();
		}

		private ObservableEventListener eventListener;
		private void ConfigureLogging()
		{
			eventListener = new ObservableEventListener();
			eventListener.EnableEvents( SparkEngineEventSource.Log, EventLevel.LogAlways,
				Keywords.All );
			eventListener.EnableEvents( MockEventSource.Log, EventLevel.LogAlways,
				Keywords.All );
			eventListener.EnableEvents( SemanticLoggingEventSource.Log, EventLevel.LogAlways, Keywords.All );
			var formatter = new JsonEventTextFormatter( EventTextFormatting.Indented );
			eventListener.LogToFlatFile( @"C:\projects\fhir\log\spark.log", formatter );
		}


		protected void Application_End()
		{
			if( eventListener != null )
			{
				eventListener.DisableEvents( SemanticLoggingEventSource.Log );
				eventListener.DisableEvents( MockEventSource.Log );
				eventListener.DisableEvents( SparkEngineEventSource.Log );
				eventListener.Dispose();
			}
		}
	}
}