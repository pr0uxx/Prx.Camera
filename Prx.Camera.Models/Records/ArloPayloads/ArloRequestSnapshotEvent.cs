using System.Text.Json;
using Prx.Camera.Models.Interfaces;

namespace Prx.Camera.Models.Records.ArloPayloads;

public sealed record ArloRequestSnapshotEvent() : ArloEventEnvelope("getSnapshot", 4), IAotJsonDeserializable<ArloRequestSnapshotEvent>
{
    public static ArloRequestSnapshotEvent? Deserialize(ReadOnlySpan<byte> buffer) =>
        JsonSerializer.Deserialize(buffer, Classes.SerializationContext.ArloJsonContext.Default.ArloRequestSnapshotEvent);

    public static ArloRequestSnapshotEvent[]? DeserializeArray(ReadOnlySpan<byte> buffer) =>
        JsonSerializer.Deserialize(buffer, Classes.SerializationContext.ArloJsonContext.Default.ArloRequestSnapshotEventArray);
}