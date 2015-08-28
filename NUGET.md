This document is for when we will deploy Spark on NUGET. Which hasn't happened yet.

# Spark as a service

How to get your own FHir server up an running with Spark.Engine 

## 1. Install packages
- Create an MVC/WebApi project
- Install nuget package Hl7.Fhir.Dstu2
- Install nuget package "Spark.Engine"
- Install nuget package "Spark.MongoStore"


## 2. Setup the Fhir Infrastructure
We have opened up the Fhir Server methods through a service call so that you can setup your own controller at a place of your preference and let you only implement those actions that are relevant for your project.
At the same time it gives you control over the actions. So to create your own FhirController do the following
	
	a. In Global.Asax.cs add the following line to the Application_Start()
			GlobalConfiguration.Configure(this.Configure);
			
	b. Add a function in the same class
			
			public void Configure(HttpConfiguration config)
			{
				config.EnableCors();
				config.AddFhir();
			}
	
	c. Create an Infrastructure // This is a form of inversion of control. We give the default for MongoDB by calling an Infrastructure extension here. But you can create and construct your own infrastructure.
	Create your own way of getting to your configuration variables (Endpoint, MongoUrl). 
	
			public static class InfrastructureProvider
			{
				public static Infrastructure Mongo;

				static InfrastructureProvider()
				{
					Mongo = Infrastructure.Default();
					Mongo.AddLocalhost(Settings.Endpoint);
					Mongo.AddMongo(Settings.MongoUrl);
				}
			}
	
## 3. Create an ApiController

a. Create a WebApi controller. This will be your FhirController.

	    public class FhirController : ApiController
		{
			public FhirController()
			{
				service = Factory.GetMongoFhirService();
			}
		}

b. Add actions to that controller. Example:

		[HttpGet, Route("{type}")]
		public FhirResponse Create(string type, Resource resource)
		{
			Key key = Key.Create(type);
			return service.Create(key, resource);   
		}

	
## 4. Done	
Your server should now be running.
If you use MongoDb, don't forget to start the MongoDb server.