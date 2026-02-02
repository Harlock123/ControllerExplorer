namespace ControllerExplorer.Models;

public class ControllerData
{
    public byte[] RawData { get; }
    public DateTime Timestamp { get; }
    public int ByteCount => RawData.Length;

    public ControllerData(byte[] data)
    {
        RawData = new byte[data.Length];
        Array.Copy(data, RawData, data.Length);
        Timestamp = DateTime.Now;
    }

    public ControllerData(byte[] data, int length)
    {
        RawData = new byte[length];
        var copyLength = Math.Min(data.Length, length);
        Array.Copy(data, RawData, copyLength);
        Timestamp = DateTime.Now;
    }

    public byte this[int index] => index >= 0 && index < RawData.Length ? RawData[index] : (byte)0;

    public static ControllerData Empty => new(Array.Empty<byte>());
}
