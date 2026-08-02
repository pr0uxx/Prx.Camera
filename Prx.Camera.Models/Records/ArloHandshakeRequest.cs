using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prx.Camera.Models.Records;

[JsonSerializable(typeof(ArloHandshakeRequest))]
[JsonSerializable(typeof(ArloHandshakeRequest[]))]
public sealed partial class ArloJsonContext : JsonSerializerContext
{
}


public sealed record ArloHandshakeRequest
{
    [JsonPropertyName("ID")]
    public int Id { get; init; }

    [JsonPropertyName("Type")]
    public string? Type { get; init; }

    [JsonPropertyName("SystemSerialNumber")]
    public string? SystemSerialNumber { get; init; }

    [JsonPropertyName("SystemModelNumber")]
    public string? SystemModelNumber { get; init; }

    [JsonPropertyName("SystemFirmwareVersion")]
    public string? SystemFirmwareVersion { get; init; }

    [JsonPropertyName("CommProtocolVersion")]
    public int CommProtocolVersion { get; init; }

    [JsonPropertyName("BatPercent")]
    public int BatPercent { get; init; }

    [JsonPropertyName("SignalStrengthIndicator")]
    public int SignalStrengthIndicator { get; init; }

    [JsonPropertyName("LogFrequency")]
    public int LogFrequency { get; init; }

    [JsonPropertyName("BatTech")]
    public string? BatTech { get; init; }

    [JsonPropertyName("ChargerTech")]
    public string? ChargerTech { get; init; }

    [JsonPropertyName("ChargingState")]
    public string? ChargingState { get; init; }

    [JsonPropertyName("ThermalShutdownRechargeMaxTemp")]
    public int ThermalShutdownRechargeMaxTemp { get; init; }

    [JsonPropertyName("Temperature")]
    public int Temperature { get; init; }

    [JsonPropertyName("InterfaceVersion")]
    public int InterfaceVersion { get; init; }

    [JsonPropertyName("Capabilities")] 
    public string[] Capabilities { get; init; } = [];

    [JsonPropertyName("HardwareRevision")]
    public string? HardwareRevision { get; init; }

    [JsonPropertyName("Sync")]
    public bool Sync { get; init; }

    [JsonPropertyName("BattChargeMinTemp")]
    public int BattChargeMinTemp { get; init; }

    [JsonPropertyName("BattChargeMaxTemp")]
    public int BattChargeMaxTemp { get; init; }

    [JsonPropertyName("ThermalShutdownMinTemp")]
    public int ThermalShutdownMinTemp { get; init; }

    [JsonPropertyName("ThermalShutdownMaxTemp")]
    public int ThermalShutdownMaxTemp { get; init; }

    [JsonPropertyName("BootSeconds")]
    public int BootSeconds { get; init; }

    public static ArloHandshakeRequest? Deserialize(ReadOnlySpan<byte> buffer)
    {
        return JsonSerializer.Deserialize(buffer, ArloJsonContext.Default.ArloHandshakeRequest);
    }

    public static ArloHandshakeRequest[]? DeserializeArray(ReadOnlySpan<byte> buffer)
    {
        return JsonSerializer.Deserialize(buffer, ArloJsonContext.Default.ArloHandshakeRequestArray);
    }
}