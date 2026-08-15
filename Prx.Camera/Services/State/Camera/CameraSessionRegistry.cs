namespace Prx.Camera.Services.State.Camera;

using System.Collections.Concurrent;
using System.Collections.Generic;
using Models.Classes;
using Models.Interfaces;

public class CameraSessionRegistry : ICameraSessionRegistry
{
    private readonly ConcurrentDictionary<ulong, CameraSession> _sessions = new();

    public bool TryAdd(ulong serialHash, CameraSession session)
    {
        return _sessions.TryAdd(serialHash, session);
    }

    public bool TryGet(ulong serialHash, out CameraSession? session)
    {
        return _sessions.TryGetValue(serialHash, out session);
    }

    public bool TryRemove(ulong serialHash)
    {
        // ConcurrentDictionary.TryRemove requires an out parameter for the removed value
        return _sessions.TryRemove(serialHash, out _);
    }

    public IReadOnlyCollection<CameraSession> GetAll()
    {
        // .Values returns an ICollection, which doesn't directly implement IReadOnlyCollection in older frameworks, 
        // but casting to IReadOnlyCollection works in modern .NET, or you can just return it as a list/array.
        return (IReadOnlyCollection<CameraSession>)_sessions.Values; 
    }
}