using System.Net;
using System.Text;
using System.Text.Json;
using LittleColony.Api.Authentication;
using Amazon.DynamoDBv2;
using LittleColony.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LittleColony.Api.Checks;

/// <summary>Hosts the real API with isolated local tables and test-only provider HTTP transports.</summary>
internal sealed class CheckHost : WebApplicationFactory<LittleColony.Api.Program>
{
    private readonly string tablePrefix = "LittleColonyCheck" + Guid.NewGuid().ToString("N");
    private readonly bool facebookEnabled;
    private readonly bool discordEnabled;
    private readonly string? publicOrigin;
    private readonly string? originToken;

    /// <summary>Selects configured providers without reading or modifying the owner's credentials.</summary>
    public CheckHost(bool facebookEnabled = true, bool discordEnabled = true,
        string? publicOrigin = null, string? originToken = null)
    {
        this.facebookEnabled = facebookEnabled;
        this.discordEnabled = discordEnabled;
        this.publicOrigin = publicOrigin;
        this.originToken = originToken;
    }

    /// <summary>Gets the provider transport substituted only in this check executable.</summary>
    public FacebookBackchannel Facebook { get; } = new();

    /// <summary>Gets the test-only Discord transport for token and verified profile responses.</summary>
    public DiscordBackchannel Discord { get; } = new();

    /// <summary>Configures test-only credentials and isolated tables without changing application configuration files.</summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Hosting:PublicOrigin", publicOrigin ?? "");
        builder.UseSetting("Hosting:OriginToken", originToken ?? "");
        builder.UseSetting("AllowedHosts", publicOrigin == null ? "localhost;127.0.0.1" : "*");
        builder.UseSetting("Authentication:Facebook:AppId", "integration-app");
        builder.UseSetting("Authentication:Facebook:AppSecret", facebookEnabled ? "integration-secret" : "");
        builder.UseSetting("Authentication:Discord:ClientId", "integration-discord-app");
        builder.UseSetting("Authentication:Discord:ClientSecret", discordEnabled ? "integration-discord-secret" : "");
        builder.UseContentRoot(Path.GetFullPath("src/LittleColony.Api"));
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Facebook:AppId"] = "integration-app",
            ["Authentication:Facebook:AppSecret"] = facebookEnabled ? "integration-secret" : "",
            ["Authentication:Discord:ClientId"] = "integration-discord-app",
            ["Authentication:Discord:ClientSecret"] = discordEnabled ? "integration-discord-secret" : "",
            ["DynamoDb:ServiceUrl"] = "http://127.0.0.1:8000",
            ["DynamoDb:PlayersTable"] = tablePrefix + "Players",
            ["DynamoDb:SavesTable"] = tablePrefix + "Saves",
            ["WebClient:RootPath"] = "",
            ["Hosting:PublicOrigin"] = publicOrigin ?? "",
            ["Hosting:OriginToken"] = originToken ?? "",
            ["AllowedHosts"] = publicOrigin == null ? "localhost;127.0.0.1" : "*"
        }));
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            services.PostConfigure<FacebookOptions>(FacebookDefaults.AuthenticationScheme, options =>
            {
                options.Backchannel = new HttpClient(Facebook);
            });
            services.PostConfigure<OAuthOptions>(DiscordAuthentication.Scheme, options =>
            {
                options.Backchannel = new HttpClient(Discord);
            });
        });
    }

    /// <summary>Deletes only this run's uniquely named test tables and disposes the in-process host.</summary>
    public override async ValueTask DisposeAsync()
    {
        try
        {
            IAmazonDynamoDB database = Services.GetRequiredService<IAmazonDynamoDB>();
            DynamoDbOptions options = Services.GetRequiredService<IOptions<DynamoDbOptions>>().Value;
            foreach (string table in new[] { options.PlayersTable, options.SavesTable })
            {
                if (!table.StartsWith(tablePrefix, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Refusing to delete a non-test table.");
                }

                await database.DeleteTableAsync(table);
            }
        }
        finally
        {
            await base.DisposeAsync();
        }
    }
}

/// <summary>Simulates Discord HTTP endpoints while retaining the real OAuth correlation and cookie flow.</summary>
internal sealed class DiscordBackchannel : HttpMessageHandler
{
    /// <summary>Gets or sets the external identity returned by Discord for the next sign-in.</summary>
    public string UserId { get; set; } = "discord-alice";

    /// <summary>Gets or sets the optional display name to exercise username fallback.</summary>
    public string? GlobalName { get; set; } = "Discord player";

    /// <summary>Gets or sets whether profile retrieval fails instead of issuing a verified identity.</summary>
    public bool RejectProfile { get; set; }

    /// <summary>Returns strict token and profile responses without contacting Discord.</summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        object response;
        if (request.RequestUri!.AbsolutePath == "/api/oauth2/token")
        {
            string form = await request.Content!.ReadAsStringAsync(cancellationToken);
            if (request.Method != HttpMethod.Post
                || request.Content.Headers.ContentType?.MediaType != "application/x-www-form-urlencoded"
                || !form.Contains("grant_type=authorization_code", StringComparison.Ordinal)
                || !form.Contains("client_secret=integration-discord-secret", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Invalid Discord token exchange.");
            }

            response = new { access_token = "discord-integration-token", token_type = "Bearer", expires_in = 3600 };
        }
        else if (request.RequestUri.AbsolutePath == "/api/v10/users/@me"
            && request.Headers.Authorization?.ToString() == "Bearer discord-integration-token")
        {
            if (RejectProfile)
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            response = new { id = UserId, username = "discord-username", global_name = GlobalName };
        }
        else
        {
            throw new InvalidOperationException("Unexpected Discord request.");
        }

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json")
        };
    }
}

/// <summary>Replaces Facebook's token and profile endpoints while exercising the actual OAuth handler.</summary>
internal sealed class FacebookBackchannel : HttpMessageHandler
{
    /// <summary>Gets or sets the verified provider identity used in the next sequential sign-in.</summary>
    public string UserId { get; set; } = "facebook-alice";

    /// <summary>Returns deterministic provider responses without contacting Facebook or accepting real credentials.</summary>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        object response = request.RequestUri!.AbsolutePath.Contains("access_token", StringComparison.Ordinal)
            ? new { access_token = "integration-token", token_type = "bearer", expires_in = 3600 }
            : (object)new { id = UserId, name = "Integration player" };
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json")
        });
    }
}
