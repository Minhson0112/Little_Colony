using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;

namespace LittleColony.Api.Infrastructure;

/// <summary>Creates local development tables; production infrastructure must provision its own tables.</summary>
public sealed class LocalDatabaseSetup : IHostedService
{
    private readonly IAmazonDynamoDB database;
    private readonly DynamoDbOptions options;
    private readonly IHostEnvironment environment;

    /// <summary>Captures the configured database and environment.</summary>
    public LocalDatabaseSetup(IAmazonDynamoDB database, IOptions<DynamoDbOptions> options, IHostEnvironment environment)
    {
        this.database = database;
        this.options = options.Value;
        this.environment = environment;
    }

    /// <summary>Ensures the two local tables exist before accepting requests.</summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() || string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            return;
        }

        await EnsureTableAsync(options.PlayersTable, "identityId", cancellationToken);
        await EnsureTableAsync(options.SavesTable, "playerId", cancellationToken);
    }

    /// <summary>Leaves persisted database files intact when the host stops.</summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>Creates a missing table without deleting or replacing existing player data.</summary>
    private async Task EnsureTableAsync(string tableName, string key, CancellationToken cancellationToken)
    {
        try
        {
            await database.DescribeTableAsync(tableName, cancellationToken);
            return;
        }
        catch (ResourceNotFoundException)
        {
            // A missing table is expected on the first local run.
        }

        try
        {
            await database.CreateTableAsync(new CreateTableRequest
            {
                TableName = tableName,
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = [new KeySchemaElement(key, KeyType.HASH)],
                AttributeDefinitions = [new AttributeDefinition(key, ScalarAttributeType.S)]
            }, cancellationToken);
        }
        catch (ResourceInUseException)
        {
            // Another local host may have created the table concurrently.
        }
    }
}
