# Spark.Web

This is the front-end for Spark FHIR server.

**DISCLAIMER: This is meant as an reference web server for local testing, and should never be used as is in a production environment.**

## Build front-end assets

All source files for frontend are found in the `client` folder. To update front end assets cd into the folder, run `npm install` and `npm run build`. 


## Admin area

When running `Spark.Web` the solution will check if any admin user exists. If non exist, it will create an admin user with credentials read from `appsettings.json`. It is strongly recommended to change this password. The default credentials are: 

```
Username: admin@email.com
Password: Str0ngPa$$word
```


## Load examples

Visit `localhost:5555/admin/maintenance to load sample data.