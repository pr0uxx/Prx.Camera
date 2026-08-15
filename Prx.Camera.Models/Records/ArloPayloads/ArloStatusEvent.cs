using System.Text.Json;
using System.Text.Json.Serialization;
using Prx.Camera.Models.Interfaces;

namespace Prx.Camera.Models.Records.ArloPayloads;

public record ArloStatusEvent() : ArloEventEnvelope("status", 7), IAotJsonDeserializable<ArloStatusEvent>
{
    public int Temperature { get; set; }

    [JsonPropertyName("BatPercent")] public byte BatteryPercent { get; set; }

    public static ArloStatusEvent? Deserialize(ReadOnlySpan<byte> buffer) =>
        JsonSerializer.Deserialize(buffer, Classes.SerializationContext.ArloJsonContext.Default.ArloStatusEvent);

    public static ArloStatusEvent[]? DeserializeArray(ReadOnlySpan<byte> buffer) =>
        JsonSerializer.Deserialize(buffer, Classes.SerializationContext.ArloJsonContext.Default.ArloStatusEventArray);
}