using System.Globalization;
using Avalonia.Data.Converters;

namespace Meowdex.Desktop.Converters;

public sealed class EqualityMultiConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2)
        {
            return false;
        }

        return Equals(values[0], values[1]);
    }
}
