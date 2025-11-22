# Spark.Web

This is the front-end for Spark FHIR server.

**DISCLAIMER: This is meant as an reference web server for local testing, and should never be used as is in a production environment.**

## Build front-end assets

All source files for frontend are found in the `ClientApp` folder. To build front-end assets:

```bash
cd ClientApp
npm install        # First time only, or when dependencies change
npm run build:dev  # Development build (with source maps)
# or
npm run build      # Production build (minified)
```

**Note**: Front-end assets are automatically built when you run `dotnet build` or `dotnet run` via MSBuild targets. Manual builds are only needed if you want to build without building the .NET project.

## Admin area

When running `Spark.Web` the solution will check if any admin user exists. If non exist, it will create an admin user with credentials read from `appsettings.json`. It is strongly recommended to change this password. The default credentials are: 

```
Username: admin@email.com
Password: Str0ngPa$$word
```


## Load examples

Visit `localhost:5555/admin/maintenance to load sample data.