using System.ComponentModel.DataAnnotations;

namespace LittleColony.Api.Infrastructure;

/// <summary>
/// Configures the AWS Region and an optional local database endpoint.
/// </summary>
public sealed class DynamoDbOptions
{
    /// <summary>
    /// Identifies the configuration section containing database settings.
    /// </summary>
    public const string SectionName = "DynamoDb";

    /// <summary>
    /// Gets or sets the Region used for AWS requests and local request signing.
    /// </summary>
    [Required]
    public string Region { get; set; } = "ap-southeast-1";

    /// <summary>
    /// Gets or sets a loopback URL for DynamoDB Local in Development only.
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>Gets or sets the table for external identity to player mappings.</summary>
    [Required]
    public string PlayersTable { get; set; } = "LittleColonyPlayers";

    /// <summary>Gets or sets the table for versioned village snapshots.</summary>
    [Required]
    public string SavesTable { get; set; } = "LittleColonySaves";
}
