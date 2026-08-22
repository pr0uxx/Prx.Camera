using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Prx.Camera.Models.Classes;
using Prx.Camera.Models.Records.ArloPayloads;
using Prx.Camera.Models.Structs;
using Prx.Camera.Services.State.Camera;

namespace Prx.Camera.Services;

public interface IArloEventHandler
{
    Task<CameraSession?> HandleAsync(
        ArloMessage message,
        TcpClient? tcpClient = null,
        CameraSession? session = null,
        CancellationToken ct = default
    );
}

// ReSharper disable once ClassNeverInstantiated.Global
public partial class ArloEventHandler(
    ILogger<ArloEventHandler> logger,
    ICameraStatePersistenceService cameraStateService
) : IArloEventHandler
{
    public async Task<CameraSession?> HandleAsync(
        ArloMessage message,
        TcpClient? tcpClient = null,
        CameraSession? session = null,
        CancellationToken ct = default
    )
    {
        switch (message.Kind)
        {
            case ArloMessageKind.Registration:
                return await HandleAsync(message.Registration!, tcpClient, ct);
            case ArloMessageKind.Ping:
                return session is not null ? await HandleAsync(session, ct) : null;
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

    private async Task<CameraSession?> HandleAsync(
        ArloHandshakeRequest registration,
        TcpClient? tcpClient,
        CancellationToken ct = default)
    {
        LogAttemptingToRegisterCameraModelSerial(registration.SystemModelNumber ?? "unknown model",
            registration.SystemSerialNumber ?? "unknown serial");

        if (tcpClient == null)
        {
            LogUnableToCommunicateWithCameraModelSerial(registration.SystemModelNumber ?? "unknown model",
                registration.SystemSerialNumber ?? "unknown serial");
            return null;
        }

        var state = CameraState.From.Handshake(registration);

        var networkStream = tcpClient.GetStream();

        var session = new CameraSession(state.SerialHash, networkStream)
        {
            State = state
        };

        await cameraStateService.StoreAsync([state]);

        LogCameraModelSerialRegistered(registration.SystemModelNumber ?? "unknown model",
            registration.SystemSerialNumber ?? "unknown serial");

        var response = new ArloRegistrationAck().SerializeToByteArray();

        var writeSuccess = await TryRespondAsync(response, session, ct);

        return writeSuccess ? session : null;
    }

    


    private async Task<CameraSession> HandleAsync(CameraSession session, CancellationToken ct)
    {
        logger.LogInformation("Received ping");

        var pong = new ArloPong().SerializeToByteArray();
        
        _ = await TryRespondAsync(pong, session, ct);

        return session;
    }
    
    private async Task<bool> TryRespondAsync(
        byte[] response, 
        CameraSession session, 
        CancellationToken ct = default
    )
    {
        const int maxWriteAttempts = 10;
        var writeSuccess = false;

        for (var i = 0; i < maxWriteAttempts; i++)
        {
            if (!session.Outbound.Writer.TryWrite(response))
            {
                if (i == maxWriteAttempts - 1)
                {
                    LogUnableToWriteOutboundArloResponseAfterMaxRetries();
                    //_ = cameraSessionRegistry.TryRemove(state.SerialHash);
                    break;
                }

                await session.Outbound.Writer.WaitToWriteAsync(ct);
            }
            else
            {
                LogSentOutboundRegistrationAck();
                writeSuccess = true;
                break;
            }
        }

        return writeSuccess;
    }

    [LoggerMessage(LogLevel.Information, "Attempting to register camera {Model} ({Serial}).")]
    partial void LogAttemptingToRegisterCameraModelSerial(string model, string serial);

    [LoggerMessage(LogLevel.Information, "Unable to communicate with  camera {Model} ({Serial}).")]
    partial void LogUnableToCommunicateWithCameraModelSerial(string model, string serial);

    [LoggerMessage(LogLevel.Information, "Camera {Model} ({Serial}) registered.")]
    partial void LogCameraModelSerialRegistered(string model, string serial);

    [LoggerMessage(LogLevel.Error, "Unable to write outbound arlo response after max retries")]
    partial void LogUnableToWriteOutboundArloResponseAfterMaxRetries();

    [LoggerMessage(LogLevel.Information, "Sent outbound registration ack")]
    partial void LogSentOutboundRegistrationAck();

    [LoggerMessage(LogLevel.Error, "Camera {Model} ({Serial}) could not be registered.")]
    partial void LogCameraModelSerialCouldNotBeRegistered(string model, string serial);
}