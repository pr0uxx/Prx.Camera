using Microsoft.Extensions.Hosting;

namespace Prx.Camera.Services;

public class BaseStationService(ITcpListenerService listener) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return listener.StartAsync(stoppingToken);
    }
}