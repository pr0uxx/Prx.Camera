using Microsoft.Extensions.Logging;
using Prx.Camera.Models.Records;

namespace Prx.Camera.Services;

public interface IRegistrationHandler
{
    Task HandleAsync(ArloHandshakeRequest registration, CancellationToken ct = default);
}

public class RegistrationHandler(ILogger<RegistrationHandler> logger) : IRegistrationHandler
{
    public Task HandleAsync(ArloHandshakeRequest registration, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Camera {Model} ({Serial}) registered. Battery {Battery}%, Temp {Temp}°C",
            registration.SystemModelNumber,
            registration.SystemSerialNumber,
            registration.BatPercent,
            registration.Temperature
        );
        return Task.CompletedTask;
    }
}