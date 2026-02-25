using System.Globalization;
using Avalonia.Data.Converters;
using Meowdex.Core.Models;

namespace Meowdex.Desktop.Converters;

public sealed class GenderIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            CatGender.Male => "avares://Meowdex.Desktop/Assets/Icons/GenderSexuality/GenderMale.png",
            CatGender.Female => "avares://Meowdex.Desktop/Assets/Icons/GenderSexuality/GenderFemale.png",
            CatGender.Fluid => "avares://Meowdex.Desktop/Assets/Icons/GenderSexuality/GenderFluid.png",
            _ => null
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value;
}

public sealed class SexualityIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            CatSexuality.Bi => "avares://Meowdex.Desktop/Assets/Icons/GenderSexuality/SexualityBisexual.png",
            CatSexuality.GayLesbian => "avares://Meowdex.Desktop/Assets/Icons/GenderSexuality/SexualityGay.png",
            CatSexuality.Straight => "avares://Meowdex.Desktop/Assets/Icons/GenderSexuality/SexualityBisexual.png",
            _ => null
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value;
}

public sealed class GenderTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            CatGender.Male => "Male",
            CatGender.Female => "Female",
            CatGender.Fluid => "Fluid",
            _ => value?.ToString() ?? string.Empty
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value;
}

public sealed class SexualityTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            CatSexuality.Bi => "Bisexual",
            CatSexuality.GayLesbian => "Gay / Lesbian",
            CatSexuality.Straight => "Straight",
            _ => value?.ToString() ?? string.Empty
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value;
}
