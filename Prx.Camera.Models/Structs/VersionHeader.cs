using System.Runtime.InteropServices;

namespace Prx.Camera.Models.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VersionHeader
{
    public uint Magic;
    public byte Version;
}