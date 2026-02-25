using System.Globalization;
using Avalonia.Data.Converters;

namespace Meowdex.Desktop.Converters;

public sealed class BoolNotConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool flag ? !flag : null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool flag ? !flag : null;
    }
}
