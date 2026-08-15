using System.Text.Json;
using System.Text.Json.Serialization;
using Prx.Camera.Models.Interfaces;

namespace Prx.Camera.Models.Records.ArloPayloads;

public sealed record ArloRegistrationAck()
    : ArloEventEnvelope("registrationResponse", 2), IAotJsonSerializable<ArloRegistrationAck>
{
    [JsonPropertyName("ResponseCode")] public int ResponseCode { get; init; }

    public ArloRegistrationAck(int responseCode) : this()
    {
        ResponseCode = responseCode;
    }

    public void SerializeToStream(ref Stream stream)
    {
        JsonSerializer.Serialize(stream, this);
    }

    public byte[] SerializeToByteArray()
    {
        return JsonSerializer.SerializeToUtf8Bytes(this,
            Classes.SerializationContext.ArloJsonContext.Default.ArloRegistrationAck);
    }
}