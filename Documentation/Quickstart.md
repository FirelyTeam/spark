## Overview

This quickstart section will teach you how to set up a new FHIR Server from scratch using the NuGet packages with MongoDB as your storage media.

This tutorial assumes a local installation of MongoDB exists. Set up [MongoDB Community edition](https://docs.mongodb.com/manual/administration/install-community/) if you do not already have a local installation.

Sample code for this tutorial is located here: [spark-example](https://github.com/kennethmyhra/spark-example)


## Setting up the ASP.NET core application

First create an empty ASP.NET Core project:
```bash
dotnet new web --name spark-example
```

Add the core package:
```bash
dotnet add package Spark.Engine.R4
```

Add the MongoDB store:
```bash
dotnet add package Spark.Mongo.R4
```

For testing purposes restore one of the examples database. The database will be restored with the name `spark`.

Windows:
```bash
mongorestore /host:localhost /archive:.\.docker\linux\r4.archive.gz /gzip
```

Linux/Mac OS X: 
```bash
mongorestore --host=localhost --archive=./.docker/linux/r4.archive.gz --gzip
```

## Configuration and infrastrucuture

For the sake of simplicity in this tutorial all configuration is set up in code.

In Startup.cs add the following code:
```cs
public void ConfigureServices(IServiceCollection services)
{
    // Sets up DI context and adds neccessary infrastructure, like ASP.NET MVC
    services.AddFhir(new SparkSettings 
    {
        Endpoint = new Uri("https://localhost:5001/fhir") 
    }, 
    options => options.EnableEndpointRouting = false
    ).SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

    // Adds support for MongoDB Store
    services.AddMongoFhirStore(new StoreSettings
    {
        // Connection string for your MongoDB Store
        ConnectionString = "mongodb://localhost/spark-r4"
    });
}

public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    // Sets up internal middleware, as well as ASP.NET MVC
    app.UseFhir(r => r.MapRoute(name: "default", template: "{controller}/{id?}"));
}
```

## Defining the API endpoint

Add a new folder called Controllers to the root of your application, then add to that folder a new controller FhirController.cs.

In FhirController.cs add the following code:

```cs
[ApiController]
public class FhirController : ControllerBase
{
    // Interface which adds and retrieve resources.
    private readonly IFhirService _fhirService;
    // Settings for your server
    private readonly SparkSettings _settings;

    public FhirController(IFhirService fhirService, SparkSettings settings)
    {
        _fhirService = fhirService ?? throw new ArgumentNullException(nameof(fhirService));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }
}
```

### Read interaction
Add the read interaction to your FHIR server by adding the following method to FhirController.cs

```cs
[HttpGet("{type}/{id}")]
public ActionResult<FhirResponse> Read(string type, string id)
{
    ConditionalHeaderParameters parameters = new ConditionalHeaderParameters(Request);
    Key key = Key.Create(type, id);
    return new ActionResult<FhirResponse>(_fhirService.Read(key, parameters));
}
```

If you restored the example database you should now be able to retrieve your first FHIR resource:

```bash
curl -H "Accept: application/fhir+json; charset=utf-8" http://localhost:5000/Patient/example
```

### Search interaction
Add the search interaction to your FHIR Server by adding the following method to FhirController.cs:

```cs
[HttpGet("{type}")]
public FhirResponse Search(string type)
{
    int start = FhirParameterParser.ParseIntParameter(Request.GetParameter(FhirParameter.SNAPSHOT_INDEX)) ?? 0;
    var searchparams = Request.GetSearchParams();

    return _fhirService.Search(type, searchparams, start);
}
```

Try out the search interaction by running the following command:

```bash
curl -H "Accept: application/fhir+json; charset=utf-8" http://localhost:5000/Observation?subject=example
```

### Create interaction
Add the create interaction to your FHIR server by adding the following method to FhirController.cs:

```cs
[HttpPost("{type}")]
public FhirResponse Create(string type, Resource resource)
{
    Key key = Key.Create(type, resource?.Id);

    if (Request.Headers.ContainsKey(FhirHttpHeaders.IfNoneExist))
    {
        NameValueCollection searchQueryString = HttpUtility.ParseQueryString(Request.GetTypedHeaders().IfNoneExist());
        IEnumerable<Tuple<string, string>> searchValues =
            searchQueryString.Keys.Cast<string>()
                .Select(k => new Tuple<string, string>(k, searchQueryString[k]));

        return _fhirService.ConditionalCreate(key, resource, SearchParams.FromUriParamList(searchValues));
    }

    return _fhirService.Create(key, resource);
}
```

Try out the create interaction by running the following command:

```bash
curl -d '{"resourceType":"Patient","active":true,"name":[{"use":"official","family":"Doe","given":["John"]}],"gender":"male"}' -H "Content-Type: application/fhir+json" -X POST http://localhost:5000/Patient
```

### Update interaction
Add the update interaction to your FHIR server by adding the following method to FhirController.cs:

```cs
[HttpPut("{type}/{id?}")]
public ActionResult<FhirResponse> Update(string type, Resource resource, string id = null)
{
    string versionId = Request.GetTypedHeaders().IfMatch?.FirstOrDefault()?.Tag.Buffer;
    Key key = Key.Create(type, id, versionId);
    if(key.HasResourceId())
    {
        Request.TransferResourceIdIfRawBinary(resource, id);

        return new ActionResult<FhirResponse>(_fhirService.Update(key, resource));
    }
    else
    {
        return new ActionResult<FhirResponse>(_fhirService.ConditionalUpdate(key, resource,
            SearchParams.FromUriParamList(Request.TupledParameters())));
    }
}
```

Using the id from the create example try out the update interaction by running the following command:

```bash
curl -d '{"resourceType":"Patient","id":"6","active":true,"name":[{"use":"official","family":"Doe","given":["Jane"]}],"gender":"female"}' -H "Content-Type: application/fhir+json" -X PUT http://localhost:5000/Patient/6
```

### Delete interaction
Add the delete interaction to your FHIR server by adding the following method to FhirController.cs:

```cs
[HttpDelete("{type}/{id}")]
public FhirResponse Delete(string type, string id)
{
    Key key = Key.Create(type, id);
    FhirResponse response = _fhirService.Delete(key);
    return response;
}
```

Using the id from the create and update examples try out the delete interaction by running the following command:

```bash
curl -H "Accept: application/fhir+json" -X DELETE http://localhost:5000/Patient/6
```
