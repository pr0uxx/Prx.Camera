using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using Prx.Camera.Models.Records;
using static System.Convert;

namespace Prx.Camera.Services;

public interface ITcpLoggerService
{
    void LogBuffer(Guid connectionId, ReadOnlySpan<byte> buffer);

    void LogError(Exception exception, Guid connectionId, string error);
}

public partial class TcpLoggerService(
    ILogger<TcpLoggerService> logger,
    IOptions<PrxCameraOptions> options
) : ITcpLoggerService
{
    public void LogBuffer(Guid connectionId, ReadOnlySpan<byte> buffer)
    {
        if (!options.Value.Debug) return;

        LogRxBufferLengthBytes(logger, connectionId, buffer.Length);
        LogRxBufferAsHex(logger, connectionId, ToHexString(buffer));
        LogRxBufferAsString(logger, connectionId, Encoding.UTF8.GetString(buffer));
        LogRxBufferAsAscii(logger, connectionId, ToBinarySafeAscii(buffer));
    }

    public void LogError(Exception exception, Guid connectionId, string error) => LogError(logger, exception, connectionId, error);

    private static string ToBinarySafeAscii(ReadOnlySpan<byte> buffer)
    {
        Span<char> chars = stackalloc char[buffer.Length];
        for (var i = 0; i < buffer.Length; i++)
        {
            var b = buffer[i];
            chars[i] = b is >= 32 and <= 126 ? (char) b : '.';
        }

        return new string(chars);
    }
    
    [LoggerMessage(LogLevel.Error, "RX {connectionId}: {error}")]
    static partial void LogError(ILogger<TcpLoggerService> logger, Exception exception, Guid connectionId, string error);

    [LoggerMessage(LogLevel.Debug, "RX {connectionId}: {bufferLength} bytes")]
    static partial void LogRxBufferLengthBytes(ILogger<TcpLoggerService> logger, Guid connectionId, int bufferLength);

    [LoggerMessage(LogLevel.Debug, "RX {connectionId} (HEX): {buffer}")]
    static partial void LogRxBufferAsHex(ILogger<TcpLoggerService> logger, Guid connectionId, string buffer);

    [LoggerMessage(LogLevel.Debug, "RX {connectionId} (UTF8): {buffer}")]
    static partial void LogRxBufferAsString(ILogger<TcpLoggerService> logger, Guid connectionId, string buffer);

    [LoggerMessage(LogLevel.Debug, "RX {connectionId} (ASCII): {buffer}")]
    static partial void LogRxBufferAsAscii(ILogger<TcpLoggerService> logger, Guid connectionId, string buffer);
}