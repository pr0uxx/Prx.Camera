using System.Text.Json;
using Prx.Camera.Models.Interfaces;

namespace Prx.Camera.Models.Records.ArloPayloads;

public record ArloPong() : ArloEventEnvelope("pong", 3), IAotJsonDeserializable<ArloPong>
{
    public static ArloPong? Deserialize(ReadOnlySpan<byte> buffer) =>
        JsonSerializer.Deserialize(buffer, Classes.SerializationContext.ArloJsonContext.Default.ArloPong);

    public static ArloPong[]? DeserializeArray(ReadOnlySpan<byte> buffer) =>
        JsonSerializer.Deserialize(buffer, Classes.SerializationContext.ArloJsonContext.Default.ArloPongArray);
}