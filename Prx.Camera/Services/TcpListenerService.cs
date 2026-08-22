using System.Buffers;
using System.Net;
using System.Net.Sockets;
using Prx.Camera.Models.Classes;

namespace Prx.Camera.Services;

public interface ITcpListenerService
{
    Task StartAsync(CancellationToken ct = default);
}

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class TcpListenerService(
    IArloProtocolParser parser,
    IArloEventHandler handler,
    ITcpLoggerService tcpLogger
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
        CameraSession? session = null;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var read = await stream.ReadAsync(buffer, ct);
                if (read <= 0) break; // Client disconnected gracefully

                tcpLogger.LogBuffer(connectionId, buffer.AsSpan(0, read));

                var message = parser.Parse(buffer.AsSpan(0, read));
                if (message is not null)
                {
                    // Update the serialHash if we get a new one, or keep the existing one 
                    var cameraSession = await handler.HandleAsync(message.Value, client, session, ct);
                    if (cameraSession is not null)
                    {
                        session = cameraSession;
                    }
                }
            }
        }
        catch (Exception e)
        {
            tcpLogger.LogError(e, connectionId, "Unhandled exception");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}