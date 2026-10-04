using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Microsoft.Extensions.Options;

namespace LittleColony.Api.Infrastructure;

/// <summary>
/// Registers database connectivity without coupling it to gameplay rules.
/// </summary>
public static class DynamoDbServiceCollectionExtensions
{
    /// <summary>
    /// Adds validated database settings, the AWS client, and a readiness check.
    /// </summary>
    /// <param name="services">The application's service registrations.</param>
    /// <param name="configuration">Environment-specific database configuration.</param>
    /// <param name="environment">The environment permitted to use local credentials.</param>
    /// <returns>The service collection for further registrations.</returns>
    public static IServiceCollection AddVillageDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOptions<DynamoDbOptions>()
            .Bind(configuration.GetSection(DynamoDbOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                options => IsEndpointAllowed(options, environment),
                "DynamoDb:ServiceUrl must be an HTTP(S) loopback URL in Development, or empty for AWS.")
            .ValidateOnStart();

        services.AddSingleton<IAmazonDynamoDB>(provider =>
        {
            DynamoDbOptions options = provider
                .GetRequiredService<IOptions<DynamoDbOptions>>()
                .Value;

            return CreateClient(options);
        });

        services.AddHealthChecks()
            .AddCheck<DynamoDbHealthCheck>(
                "dynamodb",
                tags: ["ready"],
                timeout: TimeSpan.FromSeconds(3));
        services.AddHostedService<LocalDatabaseSetup>();

        return services;
    }

    /// <summary>
    /// Restricts local credentials to explicitly configured development endpoints.
    /// </summary>
    /// <param name="options">The proposed database connection settings.</param>
    /// <param name="environment">The application's hosting environment.</param>
    /// <returns>Whether the endpoint is safe to use with the selected environment.</returns>
    private static bool IsEndpointAllowed(
        DynamoDbOptions options,
        IHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            return true;
        }

        return environment.IsDevelopment()
            && Uri.TryCreate(options.ServiceUrl, UriKind.Absolute, out Uri? endpoint)
            && endpoint.IsLoopback
            && (endpoint.Scheme == Uri.UriSchemeHttp || endpoint.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Creates a local client with dummy credentials or an AWS client using its credential chain.
    /// </summary>
    /// <param name="options">Validated connection settings.</param>
    /// <returns>A database client managed and disposed by the service container.</returns>
    private static IAmazonDynamoDB CreateClient(DynamoDbOptions options)
    {
        var clientConfiguration = new AmazonDynamoDBConfig
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region),
            Timeout = TimeSpan.FromSeconds(2),
            MaxErrorRetry = 0
        };

        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            clientConfiguration.ServiceURL = options.ServiceUrl;
            clientConfiguration.AuthenticationRegion = options.Region;

            return new AmazonDynamoDBClient(
                new BasicAWSCredentials("local", "local"),
                clientConfiguration);
        }

        return new AmazonDynamoDBClient(clientConfiguration);
    }
}
