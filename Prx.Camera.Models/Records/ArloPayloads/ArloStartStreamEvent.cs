using System.Text.Json;
using System.Text.Json.Serialization;
using Prx.Camera.Models.Interfaces;

namespace Prx.Camera.Models.Records.ArloPayloads;

public sealed record ArloStartStreamEvent()
    : ArloEventEnvelope(type: "startStream", id: 5), IAotJsonDeserializable<ArloStartStreamEvent>
{
    public ArloStartStreamEvent(uint streamId) : this()
    {
        StreamId = streamId;
    }

    [JsonPropertyName("StreamID")] public uint StreamId { get; init; }

    public string Resolution { get; init; } = "HD";

    public static ArloStartStreamEvent? Deserialize(ReadOnlySpan<byte> buffer) => JsonSerializer.Deserialize(buffer,
        Classes.SerializationContext.ArloJsonContext.Default.ArloStartStreamEvent);

    public static ArloStartStreamEvent[]? DeserializeArray(ReadOnlySpan<byte> buffer) =>
        JsonSerializer.Deserialize(buffer, Classes.SerializationContext.ArloJsonContext.Default.ArloStartStreamEventArray);
}