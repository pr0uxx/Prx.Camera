using System.Text.Json;
using Microsoft.Extensions.Logging;
using Prx.Camera.Models.Records;
using Prx.Camera.Models.Records.ArloPayloads;
using Prx.Camera.Models.Structs;

namespace Prx.Camera.Services;

public interface IArloProtocolParser
{
    ArloMessage? Parse(ReadOnlySpan<byte> data);
}

public partial class ArloProtocolParser(ILogger<ArloProtocolParser> logger) : IArloProtocolParser
{
    public ArloMessage? Parse(ReadOnlySpan<byte> data)
    {
        try
        {
            LogProcessingData(logger, data.Length);

            if (!TrySliceJsonFromFrame(ref data))
                return ArloMessage.Unrecognised(0);

            var envelope = ArloEventEnvelope.DeserializeEnvelope(data);

            if (envelope is null) return ArloMessage.Unrecognised(0);

            return envelope.Type switch
            {
                "registration" => ArloMessage.FromRegistration(
                    ArloHandshakeRequest.Deserialize(data)!
                ),
                "motion" => ArloMessage.FromMotion(
                    ArloMotionEvent.Deserialize(data)!
                ),
                "status" => ArloMessage.FromStatus(
                    ArloStatusEvent.Deserialize(data)!
                ),
                "ping" => ArloMessage.FromPing(envelope.Id),
                _ => ArloMessage.Unrecognised(envelope.Id)
            };
        }
        catch (Exception e)
        {
            LogDeserializationError(logger, e);
        }

        return null;
    }

    private bool TrySliceJsonFromFrame(ref ReadOnlySpan<byte> frame)
    {
        try
        {
            if (frame.Length <= 2 || frame[0] != (byte)'L' || frame[1] != (byte)':') return false;
            var spaceIndex = frame.IndexOf((byte)' ');
            if (spaceIndex < 0) return false;
            frame = frame[(spaceIndex + 1)..];
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error parsing frame");
            return false;
        }
    }

    [LoggerMessage(LogLevel.Debug, "Parsing {Length} bytes from camera")]
    private static partial void LogProcessingData(ILogger logger, int length);

    [LoggerMessage(LogLevel.Error, "Error during deserialization")]
    private static partial void LogDeserializationError(ILogger logger, Exception e);
}