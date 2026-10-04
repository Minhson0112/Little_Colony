# Little Colony backend

ASP.NET Core .NET 8 backend for Unity WebGL: Facebook/Discord OAuth, HttpOnly cookie
sessions, and account-scoped cloud saves in DynamoDB. Credentials stay on the
server. Each provider's application must be configured before its real sign-in works.

## Run locally

From the Unity project root:

```powershell
docker compose -f Backend/compose.yaml up -d
dotnet restore Backend/LittleColony.Backend.sln --locked-mode
dotnet run --project Backend/src/LittleColony.Api --launch-profile local
```

Open http://localhost:5100 to play the existing `Builds/WebGL` build. The API
creates `LittleColonyPlayers` and `LittleColonySaves` only against DynamoDB Local
in Development. The named Docker volume retains data across restarts.
`GET /health/live` checks the process; `/health/ready` checks database connectivity
with ListTables, not table existence. Local startup requires DynamoDB to be up.

[Full Facebook setup, save behavior, API contract, and AWS preparation](../Docs/FACEBOOK_CLOUD_SAVE.md).
The `facebook-local` launch profile serves HTTPS on localhost:7100 after the
user configures a trusted development certificate and provider credentials.
The local Python WebGL server can proxy API/auth routes to port 5100 after it
is restarted, preserving the existing localhost:8080 guest-save origin.

## Structure

- `src/LittleColony.Api/Authentication`: Facebook/Discord OAuth and session/CSRF routes.
- `src/LittleColony.Api/Players`: stable internal IDs from verified provider IDs.
- `src/LittleColony.Api/Saves`: one main snapshot per player, conditional revisions,
  bounded JSON envelopes, identical-request retry handling.
- `src/LittleColony.Api/Infrastructure`: local database setup, health, WebGL hosting.
- `tests/LittleColony.Api.Checks`: real HTTP middleware and DynamoDB Local checks;
  only providers' external HTTP responses are simulated.

## Validation

Run the API checks from `Backend` (DynamoDB Local must be running):

```powershell
dotnet build LittleColony.Backend.sln --configuration Release --no-restore
dotnet run --configuration Release --no-build --project tests/LittleColony.Api.Checks
```

Tests create and clean up uniquely named test tables. They never write the
normal game tables. Run `Tools/DomainChecks` from the repository root for domain
and localization checks; build Unity separately with `Tools/build-webgl.ps1`.

## Deployment boundaries

There is no production infrastructure or AWS deployment yet. Production needs
pre-provisioned tables, IAM role permissions, HTTPS, a persistent shared Data
Protection key ring, secret management, and trusted proxy configuration. The
SDK uses the AWS credential chain when `DynamoDb:ServiceUrl` is omitted; the
local URL is restricted to Development and loopback. Use an IAM role on AWS.

The save API validates ownership and concurrency, not gameplay economics.
Discord setup is documented in [DISCORD_CLOUD_SAVE.md](../Docs/DISCORD_CLOUD_SAVE.md).
Account linking is not implemented. The current integration is
for a same-origin browser WebGL build; native Unity authentication needs its
own callback/session design. AWS infrastructure details are in the setup guide.
