using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LittleColony.Api.Players;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace LittleColony.Api.Checks;

/// <summary>Runs integration checks against the real API and DynamoDB Local.</summary>
internal static class Program
{
    private static int assertions;

    /// <summary>Verifies OAuth correlation, account isolation, CSRF, persistence, retries, and concurrent writes.</summary>
    public static async Task Main()
    {
        await using var host = new CheckHost();
        var clientOptions = new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        };
        using HttpClient alice = host.CreateClient(clientOptions);
        using HttpClient bob = host.CreateClient(clientOptions);
        Expect((await alice.GetAsync("/health/ready")).StatusCode == HttpStatusCode.OK, "Database readiness");
        Expect((await alice.GetAsync("/api/save")).StatusCode == HttpStatusCode.Unauthorized, "Anonymous save access rejected");
        Expect((await alice.PutAsJsonAsync("/api/save", Write(0, 10))).StatusCode == HttpStatusCode.Unauthorized, "Anonymous writes rejected");

        using HttpResponseMessage invalidCallback = await alice.GetAsync("/signin-facebook?code=forged&state=forged");
        Expect(invalidCallback.Headers.Location?.ToString() == "/?login=failed", $"Forged OAuth callback rejected ({invalidCallback.StatusCode}, {invalidCallback.Headers.Location})");
        JsonElement anonymous = await SessionAsync(alice);
        Expect(!anonymous.GetProperty("authenticated").GetBoolean(), "Forged callback does not authenticate");
        Expect(anonymous.GetProperty("facebookEnabled").GetBoolean(), "Test provider enabled");
        Expect(anonymous.GetProperty("discordEnabled").GetBoolean(), "Discord provider enabled");
        using HttpResponseMessage forgedDiscord = await bob.GetAsync("/signin-discord?code=forged&state=forged");
        Expect(forgedDiscord.Headers.Location?.ToString() == "/?login=failed", "Forged Discord callback rejected");
        Expect(!(await SessionAsync(bob)).GetProperty("authenticated").GetBoolean(), "Forged Discord callback leaves browser anonymous");

        string aliceId = await LoginAsync(host, alice, "facebook-alice");
        Expect(aliceId != "facebook-alice" && Guid.TryParseExact(aliceId, "N", out _), "Internal player identity issued");
        Expect((await alice.PutAsJsonAsync("/api/save", Write(0, 10))).StatusCode == HttpStatusCode.BadRequest, "Missing CSRF rejected");
        await AttachCsrfAsync(alice);
        Expect((await alice.GetAsync("/api/save")).StatusCode == HttpStatusCode.NotFound, "New account starts without cloud save");

        object initial = Write(0, 1000);
        using HttpResponseMessage created = await alice.PutAsJsonAsync("/api/save", initial);
        Expect(created.StatusCode == HttpStatusCode.OK, "First snapshot persisted");
        Expect((await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("revision").GetInt64() == 1, "First revision is one");
        Expect((await alice.PutAsJsonAsync("/api/save", initial)).StatusCode == HttpStatusCode.OK, "Identical retry succeeds without incrementing");
        JsonElement saved = await ReadSaveAsync(alice);
        Expect(saved.GetProperty("state").GetProperty("acorns").GetInt32() == 1000, "Snapshot read from database");
        Expect(saved.GetProperty("revision").GetInt64() == 1, "Retry preserves revision");

        string bobId = await LoginAsync(host, bob, "facebook-bob");
        await AttachCsrfAsync(bob);
        Expect(aliceId != bobId, "Different provider accounts get different players");
        Expect((await bob.GetAsync("/api/save")).StatusCode == HttpStatusCode.NotFound, "Other account cannot read Alice's save");
        Expect((await bob.PutAsJsonAsync("/api/save", Write(0, 25))).StatusCode == HttpStatusCode.OK, "Other account stores its own village");

        HttpResponseMessage[] concurrent = await Task.WhenAll(
            alice.PutAsJsonAsync("/api/save", Write(1, 1500)),
            alice.PutAsJsonAsync("/api/save", Write(1, 2500)));
        Expect(concurrent.Count(response => response.StatusCode == HttpStatusCode.OK) == 1, "Exactly one concurrent writer wins");
        Expect(concurrent.Count(response => response.StatusCode == HttpStatusCode.Conflict) == 1, "Stale writer gets a conflict");
        foreach (HttpResponseMessage response in concurrent)
        {
            response.Dispose();
        }

        Expect((await ReadSaveAsync(alice)).GetProperty("revision").GetInt64() == 2, "Conflict does not increment revision");
        Expect((await ReadSaveAsync(bob)).GetProperty("state").GetProperty("acorns").GetInt32() == 25, "Other account remains isolated");
        Expect((await alice.PutAsJsonAsync("/api/save", initial)).StatusCode == HttpStatusCode.Conflict, "Old retries cannot overwrite newer progress");
        Expect((await alice.PutAsJsonAsync("/api/save", new
        {
            schemaVersion = 9, expectedRevision = 2, requestId = Guid.NewGuid().ToString(), state = new { version = "bad", buildings = Array.Empty<object>() }
        })).StatusCode == HttpStatusCode.BadRequest, "Malformed schema version rejected safely");
        Expect((await alice.PutAsJsonAsync("/api/save", new
        {
            schemaVersion = 9, expectedRevision = 2, requestId = Guid.NewGuid().ToString(), state = new { version = 9, buildings = Array.Empty<object>(), oversized = new string('x', 330 * 1024) }
        })).StatusCode == HttpStatusCode.RequestEntityTooLarge, "Oversized snapshot rejected");

        PlayerRepository players = host.Services.GetRequiredService<PlayerRepository>();
        string[] duplicateLogins = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ =>
            players.GetOrCreateAsync("facebook", "concurrent-new-user", CancellationToken.None)));
        Expect(duplicateLogins.Distinct().Count() == 1, "Concurrent first logins share one player ID");
        string returningId = await LoginAsync(host, alice, "facebook-alice");
        Expect(returningId == aliceId, "Returning provider login retains the account");
        Expect((await ReadSaveAsync(alice)).GetProperty("revision").GetInt64() == 2, "Cloud save survives a new login");
        await AttachCsrfAsync(alice);
        Expect((await alice.PostAsync("/auth/logout", null)).StatusCode == HttpStatusCode.NoContent, "Valid logout succeeds");
        Expect((await alice.GetAsync("/api/save")).StatusCode == HttpStatusCode.Unauthorized, "Logout revokes this browser session");
        await CheckDiscordAsync(host, clientOptions, aliceId);
        await CheckProviderAvailabilityAsync(clientOptions);
        Console.WriteLine($"PASS: {assertions} API checks against DynamoDB Local. Provider HTTP responses simulated only inside this test executable.");
    }

    /// <summary>Verifies disabled providers fail clearly and Discord works without Facebook credentials.</summary>
    private static async Task CheckProviderAvailabilityAsync(WebApplicationFactoryClientOptions options)
    {
        await using var discordOnly = new CheckHost(facebookEnabled: false);
        using HttpClient client = discordOnly.CreateClient(options);
        JsonElement session = await SessionAsync(client);
        Expect(!session.GetProperty("facebookEnabled").GetBoolean()
            && session.GetProperty("discordEnabled").GetBoolean(), "Discord enabled independently of Facebook");
        Expect((await client.GetAsync("/auth/facebook")).StatusCode == HttpStatusCode.ServiceUnavailable, "Unconfigured Facebook returns 503");
        await LoginAsync(discordOnly, client, "discord-only", "discord");
        await using var disabled = new CheckHost(discordEnabled: false);
        using HttpClient unavailable = disabled.CreateClient(options);
        Expect(!(await SessionAsync(unavailable)).GetProperty("discordEnabled").GetBoolean(), "Missing Discord secret disables provider");
        using HttpResponseMessage response = await unavailable.GetAsync("/auth/discord");
        Expect(response.StatusCode == HttpStatusCode.ServiceUnavailable, "Unconfigured Discord returns 503");
        Expect((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString() == "discord_not_configured", "Unavailable Discord has stable error code");
    }

    /// <summary>Verifies Discord identity, profile fallback, save isolation, returning login and logout.</summary>
    private static async Task CheckDiscordAsync(CheckHost host, WebApplicationFactoryClientOptions options, string facebookId)
    {
        using HttpClient discord = host.CreateClient(options);
        string discordId = await LoginAsync(host, discord, "facebook-alice", "discord");
        Expect(discordId != facebookId, "Same external ID from different providers stays isolated");
        JsonElement session = await SessionAsync(discord);
        Expect(session.GetProperty("provider").GetString() == "discord", "Session identifies Discord for reauthentication");
        Expect(session.GetProperty("displayName").GetString() == "Discord player", "Discord global display name used");
        Expect((await discord.GetAsync("/api/save")).StatusCode == HttpStatusCode.NotFound, "Discord cannot read Facebook village");
        Expect((await discord.PutAsJsonAsync("/api/save", Write(0, 70))).StatusCode == HttpStatusCode.BadRequest, "Discord writes require CSRF");
        await AttachCsrfAsync(discord);
        Expect((await discord.PutAsJsonAsync("/api/save", Write(0, 70))).StatusCode == HttpStatusCode.OK, "Discord village persisted");
        Expect((await discord.PostAsync("/auth/logout", null)).StatusCode == HttpStatusCode.NoContent, "Discord logout succeeds");
        Expect((await discord.GetAsync("/api/save")).StatusCode == HttpStatusCode.Unauthorized, "Discord logout revokes session");
        host.Discord.GlobalName = null;
        Expect(await LoginAsync(host, discord, "facebook-alice", "discord") == discordId, "Returning Discord user retains internal account");
        Expect((await SessionAsync(discord)).GetProperty("displayName").GetString() == "discord-username", "Discord username used when global name absent");
        Expect((await ReadSaveAsync(discord)).GetProperty("state").GetProperty("acorns").GetInt32() == 70, "Discord village survives new login");
        using HttpClient failed = host.CreateClient(options);
        host.Discord.RejectProfile = true;
        using HttpResponseMessage challenge = await failed.GetAsync("/auth/discord");
        string state = QueryHelpers.ParseQuery(challenge.Headers.Location!.Query)["state"].ToString();
        using HttpResponseMessage denied = await failed.GetAsync("/signin-discord?code=test-code&state=" + Uri.EscapeDataString(state));
        Expect(denied.Headers.Location?.ToString() == "/?login=failed", "Failed Discord profile redirects safely");
        Expect(!(await SessionAsync(failed)).GetProperty("authenticated").GetBoolean(), "Failed profile cannot create a session");
        host.Discord.RejectProfile = false;
    }

    /// <summary>Builds an independent valid write request using the existing Unity save envelope.</summary>
    private static object Write(long revision, int acorns)
    {
        return new
        {
            schemaVersion = 9,
            expectedRevision = revision,
            requestId = Guid.NewGuid().ToString(),
            state = new { version = 9, acorns, savedAt = 1000L, buildings = Array.Empty<object>() }
        };
    }

    /// <summary>Completes the real correlation and cookie flow using test-only provider responses.</summary>
    private static async Task<string> LoginAsync(CheckHost host, HttpClient client, string providerId, string provider = "facebook")
    {
        host.Facebook.UserId = providerId;
        host.Discord.UserId = providerId;
        using HttpResponseMessage challenge = await client.GetAsync("/auth/" + provider);
        Expect(challenge.StatusCode == HttpStatusCode.Redirect, "OAuth redirect created");
        Uri redirect = challenge.Headers.Location ?? throw new InvalidOperationException("Missing provider redirect.");
        string state = QueryHelpers.ParseQuery(redirect.Query)["state"].ToString();
        Expect(state.Length > 0 && !redirect.ToString().Contains("integration-secret", StringComparison.Ordinal), "OAuth state present and secret not exposed");
        if (provider == "discord")
        {
            var query = QueryHelpers.ParseQuery(redirect.Query);
            Expect(redirect.Host == "discord.com" && query["scope"].ToString() == "identify", "Discord challenge requests only identify");
            Expect(query["redirect_uri"].ToString() == "https://localhost/signin-discord", "Discord callback stays on game origin");
            Expect(!redirect.ToString().Contains("integration-discord-secret", StringComparison.Ordinal), "Discord secret not exposed in redirect");
        }

        using HttpResponseMessage callback = await client.GetAsync("/signin-" + provider + "?code=test-code&state=" + Uri.EscapeDataString(state));
        Expect(callback.StatusCode == HttpStatusCode.Redirect && callback.Headers.Location?.ToString() == "/", "OAuth callback creates a game session");
        JsonElement session = await SessionAsync(client);
        Expect(session.GetProperty("authenticated").GetBoolean(), "Server cookie authenticates session");
        return session.GetProperty("playerId").GetString()!;
    }

    /// <summary>Reads the current browser session metadata.</summary>
    private static async Task<JsonElement> SessionAsync(HttpClient client)
    {
        return await client.GetFromJsonAsync<JsonElement>("/api/session");
    }

    /// <summary>Copies the current session's CSRF token into subsequent test requests.</summary>
    private static async Task AttachCsrfAsync(HttpClient client)
    {
        JsonElement session = await SessionAsync(client);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", session.GetProperty("csrfToken").GetString());
    }

    /// <summary>Reads a snapshot through the authenticated API.</summary>
    private static async Task<JsonElement> ReadSaveAsync(HttpClient client)
    {
        return await client.GetFromJsonAsync<JsonElement>("/api/save");
    }

    /// <summary>Fails immediately when an observable behavior differs from its contract.</summary>
    private static void Expect(bool condition, string description)
    {
        if (!condition)
        {
            throw new InvalidOperationException("FAIL: " + description);
        }

        assertions++;
    }
}
