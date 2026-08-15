using System.Net.Sockets;
using System.Threading.Channels;

namespace Prx.Camera.Models.Classes;

public sealed class CameraSession(ulong serialHash, NetworkStream stream) : IAsyncDisposable
{
    public ulong SerialHash { get; } = serialHash; // Identity — from handshake
    public NetworkStream Stream { get; } = stream; // The live TCP connection
    public CameraState? State { get; set; }       // Last known state
    public Channel<byte[]> Outbound { get; } = Channel.CreateBounded<byte[]>(16); // backpressure at 16 queued commands
    // Commands queued for this camera

    // Created when a camera connects and completes registration

    public async ValueTask DisposeAsync()
    {
        Outbound.Writer.TryComplete();
        await Stream.DisposeAsync();
    }
}