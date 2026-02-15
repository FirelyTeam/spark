# Spark.Auth

OpenIddict-based OAuth 2.0 / OpenID Connect authorization server for Spark FHIR Server.

## Quick Start

### 1. Enable in appsettings.json

Enable the SMART on FHIR authorization server and define clients in `appsettings.json`.

For production (non-Development environment), also configure signing and encryption certificates:

```json
{
  "SmartAuth": {
    "Enabled": true,
    "AuthConnectionString": "mongodb://localhost:27017/spark-auth",
    "Certificates": {
      "SigningCertificatePath": "/path/to/signing.pfx",
      "SigningCertificatePassword": "<password>",
      "EncryptionCertificatePath": "/path/to/encryption.pfx",
      "EncryptionCertificatePassword": "<password>"
    }
  }
}
```

`SmartAuth:AuthConnectionString` is optional. If omitted, Spark.Auth uses `StoreSettings:ConnectionString`.

Optional endpoint overrides:

```json
{
  "SmartAuth": {
    "Endpoints": {
      "AuthorizationEndpointPath": "connect/authorize",
      "TokenEndpointPath": "connect/token",
      "EndSessionEndpointPath": "connect/endsession"
    }
  }
}
```

If omitted, Spark.Auth uses `connect/authorize`, `connect/token`, and `connect/endsession`.

### 2. Run the server

Clients defined in config are automatically created in MongoDB on startup if they don't already exist.

### 3. Endpoints

| Endpoint | URL |
| --- | --- |
| Authorization | `GET /connect/authorize` |
| Token | `POST /connect/token` |
| End Session | `GET /connect/endsession` |
| Discovery | `GET /.well-known/openid-configuration` |
| SMART Configuration | `GET /.well-known/smart-configuration` |

### 4. Test with client credentials

```bash
curl -X POST https://localhost:5001/connect/token \
  -d grant_type=client_credentials \
  -d client_id=smart-app \
  -d client_secret=SET_VIA_USER_SECRETS_OR_ENV
```

## Client Configuration

| Property | Required | Default | Description |
| --- | --- | --- | --- |
| `ClientId` | Yes | | OAuth client identifier |
| `ClientSecret` | No | | Client secret (omit for public clients) |
| `DisplayName` | No | ClientId | Human-readable name |
| `RedirectUris` | No | `[]` | Allowed redirect URIs |
| `PostLogoutRedirectUris` | No | `[]` | Allowed post-logout redirect URIs |
| `Scopes` | No | `[]` | Permitted scopes (`openid`, `email`, `profile`, `offline_access`, `roles`, or custom) |
| `GrantTypes` | No | `[]` | Permitted grants (`authorization_code`, `client_credentials`, `refresh_token`) |
| `RequirePkce` | No | `true` | Require PKCE for authorization code flow |

## Certificate setup

Development uses OpenIddict development certificates automatically. In non-Development environments, Spark.Auth requires configured PFX certificates for signing and encryption.

Quick OpenSSL setup:

```bash
openssl req -x509 -newkey rsa:2048 -keyout signing.key -out signing.crt -days 365 -nodes -subj "/CN=Spark FHIR Signing"
openssl pkcs12 -export -out signing.pfx -inkey signing.key -in signing.crt -passout pass:changeit
openssl req -x509 -newkey rsa:2048 -keyout encryption.key -out encryption.crt -days 365 -nodes -subj "/CN=Spark FHIR Encryption"
openssl pkcs12 -export -out encryption.pfx -inkey encryption.key -in encryption.crt -passout pass:changeit
```

Then set `SmartAuth:Certificates:*Path` and `*Password` to these `.pfx` files. Keep cert files/passwords out of source control.

## Architecture

See [ADR-0003](../../Documentation/ADR/0003-SMART.md) for design decisions and roadmap toward SMART on FHIR support.
