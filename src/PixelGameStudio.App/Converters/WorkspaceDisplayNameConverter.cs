using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace PixelGameStudio.App.Converters;

/// <summary>
/// Maps workspace tab ids (used by VM logic: Inspector switching, timeline
/// visibility, validation auto-run) to their Vietnamese display labels.
/// </summary>
public class WorkspaceDisplayNameConverter : IValueConverter
{
    public static readonly WorkspaceDisplayNameConverter Instance = new();

    private static readonly Dictionary<string, string> Map = new(StringComparer.Ordinal)
    {
        ["Workspace"] = "Tổng quan",
        ["Project"] = "Dự án",
        ["Character"] = "Nhân vật",
        ["Rig"] = "Bộ khung",
        ["Equipment"] = "Trang bị",
        ["Animation"] = "Hoạt ảnh",
        ["Behavior"] = "Hành vi",
        ["Validation"] = "Kiểm tra",
        ["Export"] = "Xuất",
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string id && Map.TryGetValue(id, out string? name) ? name : value ?? "";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => BindingOperations.DoNothing;
}
