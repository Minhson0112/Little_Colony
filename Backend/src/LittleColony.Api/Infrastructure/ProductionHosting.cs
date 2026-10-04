using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
using System.Text;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace LittleColony.Api.Infrastructure;

/// <summary>Configures stable encrypted session keys and a canonical public HTTPS origin on AWS.</summary>
public static class ProductionHosting
{
    /// <summary>Loads only provider credentials from an optional encrypted AWS Parameter Store path.</summary>
    /// <param name="builder">The application builder with its Lambda IAM credentials and authentication path.</param>
    public static async Task LoadProductionAuthenticationAsync(this WebApplicationBuilder builder)
    {
        string? path = builder.Configuration["Authentication:ParameterPath"];
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var allowedKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "Facebook:AppId", "Facebook:AppSecret", "Discord:ClientId", "Discord:ClientSecret"
        };
        using var client = new AmazonSimpleSystemsManagementClient(new AmazonSimpleSystemsManagementConfig
        {
            Timeout = TimeSpan.FromSeconds(3),
            MaxErrorRetry = 0
        });
        string prefix = path.TrimEnd('/') + "/";
        string? nextToken = null;
        do
        {
            var response = await client.GetParametersByPathAsync(new GetParametersByPathRequest
            {
                Path = path,
                Recursive = true,
                WithDecryption = true,
                NextToken = nextToken
            });
            foreach (Parameter parameter in response.Parameters ?? [])
            {
                if (!parameter.Name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                string key = parameter.Name[prefix.Length..].Replace('/', ':');
                if (allowedKeys.Contains(key))
                {
                    builder.Configuration["Authentication:" + key] = parameter.Value;
                }
            }
            nextToken = response.NextToken;
        }
        while (!string.IsNullOrEmpty(nextToken));
    }

    /// <summary>Persists session keys in a dedicated encrypted Parameter Store path when configured.</summary>
    /// <param name="builder">The API builder with environment-specific production settings.</param>
    public static void AddProductionHosting(this WebApplicationBuilder builder)
    {
        string? keyPath = builder.Configuration["DataProtection:ParameterPath"];
        if (!string.IsNullOrWhiteSpace(keyPath))
        {
            builder.Services.AddDataProtection()
                .SetApplicationName("LittleColony")
                .PersistKeysToAWSSystemsManager(keyPath, options => options.KMSKeyId = "alias/aws/ssm");
        }

        // Validate before serving requests; the configured host must never come from a viewer header.
        GetPublicOrigin(builder.Configuration);
    }

    /// <summary>Requires the private CDN origin token and canonicalizes OAuth redirects when configured.</summary>
    /// <param name="app">The application whose authenticated endpoints run behind CloudFront.</param>
    public static void UseProductionOrigin(this WebApplication app)
    {
        Uri? origin = GetPublicOrigin(app.Configuration);
        string? originToken = app.Configuration["Hosting:OriginToken"];
        if (origin == null && string.IsNullOrEmpty(originToken))
        {
            return;
        }

        // Canonicalize to deployment configuration so a forged forwarded host cannot redirect OAuth.
        app.Use(async (context, next) =>
        {
            if (!string.IsNullOrEmpty(originToken)
                && !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(originToken),
                    Encoding.UTF8.GetBytes(context.Request.Headers["X-LittleColony-Origin"].ToString())))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            if (origin != null)
            {
                context.Request.Scheme = Uri.UriSchemeHttps;
                context.Request.Host = origin.IsDefaultPort
                    ? new HostString(origin.Host)
                    : new HostString(origin.Host, origin.Port);
            }
            await next(context);
        });
    }

    /// <summary>Validates an optional public origin as a root HTTPS URL without credentials or query data.</summary>
    /// <param name="configuration">The deployment settings containing PublicOrigin.</param>
    /// <returns>The canonical HTTPS origin, or null for existing local hosting.</returns>
    private static Uri? GetPublicOrigin(IConfiguration configuration)
    {
        string? value = configuration["Hosting:PublicOrigin"];
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? origin)
            || origin.Scheme != Uri.UriSchemeHttps
            || string.IsNullOrWhiteSpace(origin.Host)
            || !string.IsNullOrEmpty(origin.UserInfo)
            || origin.AbsolutePath != "/"
            || !string.IsNullOrEmpty(origin.Query)
            || !string.IsNullOrEmpty(origin.Fragment))
        {
            throw new InvalidOperationException("Hosting:PublicOrigin must be a root HTTPS URL.");
        }

        return origin;
    }
}
