using System.IO.Hashing;
using System.Text;
using Prx.Camera.Models.Interfaces;
using Prx.Camera.Models.Records;
using Prx.Camera.Models.Records.ArloPayloads;
using Prx.Camera.Models.Structs;

namespace Prx.Camera.Models.Classes;

public class CameraState : ICameraState
{
    private CameraState(CameraStateV1 cameraStateV1)
    {
        LastMotionUnix = cameraStateV1.LastMotionUnix;
        FirmwareHash = cameraStateV1.FirmwareHash;
        CapabilitiesHash = cameraStateV1.CapabilitiesHash;
        SerialHash = cameraStateV1.SerialHash;
        BatteryLevel = cameraStateV1.BatteryLevel;
        Temperature = cameraStateV1.Temperature;
    }

    private CameraState(ArloHandshakeRequest handshake, uint lastMotionUnix)
    {
        SerialHash = HashToUInt64(handshake.SystemSerialNumber);
        BatteryLevel = (byte)Math.Clamp(handshake.BatPercent, 0, 255);
        Temperature = (byte)Math.Clamp(handshake.Temperature + 40, 0, 255);
        LastMotionUnix = lastMotionUnix;
        FirmwareHash = HashToUInt32(handshake.SystemFirmwareVersion);
        CapabilitiesHash = HashCapabilities(handshake.Capabilities);
    }

    public ulong SerialHash { get; }
    public byte BatteryLevel { get; }
    public byte Temperature { get; }
    /// <summary>
    /// The unix timestamp for when motion was last detected for this device. Set to 0 if never. 
    /// </summary>
    public uint LastMotionUnix { get; }
    public uint FirmwareHash { get; }
    public uint CapabilitiesHash { get; }

    public static class From
    {
        public static CameraState V1(CameraStateV1 cameraState) => new(cameraState);
        public static CameraState Handshake(ArloHandshakeRequest handshake, uint lastMotionUnix = 0) => new(handshake, lastMotionUnix);
    }

    public static class To
    {
        public static CameraStateV1 V1(CameraState state) => new()
        {
            LastMotionUnix = state.LastMotionUnix,
            FirmwareHash = state.FirmwareHash,
            CapabilitiesHash = state.CapabilitiesHash,
            SerialHash = state.SerialHash,
            BatteryLevel = state.BatteryLevel,
            Temperature = state.Temperature
        };
    }

    private static ulong HashToUInt64(string? value)
    {
        if (string.IsNullOrEmpty(value)) return 0;
        return XxHash3.HashToUInt64(Encoding.UTF8.GetBytes(value));
    }

    private static uint HashToUInt32(string? value)
    {
        if (string.IsNullOrEmpty(value)) return 0;
        return XxHash32.HashToUInt32(Encoding.UTF8.GetBytes(value));
    }

    private static uint HashCapabilities(string[] capabilities)
    {
        if (capabilities.Length == 0) return 0;
        var joined = string.Join(',', capabilities.Order());
        return XxHash32.HashToUInt32(Encoding.UTF8.GetBytes(joined));
    }
}