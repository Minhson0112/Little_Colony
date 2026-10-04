using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using LittleColony.Api.Infrastructure;
using Microsoft.Extensions.Options;

namespace LittleColony.Api.Saves;

/// <summary>Stores one main village per player using optimistic concurrency and safe write retries.</summary>
public sealed class SaveRepository
{
    private readonly IAmazonDynamoDB database;
    private readonly string tableName;

    /// <summary>Creates a repository for the configured save table.</summary>
    public SaveRepository(IAmazonDynamoDB database, IOptions<DynamoDbOptions> options)
    {
        this.database = database;
        tableName = options.Value.SavesTable;
    }

    /// <summary>Reads the most recent committed snapshot for an authenticated player.</summary>
    public async Task<StoredSave?> ReadAsync(string playerId, CancellationToken cancellationToken)
    {
        Dictionary<string, AttributeValue>? item = await ReadItemAsync(playerId, cancellationToken);
        return item?.Count > 0 ? Decode(item) : null;
    }

    /// <summary>Atomically replaces only the revision read by the caller.</summary>
    /// <remarks>An identical retry is accepted only while its committed snapshot is still current.</remarks>
    public async Task<SaveWriteResult> WriteAsync(string playerId, SaveWriteRequest request, CancellationToken cancellationToken)
    {
        string json = request.State.GetRawText();
        string fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        var item = new Dictionary<string, AttributeValue>
        {
            ["playerId"] = new(playerId),
            ["schemaVersion"] = Number(request.SchemaVersion),
            ["revision"] = Number(request.ExpectedRevision + 1),
            ["updatedAt"] = new(DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)),
            ["stateJson"] = new(json),
            ["requestId"] = new(request.RequestId),
            ["fingerprint"] = new(fingerprint)
        };
        var write = new PutItemRequest
        {
            TableName = tableName,
            Item = item,
            ConditionExpression = request.ExpectedRevision == 0
                ? "attribute_not_exists(playerId)"
                : "#revision = :expected",
        };
        if (request.ExpectedRevision > 0)
        {
            write.ExpressionAttributeNames = new Dictionary<string, string> { ["#revision"] = "revision" };
            write.ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":expected"] = Number(request.ExpectedRevision) };
        }

        try
        {
            await database.PutItemAsync(write, cancellationToken);
            return new SaveWriteResult(true, Decode(item));
        }
        catch (ConditionalCheckFailedException)
        {
            Dictionary<string, AttributeValue>? current = await ReadItemAsync(playerId, cancellationToken);
            bool identicalRetry = current?.Count > 0
                && current["requestId"].S == request.RequestId
                && current["fingerprint"].S == fingerprint
                && current["revision"].N == (request.ExpectedRevision + 1).ToString(CultureInfo.InvariantCulture)
                && current["schemaVersion"].N == request.SchemaVersion.ToString(CultureInfo.InvariantCulture);
            return new SaveWriteResult(identicalRetry, current?.Count > 0 ? Decode(current) : null);
        }
    }

    /// <summary>Reads a player's item strongly consistently so retries see committed writes.</summary>
    private async Task<Dictionary<string, AttributeValue>?> ReadItemAsync(string playerId, CancellationToken cancellationToken)
    {
        GetItemResponse response = await database.GetItemAsync(new GetItemRequest
        {
            TableName = tableName,
            Key = new Dictionary<string, AttributeValue> { ["playerId"] = new(playerId) },
            ConsistentRead = true
        }, cancellationToken);
        return response.Item;
    }

    /// <summary>Converts an integer to DynamoDB's invariant number representation.</summary>
    private static AttributeValue Number(long value)
    {
        return new AttributeValue { N = value.ToString(CultureInfo.InvariantCulture) };
    }

    /// <summary>Reconstructs a snapshot while keeping its JSON owned independently of the parser.</summary>
    private static StoredSave Decode(Dictionary<string, AttributeValue> item)
    {
        using JsonDocument document = JsonDocument.Parse(item["stateJson"].S);
        return new StoredSave(
            int.Parse(item["schemaVersion"].N, CultureInfo.InvariantCulture),
            long.Parse(item["revision"].N, CultureInfo.InvariantCulture),
            item["updatedAt"].S,
            document.RootElement.Clone());
    }
}
