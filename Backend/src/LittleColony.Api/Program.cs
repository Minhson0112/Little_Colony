using LittleColony.Api.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using LittleColony.Api.Authentication;
using LittleColony.Api.Players;
using LittleColony.Api.Saves;
using Amazon.Lambda.AspNetCoreServer.Hosting;

namespace LittleColony.Api;

/// <summary>
/// Hosts authenticated cloud saves, provider sign-in, the WebGL build, and dependency health endpoints.
/// </summary>
public sealed class Program
{
    /// <summary>
    /// Builds and runs the API using environment-specific configuration.
    /// </summary>
    /// <param name="args">Command-line configuration overrides.</param>
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
        builder.AddProductionHosting();
        await builder.LoadProductionAuthenticationAsync();
        builder.Services.AddProblemDetails();
        builder.Services.AddVillageDatabase(builder.Configuration, builder.Environment);
        builder.Services.AddSingleton<PlayerRepository>();
        builder.Services.AddSingleton<SaveRepository>();
        builder.AddGameAuthentication();
        builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 350 * 1024);

        string[] allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .WithMethods("GET", "POST", "PUT", "DELETE");
                }
            });
        });

        WebApplication app = builder.Build();
        app.UseProductionOrigin();
        app.UseExceptionHandler();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseWebGame();
        app.MapGameSessions();
        app.MapVillageSaves();

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        await app.RunAsync();
    }
}
