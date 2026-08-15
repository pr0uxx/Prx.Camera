using System.Text.Json;
using Prx.Camera.Models.Interfaces;

namespace Prx.Camera.Models.Records.ArloPayloads;

public record ArloPing() : ArloEventEnvelope("ping", 4), IAotJsonDeserializable<ArloPing>
{
    public static ArloPing? Deserialize(ReadOnlySpan<byte> buffer) =>
        JsonSerializer.Deserialize(buffer, Classes.SerializationContext.ArloJsonContext.Default.ArloPing);

    public static ArloPing[]? DeserializeArray(ReadOnlySpan<byte> buffer) =>
        JsonSerializer.Deserialize(buffer, Classes.SerializationContext.ArloJsonContext.Default.ArloPingArray);
}