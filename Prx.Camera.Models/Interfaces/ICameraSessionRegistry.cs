using Prx.Camera.Models.Classes;

namespace Prx.Camera.Models.Interfaces;

public interface ICameraSessionRegistry
{
    bool TryAdd(ulong serialHash, CameraSession session);
    bool TryGet(ulong serialHash, out CameraSession? session);
    bool TryRemove(ulong serialHash);
    IReadOnlyCollection<CameraSession> GetAll();
}