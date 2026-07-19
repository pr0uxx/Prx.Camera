using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Prx.Camera.Services;

public interface ITcpListenerService
{
    Task StartAsync(CancellationToken ct = default);
}

public sealed class TcpListenerService(
    IArloProtocolParser parser,
    IRegistrationHandler handler
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
        await using var stream = client.GetStream();
        var buffer = new byte[4096];

        while (!ct.IsCancellationRequested)
        {
            var read = await stream.ReadAsync(buffer, ct);
            if (read <= 0) break;

            var registration = parser.Parse(buffer.AsSpan(0, read));
            if (registration is not null)
            {
                await handler.HandleAsync(registration, ct);
            }
        }
    }
}