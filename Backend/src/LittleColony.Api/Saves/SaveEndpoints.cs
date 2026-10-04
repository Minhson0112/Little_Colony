using System.Text;
using System.Text.Json;
using LittleColony.Api.Authentication;
using Microsoft.AspNetCore.Antiforgery;

namespace LittleColony.Api.Saves;

/// <summary>Exposes authenticated, account-scoped save reads and conditional writes.</summary>
public static class SaveEndpoints
{
    /// <summary>Maps the main save routes without accepting player IDs from request bodies or URLs.</summary>
    public static void MapVillageSaves(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/save").RequireAuthorization();
        group.MapGet("", ReadAsync);
        group.MapPut("", WriteAsync);
    }

    /// <summary>Returns only the signed-in player's village, or 404 for a new account.</summary>
    private static async Task<IResult> ReadAsync(HttpContext context, SaveRepository repository)
    {
        StoredSave? save = await repository.ReadAsync(context.User.FindFirst(GameAuthentication.PlayerIdClaim)!.Value, context.RequestAborted);
        context.Response.Headers.CacheControl = "no-store";
        return save == null ? Results.NotFound(new { code = "save_not_found" }) : Results.Ok(save);
    }

    /// <summary>Validates the CSRF token and bounded save envelope before making an atomic write.</summary>
    private static async Task<IResult> WriteAsync(HttpContext context, SaveWriteRequest request, SaveRepository repository, IAntiforgery antiforgery)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.BadRequest(new { code = "invalid_csrf_token" });
        }

        if (request.ExpectedRevision < 0 || request.ExpectedRevision == long.MaxValue
            || !Guid.TryParse(request.RequestId, out _)
            || request.SchemaVersion < 1 || request.SchemaVersion > 1000
            || request.State.ValueKind != JsonValueKind.Object
            || !request.State.TryGetProperty("version", out JsonElement version)
            || version.ValueKind != JsonValueKind.Number
            || !version.TryGetInt32(out int stateVersion) || stateVersion != request.SchemaVersion
            || !request.State.TryGetProperty("buildings", out JsonElement buildings)
            || buildings.ValueKind != JsonValueKind.Array)
        {
            return Results.BadRequest(new { code = "invalid_save" });
        }

        if (Encoding.UTF8.GetByteCount(request.State.GetRawText()) > 320 * 1024)
        {
            return Results.Json(new { code = "save_too_large" }, statusCode: StatusCodes.Status413PayloadTooLarge);
        }

        SaveWriteResult result = await repository.WriteAsync(
            context.User.FindFirst(GameAuthentication.PlayerIdClaim)!.Value,
            request,
            context.RequestAborted);
        context.Response.Headers.CacheControl = "no-store";
        return result.Committed
            ? Results.Ok(new { result.Save!.Revision, result.Save.UpdatedAt })
            : Results.Conflict(new { code = "save_conflict", revision = result.Save?.Revision ?? 0 });
    }
}
