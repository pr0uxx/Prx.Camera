using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Prx.Camera.Models.Classes;
using Prx.Camera.Models.Interfaces;
using Prx.Camera.Models.Records;
using Prx.Camera.Models.Records.ArloPayloads;
using Prx.Camera.Models.Structs;
using Prx.Camera.Services.State.Camera;

namespace Prx.Camera.Services;

public interface IArloEventHandler
{
    Task<ulong?> HandleAsync(ArloMessage message, TcpClient? tcpClient = null, CancellationToken ct = default);
}

public class ArloEventHandler(
    ICameraSessionRegistry cameraSessionRegistry,
    ILogger<ArloEventHandler> logger,
    ICameraStatePersistenceService cameraStateService
) : IArloEventHandler
{
    public async Task<ulong?> HandleAsync(ArloMessage message, TcpClient? tcpClient = null, CancellationToken ct = default)
    {
        switch (message.Kind)
        {
            case ArloMessageKind.Registration:
                return await HandleAsync(message.Registration!, tcpClient, ct);
            case ArloMessageKind.Ping:
                return await HandleAsync(message.Ping!, ct);
            case ArloMessageKind.Unknown:
            case ArloMessageKind.Motion:
            case ArloMessageKind.Status:
            case ArloMessageKind.Pong:
                logger.LogError("{MessageKind} not yet supported", message.Kind);
                return null;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task<ulong?> HandleAsync(
        ArloHandshakeRequest registration,
        TcpClient? tcpClient,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "Attempting to register camera {Model} ({Serial}).",
            registration.SystemModelNumber ?? "unknown model",
            registration.SystemSerialNumber ?? "unknown serial"
        );

        if (tcpClient == null)
        {
            logger.LogInformation("Unable to communicate with  camera {Model} ({Serial}).",
                registration.SystemModelNumber ?? "unknown model",
                registration.SystemSerialNumber ?? "unknown serial");
            return null;
        }

        var state = CameraState.From.Handshake(registration);

        var networkStream = tcpClient.GetStream();

        var session = new CameraSession(state.SerialHash, networkStream)
        {
            State = state
        };

        if (cameraSessionRegistry.TryAdd(state.SerialHash, session))
        {
            await cameraStateService.StoreAsync([state]);

            logger.LogInformation(
                "Camera {Model} ({Serial}) registered.",
                registration.SystemModelNumber ?? "unknown model",
                registration.SystemSerialNumber ?? "unknown serial"
            );

            var response = new ArloRegistrationAck();
 
            var maxWriteAttempts = 10;
            var writeSuccess = false;

            for (var i = 0; i < maxWriteAttempts; i++)
            {
                if (!session.Outbound.Writer.TryWrite(response.SerializeToByteArray()))
                {
                    if (i == maxWriteAttempts - 1)
                    {
                        logger.LogError("Unable to write outbound arlo response after max retries");
                        _ = cameraSessionRegistry.TryRemove(state.SerialHash);
                        break;
                    }
                    
                    await session.Outbound.Writer.WaitToWriteAsync(ct);
                }
                else
                {
                    logger.LogInformation("Sent outbound registration ack");
                    writeSuccess =  true;
                    break;
                }
            }
            
            return writeSuccess ? state.SerialHash : null;
        }
        
        logger.LogError(
            "Camera {Model} ({Serial}) could not be registered.",
            registration.SystemModelNumber ?? "unknown model",
            registration.SystemSerialNumber ?? "unknown serial"
        );

        return null;
    }


    private async Task<ulong?> HandleAsync(ArloPing messagePing, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}