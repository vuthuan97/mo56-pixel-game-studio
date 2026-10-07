using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace PixelGameStudio.App.Converters;

public sealed class CharacterSectionDisplayNameConverter : IValueConverter
{
    public static readonly CharacterSectionDisplayNameConverter Instance = new();

    private static readonly Dictionary<string, string> Map = new(StringComparer.Ordinal)
    {
        ["Frame"] = "Bộ khung",
        ["Equipment"] = "Trang bị",
        ["Animation"] = "Hoạt ảnh",
        ["Actions"] = "Hành động",
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string id && Map.TryGetValue(id, out string? name) ? name : value ?? "";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => BindingOperations.DoNothing;
}
