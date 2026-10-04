using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LittleColony.Api.Infrastructure;

/// <summary>
/// Checks database connectivity without creating or modifying player data.
/// </summary>
public sealed class DynamoDbHealthCheck : IHealthCheck
{
    private readonly IAmazonDynamoDB database;

    /// <summary>
    /// Initializes the check with the application's shared database client.
    /// </summary>
    /// <param name="database">The configured DynamoDB client.</param>
    public DynamoDbHealthCheck(IAmazonDynamoDB database)
    {
        this.database = database;
    }

    /// <summary>
    /// Reports whether DynamoDB responds to a small, read-only request.
    /// </summary>
    /// <param name="context">The health check execution context.</param>
    /// <param name="cancellationToken">Cancels a probe when its deadline expires.</param>
    /// <returns>A connectivity result; this does not verify the cloud save schema.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await database.ListTablesAsync(
                new ListTablesRequest { Limit = 1 },
                cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (AmazonServiceException exception)
        {
            return HealthCheckResult.Unhealthy("DynamoDB is unavailable.", exception);
        }
        catch (HttpRequestException exception)
        {
            return HealthCheckResult.Unhealthy("DynamoDB is unavailable.", exception);
        }
        catch (TimeoutException exception)
        {
            return HealthCheckResult.Unhealthy("DynamoDB did not respond in time.", exception);
        }
    }
}
