namespace Prx.Camera.Models.Interfaces;

public interface IAotJsonDeserializable<out T>
{
    static abstract T? Deserialize(ReadOnlySpan<byte> buffer);
    static abstract T[]? DeserializeArray(ReadOnlySpan<byte> buffer);
}

public interface IAotJsonSerializable<in T> where T : class
{
    void SerializeToStream(ref Stream stream);
    byte[] SerializeToByteArray();
}