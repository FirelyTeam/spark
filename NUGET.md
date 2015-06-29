This document is for when we will deploy Spark on NUGET. Which hasn't happened yet.

# Spark as a service

How to get your own FHir server up an running with Spark.Engine 

## Install package
- Install nuget package "Spark.Engine"
- Install nuget package "Spark.MongoStore"

## Set up your WebApi

To your Global.Asax.cs HttpApplication class , add a function: 	
		
		public static void Configure(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();
            config.AddFhir();
        }
		
To your Global.Asax.cs Application_Start(), before RouteConfig.RegisterRoutes... add:
		
		GlobalConfiguration.Configure(Configure);

		
Add a factory class to your project
	
	public static class Factory
    {
        static string MONGO_URI = "mongodb://localhost/fhir";
        static string FHIR_ENDPOINT = "http://yourwebsite.com/fhir";

        static volatile MongoStoreFactory storefactory = new MongoStoreFactory(MONGO_URI);
        static volatile Localhost localhost = new Localhost(FHIR_ENDPOINT);

        public static FhirService GetMongoFhirService()
        {
            return storefactory.MongoFhirService(localhost);
        }
    }		

## Add a FHIR controller
	
Add ApiController with an action, like this:

	[RoutePrefix("fhirapi")]
    public class FhirController : ApiController
    {
        public FhirController()
        {
            service = Factory.GetMongoFhirService();
        }

        FhirService service = null;

        [HttpGet, Route("{type}/{id}")]
        public FhirResponse Hello(string type, string id)
        {
            Key key = Key.CreateLocal(type, id);
            return service.Read(key);
        }
    }
		
## Done	
Your server should now be running.
		