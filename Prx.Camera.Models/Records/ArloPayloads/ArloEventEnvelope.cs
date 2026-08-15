using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prx.Camera.Models.Records.ArloPayloads;

public record ArloEventEnvelope
{
    public ArloEventEnvelope(string type, int id)
    {
        Id = id;
        Type = type;
    }

    public ArloEventEnvelope()
    {
        
    }
    
    [JsonPropertyName("ID")]
    public int Id { get; init; }
    
    [JsonPropertyName("Type")]
    public string? Type { get; init; }

    public static ArloEventEnvelope? DeserializeEnvelope(ReadOnlySpan<byte> buffer) =>
        JsonSerializer.Deserialize(buffer, Classes.SerializationContext.ArloJsonContext.Default.ArloEventEnvelope);
}