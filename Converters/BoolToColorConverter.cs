using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ControllerExplorer.Converters;

public class BoolToColorConverter : IValueConverter
{
    public IBrush? TrueBrush { get; set; } = new SolidColorBrush(Color.Parse("#00ff00"));
    public IBrush? FalseBrush { get; set; } = new SolidColorBrush(Color.Parse("#666666"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? TrueBrush : FalseBrush;
        }
        return FalseBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
