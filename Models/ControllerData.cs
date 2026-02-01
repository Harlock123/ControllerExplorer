namespace ControllerExplorer.Models;

public class ControllerData
{
    public const int ByteCount = 9;

    public byte[] RawData { get; }
    public DateTime Timestamp { get; }

    public ControllerData(byte[] data)
    {
        RawData = new byte[ByteCount];
        var copyLength = Math.Min(data.Length, ByteCount);
        Array.Copy(data, RawData, copyLength);
        Timestamp = DateTime.Now;
    }

    public byte this[int index] => index >= 0 && index < RawData.Length ? RawData[index] : (byte)0;

    public static ControllerData Empty => new(Array.Empty<byte>());
}
