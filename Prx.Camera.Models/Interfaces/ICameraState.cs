namespace Prx.Camera.Models.Interfaces;

public interface ICameraState
{
    public ulong SerialHash { get; }
    public byte BatteryLevel { get; }
    public byte Temperature { get; }
    public uint LastMotionUnix { get; }
    public uint FirmwareHash { get; }
    public uint CapabilitiesHash { get; }
}