# Spark.Web

This is the front-end for Spark FHIR server.

**DISCLAIMER: This is meant as a reference web server for local testing, and should never be used as is in a production environment.**

## Build front-end assets

All source files for frontend are found in the `app` folder. To update front end assets cd into the folder, run `npm install` and `npm run build`.

## Authentication (Optional)

Admin access is protected by GitHub OAuth. Without configuration, the server runs without authentication - the Admin section will simply not be available.

### Setting up GitHub OAuth

1. **Create a GitHub OAuth App:**
   - Go to https://github.com/settings/developers
   - Click "New OAuth App"
   - Fill in the details:
     - **Application name:** Spark FHIR Server (or your preferred name)
     - **Homepage URL:** `https://localhost:5001` (or your server URL)
     - **Authorization callback URL:** `https://localhost:5001/signin-github`
   - Click "Register application"
   - Copy the **Client ID**
   - Generate and copy a **Client Secret**

2. **Configure appsettings.json:**

   Add the following to your `appsettings.json` (or use [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) for development):

   ```json
   {
     "GitHub": {
       "ClientId": "your-client-id",
       "ClientSecret": "your-client-secret",
       "AdminUsers": ["your-github-username", "another-admin"]
     }
   }
   ```

3. **Configure Admin Users:**
   
   The `AdminUsers` array contains GitHub usernames or email addresses that should have admin access. Only users in this list will see the Admin menu and be able to access admin functionality.

### Using User Secrets (Recommended for Development)

To avoid committing secrets to source control:

```bash
cd src/Spark.Web
dotnet user-secrets init
dotnet user-secrets set "GitHub:ClientId" "your-client-id"
dotnet user-secrets set "GitHub:ClientSecret" "your-client-secret"
dotnet user-secrets set "GitHub:AdminUsers:0" "your-github-username"
```

## Load examples

Visit `https://localhost:5001/admin` to access maintenance operations and load sample data (requires GitHub OAuth configuration and admin access).