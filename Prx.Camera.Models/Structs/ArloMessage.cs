using Prx.Camera.Models.Records;
using Prx.Camera.Models.Records.ArloPayloads;

namespace Prx.Camera.Models.Structs;

public enum ArloMessageKind : byte
{
    Unknown = 0,
    Registration,
    Motion,
    Status,
    Ping,
    Pong
}

public readonly struct ArloMessage
{
    public ArloMessageKind Kind { get; }
    public int Id { get; }
    public ArloHandshakeRequest? Registration { get; private init; }
    public ArloMotionEvent? Motion { get; private init; }
    public ArloStatusEvent? Status { get; private init; }
    public ArloPing? Ping { get; private init; }
    
    // Private constructors — one per variant
    private ArloMessage(ArloMessageKind kind, int id) { Kind = kind; Id = id; }

    // Static factory methods — the only way to construct
    public static ArloMessage FromRegistration(ArloHandshakeRequest r) =>
        new(ArloMessageKind.Registration, r.Id) { Registration = r };
    public static ArloMessage FromMotion(ArloMotionEvent m) =>
        new(ArloMessageKind.Motion, m.Id) { Motion = m };
    public static ArloMessage FromStatus(ArloStatusEvent s) =>
        new(ArloMessageKind.Status, s.Id) { Status = s };
    public static ArloMessage FromPing(int id) =>
        new(ArloMessageKind.Ping, id);

    // For unrecognised Type strings — log and move on
    public static ArloMessage Unrecognised(int id) =>
        new(ArloMessageKind.Unknown, id);
}