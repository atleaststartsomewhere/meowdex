using System.Globalization;
using Avalonia.Data.Converters;
using Meowdex.Core.Services;

namespace Meowdex.Desktop.Converters;

public sealed class MaskConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int mask)
        {
            return BreedingAdvisorService.FormatMask(mask);
        }

        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }
}
