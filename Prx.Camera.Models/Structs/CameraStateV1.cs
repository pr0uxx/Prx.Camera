using System.Runtime.InteropServices;

namespace Prx.Camera.Models.Structs;

//Never modify this, only ever create new versions
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CameraStateV1
{
    public ulong SerialHash;
    public byte BatteryLevel;
    public byte Temperature;
    public uint LastMotionUnix;
    public uint FirmwareHash;
    public uint CapabilitiesHash;
}