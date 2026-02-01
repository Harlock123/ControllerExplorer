using HidSharp;

namespace ControllerExplorer.Models;

public class ControllerDevice
{
    public string DevicePath { get; }
    public int VendorId { get; }
    public int ProductId { get; }
    public string ProductName { get; }
    public string Manufacturer { get; }
    public int MaxInputReportLength { get; }

    internal HidDevice HidDevice { get; }

    public ControllerDevice(HidDevice device)
    {
        HidDevice = device;
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

    public string DisplayName => $"{ProductName} ({VendorId:X4}:{ProductId:X4})";

    public override string ToString() => DisplayName;
}
