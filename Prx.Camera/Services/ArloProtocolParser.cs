using Microsoft.Extensions.Logging;
using Prx.Camera.Records;

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
            return ArloHandshakeRequest.DeserializeArray(data)?.FirstOrDefault();
        }
        catch (Exception e)
        {
            LogDeserializationError(logger, e);
        }

        return null;
    }
    
    [LoggerMessage(Level = LogLevel.Debug, Message = "Parsing {Length} bytes from camera")]
    private static partial void LogProcessingData(ILogger logger, int length);
    
    [LoggerMessage(Level = LogLevel.Error, Message = "Error during deserialization")]
    private static partial void LogDeserializationError(ILogger logger, Exception e);
}