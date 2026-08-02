using Microsoft.Extensions.Logging;
using Prx.Camera.Models.Records;

namespace Prx.Camera.Services;

public interface IArloProtocolParser
{
    ArloHandshakeRequest? Parse(ReadOnlySpan<byte> data);
}

public partial class ArloProtocolParser(ILogger<ArloProtocolParser> logger) : IArloProtocolParser
{
    public ArloHandshakeRequest? Parse(ReadOnlySpan<byte> data)
    {
        try
        {
            LogProcessingData(logger, data.Length);

            if (TrySliceJsonFromFrame(ref data))
            {
                return data[0] == (byte) '['
                    ? ArloHandshakeRequest.DeserializeArray(data)?.FirstOrDefault()
                    : ArloHandshakeRequest.Deserialize(data);
            }

            logger.LogWarning("Slice data could not be parsed");
            return null;
        }
        catch (Exception e)
        {
            LogDeserializationError(logger, e);
        }

        return null;
    }

    private static bool TrySliceJsonFromFrame(ref ReadOnlySpan<byte> frame)
    {
        if (frame.Length <= 2 || frame[0] != (byte) 'L' || frame[1] != (byte) ':') return false;
        var spaceIndex = frame.IndexOf((byte) ' ');
        if (spaceIndex < 0) return false;
        frame = frame[(spaceIndex + 1)..];

        return true;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Parsing {Length} bytes from camera")]
    private static partial void LogProcessingData(ILogger logger, int length);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error during deserialization")]
    private static partial void LogDeserializationError(ILogger logger, Exception e);
}