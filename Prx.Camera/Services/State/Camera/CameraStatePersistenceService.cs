using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using Prx.Camera.Models.Classes;
using Prx.Camera.Models.Records;
using Prx.Camera.Models.Structs;

namespace Prx.Camera.Services.State.Camera;

public interface ICameraStatePersistenceService
{
    public Task StoreAsync(CameraState[] state);
    public Task<CameraState[]?> LoadAsync(CancellationToken ct = default);
}

public class CameraStatePersistenceService(IOptions<PrxCameraOptions> options) : ICameraStatePersistenceService
{
    private readonly string _filepath = options.Value.StateFilepath;

    private readonly VersionHeader _expectedHeader = new()
    {
        Magic = 'P' | ('R' << 8) | ('X' << 16) | ('C' << 24),
        Version = 1
    };

    public async Task<CameraState[]?> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_filepath)) return null;

        await using var fs = File.OpenRead(_filepath);
        var length = (int) fs.Length;

        var header = await ReadVersionHeaderAsync(fs, length, ct).ConfigureAwait(false);

        if (header is null) return null;

        if (header.Value.header.Magic != _expectedHeader.Magic)
            throw new InvalidDataException("Invalid state file format");

        return header.Value.header.Version switch
        {
            1 => (await LoadAsync<CameraStateV1>(fs, header.Value.headerLength, ct))?
                .Select(CameraState.From.V1).ToArray(),
            _ => throw new InvalidDataException($"Unsupported state version {header.Value.header.Version}")
        };
    }

    public Task StoreAsync(CameraState[] state)
    {
        if (state.Length > byte.MaxValue)
        {
            throw new OverflowException("State file is too large, more than 255 cameras are not supported");
        }

        return _expectedHeader.Version switch
        {
            1 => StoreAsync([.. state.Select(CameraState.To.V1)], _expectedHeader),
            _ => throw new InvalidDataException($"Unsupported state version {_expectedHeader.Version}")
        };
    } 

    private static async Task<(VersionHeader header, byte headerLength)?> ReadVersionHeaderAsync(FileStream fs,
        int length, CancellationToken ct = default)
    {
        var structSize = (byte) Unsafe.SizeOf<VersionHeader>();

        if (length < structSize)
            throw new InvalidDataException("Corrupted state file");

        var buffer = ArrayPool<byte>.Shared.Rent(structSize);

        try
        {
            var read = await fs.ReadAsync(buffer.AsMemory(0, structSize), ct);
            if (read != structSize)
                throw new IOException("Incomplete read");

            var header = MemoryMarshal.Read<VersionHeader>(buffer.AsSpan(0, read));
            return (header, structSize);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task StoreAsync<T>(T[] state, VersionHeader header) where T : unmanaged
    {
        var structSize = Unsafe.SizeOf<T>();
        var headerSize = Unsafe.SizeOf<VersionHeader>();
        var totalSize = headerSize + (state.Length * structSize);

        // Write to temp path and then move temp file to ensure atomic write
        var tempPath = $"{Path.GetFileNameWithoutExtension(_filepath)}.temp.bin";
        var buffer = ArrayPool<byte>.Shared.Rent(totalSize);

        try
        {
            // Perform all synchronous writes to the buffer first.
            MemoryMarshal.Write(buffer.AsSpan(0, headerSize), in header);
            var stateBytes = MemoryMarshal.AsBytes(state.AsSpan());
            stateBytes.CopyTo(buffer.AsSpan(headerSize));

            // Explicit using block to ensure filestream is disposed and resources are released before moving
            await using (var fs = File.Open(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await fs.WriteAsync(buffer.AsMemory(0, totalSize));
                await fs.FlushAsync();
            }

            File.Move(tempPath, _filepath, true);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }


    private static async Task<T[]?> LoadAsync<T>(FileStream fs, byte headerSize, CancellationToken ct = default)
        where T : unmanaged
    {
        var length = (int) fs.Length - headerSize;
        var structSize = Unsafe.SizeOf<T>();

        if (length % structSize != 0)
            throw new InvalidDataException("Corrupted state file");

        var buffer = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            var read = await fs.ReadAsync(buffer.AsMemory(0, length), ct);
            if (read != length)
                throw new IOException("Incomplete read");

            return MemoryMarshal.Cast<byte, T>(buffer.AsSpan(0, read)).ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
