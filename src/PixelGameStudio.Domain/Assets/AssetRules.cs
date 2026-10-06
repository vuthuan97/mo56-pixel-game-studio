namespace PixelGameStudio.Domain.Assets;

/// <summary>Structural validation rules for asset definitions (no filesystem access — pure data checks).</summary>
public static class AssetRules
{
    /// <summary>Valid ids: letters/digits start, then letters/digits/'-'/'_'/'.'; no "..", up to 64 chars.</summary>
    public static bool IsValidId(string? id)
    {
        if (string.IsNullOrEmpty(id) || id.Length > 64 || !char.IsAsciiLetterOrDigit(id[0]))
        {
            return false;
        }

        if (id.Contains(".."))
        {
            return false;
        }

        return id.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');
    }

    public static IReadOnlyList<string> Validate(Project project, AssetDefinition asset)
    {
        var issues = new List<string>();
        if (asset is null)
        {
            return ["Asset chưa được tạo."];
        }

        if (!IsValidId(asset.Id))
        {
            issues.Add($"Asset id không hợp lệ: '{asset.Id}' (chữ/số/'-'/'_', bắt đầu bằng chữ hoặc số, tối đa 64 ký tự).");
        }

        if (string.IsNullOrWhiteSpace(asset.DisplayName))
        {
            issues.Add($"Asset '{asset.Id}' thiếu DisplayName.");
        }

        ValidateFile(asset, issues);
        ValidateCanvasAndAnchor(project, asset, issues);
        ValidateTagsAndViews(project, asset, issues);
        return issues;
    }

    private static void ValidateFile(AssetDefinition asset, List<string> issues)
    {
        string file = asset.File;
        if (string.IsNullOrWhiteSpace(file))
        {
            issues.Add($"Asset '{asset.Id}' thiếu đường dẫn file.");
            return;
        }

        if (file.Contains('\\') || file.StartsWith('/') || file.Contains("//") || file.Split('/').Contains(".."))
        {
            issues.Add($"Asset '{asset.Id}' có đường dẫn không an toàn: '{file}' (chỉ dùng '/', không '..', không absolute).");
        }

        if (!file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add($"Asset '{asset.Id}' phải trỏ tới file .png (hiện '{file}').");
        }
    }

    private static void ValidateCanvasAndAnchor(Project project, AssetDefinition asset, List<string> issues)
    {
        if (asset.CanvasWidth < 1 || asset.CanvasHeight < 1)
        {
            issues.Add($"Asset '{asset.Id}' có canvas không hợp lệ: {asset.CanvasWidth}x{asset.CanvasHeight}.");
        }

        if (asset.ZIndex < 0)
        {
            issues.Add($"Asset '{asset.Id}' có ZIndex âm.");
        }

        if (asset.Anchor is { } anchor)
        {
            if (anchor.X < 0 || anchor.X >= asset.CanvasWidth || anchor.Y < 0 || anchor.Y >= asset.CanvasHeight)
            {
                issues.Add($"Asset '{asset.Id}' có anchor ({anchor.X},{anchor.Y}) ngoài canvas {asset.CanvasWidth}x{asset.CanvasHeight}.");
            }
        }

        if (asset.CanvasWidth > 0 && asset.CanvasHeight > 0 &&
            (asset.CanvasWidth > project.Pixels.CanvasWidth || asset.CanvasHeight > project.Pixels.CanvasHeight))
        {
            issues.Add(
                $"Asset '{asset.Id}' ({asset.CanvasWidth}x{asset.CanvasHeight}) lớn hơn canvas của project " +
                $"({project.Pixels.CanvasWidth}x{project.Pixels.CanvasHeight}).");
        }
    }

    private static void ValidateTagsAndViews(Project project, AssetDefinition asset, List<string> issues)
    {
        var tagSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string tag in asset.Tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                issues.Add($"Asset '{asset.Id}' có tag rỗng.");
            }
            else if (!tagSeen.Add(tag))
            {
                issues.Add($"Asset '{asset.Id}' bị trùng tag '{tag}'.");
            }
        }

        var viewSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string view in asset.Views)
        {
            if (string.IsNullOrWhiteSpace(view))
            {
                issues.Add($"Asset '{asset.Id}' có view rỗng.");
            }
            else if (!viewSeen.Add(view))
            {
                issues.Add($"Asset '{asset.Id}' bị trùng view '{view}'.");
            }
            else if (!project.View.Directions.Contains(view, StringComparer.OrdinalIgnoreCase))
            {
                issues.Add($"Asset '{asset.Id}' khai báo view '{view}' không có trong ViewProfile của project.");
            }
        }
    }
}
