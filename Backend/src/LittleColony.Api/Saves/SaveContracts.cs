using System.Text.Json;

namespace LittleColony.Api.Saves;

/// <summary>Contains a client snapshot and the revision it is allowed to replace.</summary>
public sealed class SaveWriteRequest
{
    /// <summary>Gets the village serialization version, independent of its cloud revision.</summary>
    public int SchemaVersion { get; init; }

    /// <summary>Gets the last server revision seen by this client; zero creates a new save.</summary>
    public long ExpectedRevision { get; init; }

    /// <summary>Gets a unique identifier reused only when retrying the identical write.</summary>
    public string RequestId { get; init; } = "";

    /// <summary>Gets the existing Unity save object without changing its field or enum semantics.</summary>
    public JsonElement State { get; init; }
}

/// <summary>Contains the last committed village snapshot.</summary>
/// <param name="SchemaVersion">The Unity save format version.</param>
/// <param name="Revision">The monotonically increasing server revision.</param>
/// <param name="UpdatedAt">The server's commit time in UTC.</param>
/// <param name="State">The preserved Unity save object.</param>
public sealed record StoredSave(int SchemaVersion, long Revision, string UpdatedAt, JsonElement State);

/// <summary>Contains the result of an atomic conditional save write.</summary>
/// <param name="Committed">Whether this request was committed or was an identical retry.</param>
/// <param name="Save">The current server snapshot, when present.</param>
public sealed record SaveWriteResult(bool Committed, StoredSave? Save);
