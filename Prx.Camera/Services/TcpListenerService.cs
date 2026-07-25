using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace Prx.Camera.Services;

public interface ITcpListenerService
{
    Task StartAsync(CancellationToken ct = default);
}

public sealed class TcpListenerService(
    IArloProtocolParser parser,
    IRegistrationHandler handler,
    ITcpLoggerService tcpLogger
    ) : ITcpListenerService
{
    public async Task StartAsync(CancellationToken ct = default)
    {
        var listener = new TcpListener(IPAddress.Any, 4000);
        listener.Start();

        while (!ct.IsCancellationRequested)
        {
            var client = await listener.AcceptTcpClientAsync(ct);
            _ = ProcessClientAsync(client, ct);
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

                tcpLogger.LogBuffer(connectionId, buffer);
                
                var registration = parser.Parse(buffer.AsSpan(0, read));
                if (registration is not null)
                {
                    await handler.HandleAsync(registration, ct);
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}