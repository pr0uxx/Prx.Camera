using Microsoft.Extensions.Options;
using NSubstitute;
using Prx.Camera.Models.Classes;
using Prx.Camera.Models.Records;
using Prx.Camera.Models.Structs;
using Prx.Camera.Services.State.Camera;

namespace Prx.Camera.Tests;

public class CameraStatePersistenceServiceTests
{
    private const string TestStateFile = "test_state.bin";

    private readonly IOptions<PrxCameraOptions> _options;

    public CameraStatePersistenceServiceTests()
    {
        _options = Substitute.For<IOptions<PrxCameraOptions>>();
        _options.Value.Returns(new PrxCameraOptions {StateFilepath = TestStateFile});
    }

    [Fact]
    public async Task StoreAndLoad_ShouldReturnSameState()
    {
        // Arrange
        var service = new CameraStatePersistenceService(_options);
        var expectedState = new CameraStateV1[]
        {
            new()
            {
                SerialHash = ulong.MaxValue, BatteryLevel = 50, Temperature = 30, LastMotionUnix = 1234567890,
                FirmwareHash = 1200, CapabilitiesHash = 12345
            },
            new()
            {
                SerialHash = ulong.MaxValue - 50, BatteryLevel = 60, Temperature = 32, LastMotionUnix = 1243567890,
                FirmwareHash = 120011, CapabilitiesHash = 12546
            }
        };
        File.Delete(TestStateFile);

        // Act
        await service.StoreAsync([.. expectedState.Select(CameraState.From.V1)]);
        var actualState = await service.LoadAsync();

        // Assert
        Assert.NotNull(actualState);
        Assert.Equal(expectedState.Length, actualState.Length);
        for (int i = 0; i < expectedState.Length; i++)
        {
            Assert.Equal(expectedState[i], CameraState.To.V1(actualState[i]));
        }
    }

    [Fact]
    public async Task Load_FromNonExistentFile_ShouldReturnNull()
    {
        // Arrange
        var service = new CameraStatePersistenceService(_options);
        File.Delete(TestStateFile);

        // Act
        var result = await service.LoadAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Load_WithInvalidMagic_ShouldThrow()
    {
        // Arrange
        var service = new CameraStatePersistenceService(_options);
        await File.WriteAllBytesAsync(TestStateFile, [1, 2, 3, 4]);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidDataException>(() => service.LoadAsync());
    }

    [Fact]
    public async Task Load_WithUnsupportedVersion_ShouldThrow()
    {
        // Arrange
        var service = new CameraStatePersistenceService(_options);
        // PRCX magic number, but version 2
        await File.WriteAllBytesAsync(TestStateFile, [0x50, 0x52, 0x43, 0x58, 2, 0]);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidDataException>(() => service.LoadAsync());
    }
}