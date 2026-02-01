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

    public ControllerDevice Device { get; }

    public string DisplayName => Device.DisplayName;

    public string ProductName => Device.ProductName;

    public string VendorProductId => $"VID:{Device.VendorId:X4} PID:{Device.ProductId:X4}";

    public string Manufacturer => Device.Manufacturer;

    public ObservableCollection<ByteDisplayViewModel> Bytes { get; }

    public ControllerViewModel(ControllerDevice device)
    {
        Device = device;
        Bytes = [];

        for (int i = 0; i < ControllerData.ByteCount; i++)
        {
            Bytes.Add(new ByteDisplayViewModel(i));
        }
    }

    public void UpdateData(ControllerData data)
    {
        for (int i = 0; i < ControllerData.ByteCount && i < Bytes.Count; i++)
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
