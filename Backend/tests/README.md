# API integration checks

Run from Backend with DynamoDB Local running:

```powershell
dotnet run --configuration Release --project tests/LittleColony.Api.Checks
```

This executable exercises the real ASP.NET Core Facebook/Discord middleware, cookie
session, CSRF handling, player repository and save repository. It simulates
Facebook/Discord token/profile HTTP responses in the test host only. No provider
credentials, production bypass route, or live Facebook calls are needed.

Each run creates isolated DynamoDB Local tables and deletes only those tables.
Checks cover forged callbacks, anonymous access, identity stability, account
isolation, CSRF, concurrent first logins, concurrent saves, idempotent retries,
stale writes, malformed/oversized payloads, returning login, and logout.

Discord checks also verify minimal scope, token exchange, display name fallback,
and identity isolation between providers with the same external ID.
It does not validate Meta/Discord app configuration or the Unity browser client.
Real provider sign-in remains a manual acceptance check after app setup.
