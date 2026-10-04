using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace LittleColony.Api.Authentication;

/// <summary>Verifies Discord identities through server-side OAuth using only the identify scope.</summary>
public static class DiscordAuthentication
{
    /// <summary>Names the OAuth handler used for Discord challenges and callbacks.</summary>
    public const string Scheme = "Discord";

    /// <summary>Reports whether both Discord credentials are present without exposing their values.</summary>
    public static bool IsConfigured(IConfiguration configuration)
    {
        return !string.IsNullOrWhiteSpace(configuration["Authentication:Discord:ClientId"])
            && !string.IsNullOrWhiteSpace(configuration["Authentication:Discord:ClientSecret"]);
    }

    /// <summary>Registers Discord independently of Facebook while sharing the game's session cookie.</summary>
    public static void AddDiscord(AuthenticationBuilder authentication, IConfiguration configuration, CookieSecurePolicy cookieSecurity)
    {
        if (!IsConfigured(configuration))
        {
            return;
        }

        authentication.AddOAuth(Scheme, options =>
        {
            options.ClientId = configuration["Authentication:Discord:ClientId"]!;
            options.ClientSecret = configuration["Authentication:Discord:ClientSecret"]!;
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.CallbackPath = "/signin-discord";
            options.AuthorizationEndpoint = "https://discord.com/oauth2/authorize";
            options.TokenEndpoint = "https://discord.com/api/oauth2/token";
            options.UserInformationEndpoint = "https://discord.com/api/v10/users/@me";
            options.Scope.Add("identify");
            options.SaveTokens = false;
            options.CorrelationCookie.SameSite = SameSiteMode.Lax;
            options.CorrelationCookie.SecurePolicy = cookieSecurity;
            options.Events.OnCreatingTicket = CreatePlayerTicketAsync;
            options.Events.OnRemoteFailure = context =>
            {
                context.HandleResponse();
                context.Response.Redirect("/?login=failed");
                return Task.CompletedTask;
            };
        });
    }

    /// <summary>Fetches the authenticated Discord profile before issuing internal player claims.</summary>
    private static async Task CreatePlayerTicketAsync(OAuthCreatingTicketContext context)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
        using HttpResponseMessage response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
        response.EnsureSuccessStatusCode();
        using JsonDocument profile = JsonDocument.Parse(await response.Content.ReadAsStringAsync(context.HttpContext.RequestAborted));
        string externalId = profile.RootElement.GetProperty("id").GetString() ?? "";
        string name = profile.RootElement.TryGetProperty("global_name", out JsonElement globalName)
            ? globalName.GetString() ?? ""
            : "";
        if (string.IsNullOrWhiteSpace(name))
        {
            name = profile.RootElement.GetProperty("username").GetString() ?? "";
        }

        await GameAuthentication.IssuePlayerTicketAsync(context, "discord", externalId, name);
    }
}
