using System.Buffers;
using System.Net;
using System.Net.Sockets;
using Prx.Camera.Models.Interfaces;

namespace Prx.Camera.Services;

public interface ITcpListenerService
{
    Task StartAsync(CancellationToken ct = default);
}

public sealed class TcpListenerService(
    IArloProtocolParser parser,
    IArloEventHandler handler,
    ITcpLoggerService tcpLogger,
    ICameraSessionRegistry cameraSessionRegistry
    ) : ITcpListenerService
{
    public async Task StartAsync(CancellationToken ct = default)
    {
        TcpListener? listener = null;
        
        try
        {
            listener = new TcpListener(IPAddress.Any, 4000);
            listener.Start();

            while (!ct.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(ct);
                _ = ProcessClientAsync(client, ct);
            }
        }
        finally
        {
            listener?.Stop();
            listener?.Dispose();
        }
    }

    private async Task ProcessClientAsync(TcpClient client, CancellationToken ct = default)
    {
        var connectionId = Guid.NewGuid();
        await using var stream = client.GetStream();
        var buffer = ArrayPool<byte>.Shared.Rent(4096);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var read = await stream.ReadAsync(buffer, ct);
                if (read <= 0) break;

                tcpLogger.LogBuffer(connectionId, buffer.AsSpan(0, read));
                ulong? serialHash = null;
                
                var message = parser.Parse(buffer.AsSpan(0, read));
                if (message is not null)
                {
                    serialHash = await handler.HandleAsync(message.Value, client, ct);
                }

                if (ct.IsCancellationRequested && serialHash.HasValue) //or if tcp client is closed but idk how to check
                {
                    _ = cameraSessionRegistry.TryRemove(serialHash.Value);
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}