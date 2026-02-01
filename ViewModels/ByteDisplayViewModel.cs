using CommunityToolkit.Mvvm.ComponentModel;

namespace ControllerExplorer.ViewModels;

public partial class ByteDisplayViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _byteIndex;

    [ObservableProperty]
    private byte _value;

    [ObservableProperty]
    private bool _hasChanged;

    public string HexValue => $"0x{Value:X2}";

    public string BinaryValue => Convert.ToString(Value, 2).PadLeft(8, '0');

    public string BinaryUpperNibble => BinaryValue[..4];

    public string BinaryLowerNibble => BinaryValue[4..];

    public bool[] Bits { get; } = new bool[8];

    public ByteDisplayViewModel(int index)
    {
        ByteIndex = index;
        UpdateBits();
    }

    public void UpdateValue(byte newValue)
    {
        if (Value != newValue)
        {
            Value = newValue;
            HasChanged = true;
            UpdateBits();
            OnPropertyChanged(nameof(HexValue));
            OnPropertyChanged(nameof(BinaryValue));
            OnPropertyChanged(nameof(BinaryUpperNibble));
            OnPropertyChanged(nameof(BinaryLowerNibble));
            OnPropertyChanged(nameof(Bits));
        }
    }

    public void ClearChangeFlag()
    {
        HasChanged = false;
    }

    private void UpdateBits()
    {
        for (int i = 0; i < 8; i++)
        {
            Bits[7 - i] = (Value & (1 << i)) != 0;
        }
    }
}
