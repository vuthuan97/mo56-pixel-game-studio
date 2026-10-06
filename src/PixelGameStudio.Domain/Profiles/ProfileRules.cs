using PixelGameStudio.Core.Primitives;
using PixelGameStudio.Domain.Animation;
using PixelGameStudio.Domain.Assets;
using PixelGameStudio.Domain.Behaviors;
using PixelGameStudio.Domain.Character;
using PixelGameStudio.Domain.Role;

namespace PixelGameStudio.Domain.Profiles;

/// <summary>Project-level validation rules. Returns human-readable issues; empty list = valid.</summary>
public static class ProfileRules
{
    public static IReadOnlyList<string> Validate(Project project)
    {
        var issues = new List<string>();
        if (project is null)
        {
            return ["Project chưa được tạo."];
        }

        ValidateIdentity(project, issues);
        ValidateView(project, issues);
        ValidatePixels(project, issues);
        ValidatePalette(project, issues);
        ValidateStyle(project, issues);
        ValidateAssets(project, issues);
        issues.AddRange(CharacterRules.Validate(project));
        issues.AddRange(AnimationRules.Validate(project));
        issues.AddRange(RoleRules.Validate(project));
        return issues;
    }

    private static void ValidateAssets(Project project, List<string> issues)
    {
        var idSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (AssetDefinition asset in project.Assets)
        {
            issues.AddRange(AssetRules.Validate(project, asset));
            if (!idSeen.Add(asset.Id))
            {
                issues.Add($"Asset id bị trùng: '{asset.Id}'");
            }
        }
    }

    private static void ValidateIdentity(Project project, List<string> issues)
    {
        if (string.IsNullOrWhiteSpace(project.Name))
        {
            issues.Add("Tên project không được để trống.");
        }
    }

    private static void ValidateView(Project project, List<string> issues)
    {
        ViewProfile view = project.View;
        if (string.IsNullOrWhiteSpace(view.Perspective))
        {
            issues.Add("Perspective không được để trống.");
        }

        if (view.Directions.Count == 0)
        {
            issues.Add("ViewProfile cần ít nhất 1 direction.");
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string direction in view.Directions)
        {
            if (string.IsNullOrWhiteSpace(direction))
            {
                issues.Add("Direction không được rỗng.");
            }
            else if (!seen.Add(direction))
            {
                issues.Add($"Direction bị trùng: {direction}");
            }
        }

        if (!string.IsNullOrWhiteSpace(view.DefaultDirection) && !view.Directions.Contains(view.DefaultDirection))
        {
            issues.Add($"DefaultDirection '{view.DefaultDirection}' không có trong danh sách direction.");
        }
    }

    private static void ValidatePixels(Project project, List<string> issues)
    {
        PixelProfile pixels = project.Pixels;
        if (pixels.CanvasWidth is < 8 or > 1024)
        {
            issues.Add($"CanvasWidth phải trong khoảng 8..1024 (hiện {pixels.CanvasWidth}).");
        }

        if (pixels.CanvasHeight is < 8 or > 1024)
        {
            issues.Add($"CanvasHeight phải trong khoảng 8..1024 (hiện {pixels.CanvasHeight}).");
        }

        if (pixels.PixelSize < 1)
        {
            issues.Add("PixelSize phải >= 1.");
        }
    }

    private static void ValidatePalette(Project project, List<string> issues)
    {
        PaletteProfile palette = project.Palette;
        if (palette.MaxColors < 2)
        {
            issues.Add("Palette MaxColors phải >= 2.");
        }

        if (palette.Colors.Count > palette.MaxColors)
        {
            issues.Add($"Palette đang có {palette.Colors.Count} màu, vượt giới hạn {palette.MaxColors}.");
        }

        var hexSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < palette.Colors.Count; i++)
        {
            PaletteEntry entry = palette.Colors[i];
            if (string.IsNullOrWhiteSpace(entry.Hex) || !Rgba32.TryParseHex(entry.Hex, out _))
            {
                issues.Add($"Màu #{i + 1} ('{entry.Name}') có hex không hợp lệ: '{entry.Hex}'.");
                continue;
            }

            if (!hexSeen.Add(entry.Hex.Trim().ToUpperInvariant()))
            {
                issues.Add($"Palette bị trùng màu {entry.Hex} ('{entry.Name}').");
            }

            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                issues.Add($"Màu {entry.Hex} thiếu tên.");
            }
        }
    }

    private static void ValidateStyle(Project project, List<string> issues)
    {
        ProjectStyleProfile style = project.Style;
        if (!style.NearestNeighborOnly)
        {
            issues.Add("NearestNeighborOnly phải bật: quy tắc bắt buộc của pixel art (TECH_STACK.md).");
        }

        if (style.OutlineThickness is < 0 or > 4)
        {
            issues.Add("OutlineThickness phải trong khoảng 0..4.");
        }

        if (string.IsNullOrWhiteSpace(style.OutlineColorHex) || !Rgba32.TryParseHex(style.OutlineColorHex, out _))
        {
            issues.Add($"OutlineColorHex không hợp lệ: '{style.OutlineColorHex}'.");
        }

        if (style.ShadowLevels is < 0 or > 3 || style.HighlightLevels is < 0 or > 3)
        {
            issues.Add("ShadowLevels/HighlightLevels phải trong khoảng 0..3.");
        }

        if (style.AlphaThreshold is < 0 or > 255)
        {
            issues.Add("AlphaThreshold phải trong khoảng 0..255.");
        }

        if (style.MinValueRange is < 0 or > 255)
        {
            issues.Add("MinValueRange phải trong khoảng 0..255.");
        }

        if (style.TileSizePx < 0)
        {
            issues.Add("TileSizePx không được âm.");
        }

        CharacterProportions c = style.Character;
        if (c.HeadHeightPx < 0 || c.TorsoHeightPx < 0 || c.LegHeightPx < 0)
        {
            issues.Add("Tỷ lệ nhân vật không được âm.");
        }
        else if (c.HeadHeightPx > 0 && c.HeadHeightPx + c.TorsoHeightPx + c.LegHeightPx > project.Pixels.CanvasHeight)
        {
            issues.Add("Tổng tỷ lệ nhân vật (đầu+thân+chân) vượt chiều cao canvas.");
        }
    }
}
