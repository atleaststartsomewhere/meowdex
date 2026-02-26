using System.Globalization;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Data.Converters;
using Meowdex.Core.Models;

namespace Meowdex.Desktop.Converters;

public sealed class GenderIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            CatGender.Male => IconBitmapCache.Get("avares://Meowdex.Desktop/Assets/Icons/GenderSexuality/GenderMale.png"),
            CatGender.Female => IconBitmapCache.Get("avares://Meowdex.Desktop/Assets/Icons/GenderSexuality/GenderFemale.png"),
            CatGender.Fluid => IconBitmapCache.Get("avares://Meowdex.Desktop/Assets/Icons/GenderSexuality/GenderFluid.png"),
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
            CatSexuality.Bi => IconBitmapCache.Get("avares://Meowdex.Desktop/Assets/Icons/GenderSexuality/SexualityBisexual.png"),
            CatSexuality.GayLesbian => IconBitmapCache.Get("avares://Meowdex.Desktop/Assets/Icons/GenderSexuality/SexualityGay.png"),
            CatSexuality.Straight => null,
            _ => null
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value;
}

internal static class IconBitmapCache
{
    private static readonly Dictionary<string, Bitmap> Cache = new();

    public static Bitmap? Get(string assetUri)
    {
        if (Cache.TryGetValue(assetUri, out var cached))
        {
            return cached;
        }

        try
        {
            using var stream = AssetLoader.Open(new Uri(assetUri));
            var bitmap = new Bitmap(stream);
            Cache[assetUri] = bitmap;
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
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
