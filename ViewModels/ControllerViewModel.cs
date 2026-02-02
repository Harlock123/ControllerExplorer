using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ControllerExplorer.Models;

namespace ControllerExplorer.ViewModels;

public partial class ControllerViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isConnected = true;

    [ObservableProperty]
    private string _lastUpdateTime = "--:--:--.---";

    private readonly int _byteCount;

    public ControllerDevice Device { get; }

    public string DisplayName => Device.DisplayName;

    public string ProductName => Device.ProductName;

    public string VendorProductId => Device.InputType == InputType.XInput
        ? $"XInput Index: {Device.XInputIndex}"
        : $"VID:{Device.VendorId:X4} PID:{Device.ProductId:X4}";

    public string Manufacturer => Device.Manufacturer;

    public ObservableCollection<ByteDisplayViewModel> Bytes { get; }

    public ControllerViewModel(ControllerDevice device)
    {
        Device = device;
        Bytes = [];

        _byteCount = device.MaxInputReportLength;

        for (int i = 0; i < _byteCount; i++)
        {
            Bytes.Add(new ByteDisplayViewModel(i));
        }
    }

    public void UpdateData(ControllerData data)
    {
        var updateCount = Math.Min(data.ByteCount, Bytes.Count);
        for (int i = 0; i < updateCount; i++)
        {
            Bytes[i].UpdateValue(data[i]);
        }

        LastUpdateTime = data.Timestamp.ToString("HH:mm:ss.fff");
    }

    public void ClearChangeFlags()
    {
        foreach (var byteVm in Bytes)
        {
            byteVm.ClearChangeFlag();
        }
    }
}
