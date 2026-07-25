using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prx.Camera.Records;

namespace Prx.Camera.Services;

public class BaseStationService(
    ITcpListenerService listener,
    ILogger<BaseStationService> logger,
    IOptions<PrxCameraOptions> options
    ) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BaseStation service started");
        logger.LogInformation("Debug = {Debug}", options.Value.Debug);
        return listener.StartAsync(stoppingToken);
    }
}