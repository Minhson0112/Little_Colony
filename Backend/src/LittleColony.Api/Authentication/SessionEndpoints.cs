using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;

namespace LittleColony.Api.Authentication;

/// <summary>Exposes session discovery, provider entry, and CSRF-protected sign-out.</summary>
public static class SessionEndpoints
{
    /// <summary>Maps authentication routes without accepting arbitrary redirect destinations.</summary>
    public static void MapGameSessions(this WebApplication app)
    {
        app.MapGet("/api/session", GetSession);
        app.MapGet("/auth/facebook", (IConfiguration configuration) =>
        {
            return GameAuthentication.IsFacebookConfigured(configuration)
                ? Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }, [FacebookDefaults.AuthenticationScheme])
                : Results.Json(new { code = "facebook_not_configured" }, statusCode: StatusCodes.Status503ServiceUnavailable);
        });
        app.MapPost("/auth/logout", LogoutAsync).RequireAuthorization();
        app.MapGet("/auth/discord", (IConfiguration configuration) =>
        {
            return DiscordAuthentication.IsConfigured(configuration)
                ? Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }, [DiscordAuthentication.Scheme])
                : Results.Json(new { code = "discord_not_configured" }, statusCode: StatusCodes.Status503ServiceUnavailable);
        });
    }

    /// <summary>Returns account availability and a CSRF token for subsequent same-origin requests.</summary>
    private static IResult GetSession(HttpContext context, IConfiguration configuration, IAntiforgery antiforgery)
    {
        bool authenticated = context.User.Identity?.IsAuthenticated == true && context.User.HasClaim(claim => claim.Type == GameAuthentication.PlayerIdClaim);
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(new
        {
            authenticated,
            facebookEnabled = GameAuthentication.IsFacebookConfigured(configuration),
            discordEnabled = DiscordAuthentication.IsConfigured(configuration),
            provider = authenticated ? context.User.FindFirst("provider")?.Value ?? "" : "",
            playerId = authenticated ? context.User.FindFirst(GameAuthentication.PlayerIdClaim)!.Value : "",
            displayName = authenticated ? context.User.Identity!.Name ?? "" : "",
            csrfToken = antiforgery.GetAndStoreTokens(context).RequestToken
        });
    }

    /// <summary>Expires the server session only after validating the caller's CSRF token.</summary>
    private static async Task<IResult> LogoutAsync(HttpContext context, IAntiforgery antiforgery)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.BadRequest(new { code = "invalid_csrf_token" });
        }

        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.NoContent();
    }
}
