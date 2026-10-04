using System.Security.Cryptography;
using System.Text;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using LittleColony.Api.Infrastructure;
using Microsoft.Extensions.Options;

namespace LittleColony.Api.Players;

/// <summary>Maps a verified external provider identity to a stable internal player ID.</summary>
public sealed class PlayerRepository
{
    private readonly IAmazonDynamoDB database;
    private readonly string tableName;

    /// <summary>Creates a repository using the configured player table.</summary>
    public PlayerRepository(IAmazonDynamoDB database, IOptions<DynamoDbOptions> options)
    {
        this.database = database;
        tableName = options.Value.PlayersTable;
    }

    /// <summary>Returns one stable account even when first-time sign-ins arrive concurrently.</summary>
    /// <remarks>Call only with a provider ID verified by the authentication middleware, never a client claim.</remarks>
    public async Task<string> GetOrCreateAsync(string provider, string providerId, CancellationToken cancellationToken)
    {
        string identity = provider + ":" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(providerId)));
        var key = new Dictionary<string, AttributeValue> { ["identityId"] = new(identity) };
        GetItemResponse existing = await database.GetItemAsync(new GetItemRequest
        {
            TableName = tableName,
            Key = key,
            ConsistentRead = true
        }, cancellationToken);
        if (existing.Item?.Count > 0)
        {
            return existing.Item["playerId"].S;
        }

        string playerId = Guid.NewGuid().ToString("N");
        try
        {
            await database.PutItemAsync(new PutItemRequest
            {
                TableName = tableName,
                Item = new Dictionary<string, AttributeValue>(key)
                {
                    ["playerId"] = new(playerId),
                    ["createdAt"] = new(DateTimeOffset.UtcNow.ToString("O"))
                },
                ConditionExpression = "attribute_not_exists(identityId)"
            }, cancellationToken);
            return playerId;
        }
        catch (ConditionalCheckFailedException)
        {
            GetItemResponse winner = await database.GetItemAsync(new GetItemRequest
            {
                TableName = tableName,
                Key = key,
                ConsistentRead = true
            }, cancellationToken);
            return winner.Item["playerId"].S;
        }
    }
}
