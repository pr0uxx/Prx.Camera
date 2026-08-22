using System.Buffers;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Prx.Camera.Models.Classes;

namespace Prx.Camera.Services;

public interface ITcpListenerService
{
    Task StartAsync(CancellationToken ct = default);
}

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class TcpListenerService(
    IArloProtocolParser parser,
    IArloEventHandler handler,
    ITcpLoggerService tcpLogger
    ) : ITcpListenerService
{
    private static readonly X509Certificate2 ServerCertificate = GenerateSelfSignedCert();

    private static X509Certificate2 GenerateSelfSignedCert()
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=registration.arloxcld.com", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var cert = req.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(5));
        
        // Exporting and re-importing is required in .NET on Linux to ensure the private key is properly attached for SslStream
        return X509CertificateLoader.LoadPkcs12(cert.Export(X509ContentType.Pfx), null);
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        TcpListener? listener = null;
        
        try
        {
            listener = new TcpListener(IPAddress.Any, 4000);
            listener.Start();

            while (!ct.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(ct);
                _ = ProcessClientAsync(client, ct);
            }
        }
        finally
        {
            listener?.Stop();
            listener?.Dispose();
        }
    }

    private async Task ProcessClientAsync(TcpClient client, CancellationToken ct = default)
    {
        var connectionId = Guid.NewGuid();
        using var tcpClientScope = client; // Ensure TcpClient is disposed when done
        Stream stream = client.GetStream();
        
        try
        {
            // Peek the first byte to determine if this is a TLS connection (0x16 is the TLS Client Hello byte)
            var peekBuffer = new byte[1];
            if (client.Client.Receive(peekBuffer, SocketFlags.Peek) > 0 && peekBuffer[0] == 0x16)
            {
                var sslStream = new SslStream(stream, false);
                await sslStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                {
                    ServerCertificate = ServerCertificate,
                    ClientCertificateRequired = false,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck
                }, ct);
                stream = sslStream; // Use the decrypted stream from now on
            }
        }
        catch (Exception ex)
        {
            tcpLogger.LogError(ex, connectionId, "TLS Handshake failed");
            return; // Drop the connection if TLS handshake fails
        }

        await using var streamScope = stream; // Ensures the active stream is disposed gracefully

        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        CameraSession? session = null;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var read = await stream.ReadAsync(buffer, ct);
                if (read <= 0) break; // Client disconnected gracefully

                tcpLogger.LogBuffer(connectionId, buffer.AsSpan(0, read));

                var message = parser.Parse(buffer.AsSpan(0, read));
                if (message is not null)
                {
                    // Update the serialHash if we get a new one, or keep the existing one 
                    var cameraSession = await handler.HandleAsync(message.Value, client, session, ct);
                    if (cameraSession is not null)
                    {
                        session = cameraSession;
                    }
                }
            }
        }
        catch (Exception e)
        {
            tcpLogger.LogError(e, connectionId, "Unhandled exception during data processing");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}