using System.Globalization;
using Avalonia.Data.Converters;

namespace Meowdex.Desktop.Converters;

public sealed class EqualsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || parameter is null)
        {
            return false;
        }

        if (value.GetType().IsEnum && parameter is string text)
        {
            return string.Equals(value.ToString(), text, StringComparison.OrdinalIgnoreCase);
        }

        return Equals(value, parameter);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool flag && flag ? parameter : null;
    }
}
