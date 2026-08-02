namespace Prx.Camera.Models.Records;

public sealed record PrxCameraOptions
{
    public bool Debug { get; set; }
    public string StateFilepath { get; set; } = "/var/lib/prx-camera/state.bin";
};