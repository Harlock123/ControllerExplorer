using HidSharp;

namespace ControllerExplorer.Models;

public enum InputType
{
    Hid,
    XInput
}

public class ControllerDevice
{
    public string DevicePath { get; }
    public int VendorId { get; }
    public int ProductId { get; }
    public string ProductName { get; }
    public string Manufacturer { get; }
    public int MaxInputReportLength { get; }
    public InputType InputType { get; }
    public int XInputIndex { get; }

    internal HidDevice? HidDevice { get; }

    public ControllerDevice(HidDevice device)
    {
        HidDevice = device;
        InputType = InputType.Hid;
        XInputIndex = -1;
        DevicePath = device.DevicePath;
        VendorId = device.VendorID;
        ProductId = device.ProductID;
        MaxInputReportLength = device.GetMaxInputReportLength();

        try
        {
            ProductName = device.GetProductName() ?? "Unknown Device";
        }
        catch
        {
            ProductName = "Unknown Device";
        }

        try
        {
            Manufacturer = device.GetManufacturer() ?? "Unknown";
        }
        catch
        {
            Manufacturer = "Unknown";
        }
    }

    public ControllerDevice(int xInputIndex, string productName)
    {
        HidDevice = null;
        InputType = InputType.XInput;
        XInputIndex = xInputIndex;
        DevicePath = $"XInput:{xInputIndex}";
        VendorId = 0x045E; // Microsoft
        ProductId = 0x0000;
        ProductName = productName;
        Manufacturer = "Microsoft (XInput)";
        MaxInputReportLength = 12; // XInput state size
    }

    public string DisplayName => InputType == InputType.XInput
        ? $"{ProductName} (XInput:{XInputIndex})"
        : $"{ProductName} ({VendorId:X4}:{ProductId:X4})";

    public override string ToString() => DisplayName;
}
