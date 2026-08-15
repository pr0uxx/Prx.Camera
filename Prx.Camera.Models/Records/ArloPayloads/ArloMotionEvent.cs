using System.Text.Json;
using System.Text.Json.Serialization;
using Prx.Camera.Models.Interfaces;

namespace Prx.Camera.Models.Records.ArloPayloads;

public sealed record ArloMotionEvent() : ArloEventEnvelope("motion", 6), IAotJsonDeserializable<ArloMotionEvent>
{
    public ArloMotionEvent(bool motionDetected) : this()
    {
        MotionDetected = motionDetected;
    }
    
    [JsonPropertyName("MotionDetected")] public bool MotionDetected { get; init; }

    public static ArloMotionEvent? Deserialize(ReadOnlySpan<byte> buffer)
        => JsonSerializer.Deserialize(buffer, Classes.SerializationContext.ArloJsonContext.Default.ArloMotionEvent);

    public static ArloMotionEvent[]? DeserializeArray(ReadOnlySpan<byte> buffer)
        => JsonSerializer.Deserialize(buffer, Classes.SerializationContext.ArloJsonContext.Default.ArloMotionEventArray);
}