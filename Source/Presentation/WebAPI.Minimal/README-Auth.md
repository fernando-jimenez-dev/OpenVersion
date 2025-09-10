PAT/API-key protection

Overview
- The API can be protected by a simple Personal Access Token (PAT) / API-key middleware.
- It is opt-in and only enforces when a token is configured.
- In Development, enforcement is off by default (set PatAuth:EnforceInDevelopment=true to enable).

Headers accepted
- X-Api-Key: <token>
- Authorization: Bearer <token>

Configuration (App Service App Settings or environment variables)
- PatAuth:Enabled (bool, default true)
- PatAuth:EnforceInDevelopment (bool, default false)
- PatAuth:Token (string)

Examples (environment variables)
- Windows: setx PatAuth__Token  "<your-token>"
- Azure App Service: add App Setting key PatAuth__Token with value <your-token>

Notes
- If no token is configured, the middleware is a no-op.
- Use App Service slot settings/variable groups to keep tokens out of source control.
