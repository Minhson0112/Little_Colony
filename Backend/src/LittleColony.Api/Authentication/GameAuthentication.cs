using System.Security.Claims;
using LittleColony.Api.Players;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace LittleColony.Api.Authentication;

/// <summary>Configures account providers and a server-issued session cookie for the game.</summary>
public static class GameAuthentication
{
    /// <summary>Names the verified internal account identifier used for save authorization.</summary>
    public const string PlayerIdClaim = "player_id";

    /// <summary>Registers cookies and enables each provider when its server-side credentials exist.</summary>
    public static void AddGameAuthentication(this WebApplicationBuilder builder)
    {
        CookieSecurePolicy cookieSecurity = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        AuthenticationBuilder authentication = builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "LittleColony.Session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = cookieSecurity;
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });
        builder.Services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireClaim(PlayerIdClaim)
                .Build();
        });
        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "LittleColony.Csrf";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = cookieSecurity;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

        DiscordAuthentication.AddDiscord(authentication, builder.Configuration, cookieSecurity);
        if (!IsFacebookConfigured(builder.Configuration))
        {
            return;
        }

        authentication.AddFacebook(options =>
        {
            options.AppId = builder.Configuration["Authentication:Facebook:AppId"]!;
            options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"]!;
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.CallbackPath = "/signin-facebook";
            options.SaveTokens = false;
            options.Scope.Clear();
            options.Scope.Add("public_profile");
            options.Fields.Clear();
            options.Fields.Add("id");
            options.Fields.Add("name");
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

    /// <summary>Reports configuration availability without returning any credential values.</summary>
    public static bool IsFacebookConfigured(IConfiguration configuration)
    {
        return !string.IsNullOrWhiteSpace(configuration["Authentication:Facebook:AppId"])
            && !string.IsNullOrWhiteSpace(configuration["Authentication:Facebook:AppSecret"]);
    }

    /// <summary>Issues internal account claims only after the Facebook handler has verified the external identity.</summary>
    private static async Task CreatePlayerTicketAsync(OAuthCreatingTicketContext context)
    {
        string facebookId = context.User.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Facebook did not provide an account identifier.");
        string name = context.User.TryGetProperty("name", out var nameProperty) ? nameProperty.GetString() ?? "" : "";
        await IssuePlayerTicketAsync(context, "facebook", facebookId, name);
    }

    /// <summary>Maps a verified provider identity to a stable internal account without linking providers.</summary>
    internal static async Task IssuePlayerTicketAsync(OAuthCreatingTicketContext context, string provider, string externalId, string name)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            throw new InvalidOperationException("The provider did not provide an account identifier.");
        }

        PlayerRepository players = context.HttpContext.RequestServices.GetRequiredService<PlayerRepository>();
        string playerId = await players.GetOrCreateAsync(provider, externalId, context.HttpContext.RequestAborted);
        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, playerId),
            new Claim(PlayerIdClaim, playerId),
            new Claim(ClaimTypes.Name, name),
            new Claim("provider", provider)
        ], context.Scheme.Name));
    }
}
