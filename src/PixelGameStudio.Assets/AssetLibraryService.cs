using System.Security.Cryptography;
using PixelGameStudio.Core.Primitives;
using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Assets;
using PixelGameStudio.Rendering;

namespace PixelGameStudio.Assets;

/// <summary>
/// Asset library of a project: import/validate/store PNG assets under
/// &lt;projectRoot&gt;/assets, search and filter definitions, load pixels.
/// Fully UI-independent. The project.pgsproj carries the AssetDefinition list;
/// PNG binaries live on disk — JSON never embeds image data.
/// </summary>
public sealed class AssetLibraryService
{
    /// <summary>Folder name inside the project root; matches ProjectPaths in ProjectSystem.</summary>
    public const string AssetsFolderName = "assets";

    /// <summary>Imports one PNG into the project library and registers its definition.</summary>
    public AssetImportResult Import(Project project, string projectRoot, string sourcePngPath, AssetImportOptions options)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentNullException.ThrowIfNull(options);

        if (!File.Exists(sourcePngPath))
        {
            throw new AssetImportException($"Không tìm thấy file nguồn: '{sourcePngPath}'.");
        }

        PixelBuffer buffer = DecodeSource(sourcePngPath);

        string id = Slugify(options.AssetId) ?? Slugify(Path.GetFileNameWithoutExtension(sourcePngPath))
            ?? throw new AssetImportException("Không tạo được asset id từ tên file.");
        EnsureUniqueId(project, id, options.OverwriteExisting);

        var warnings = new List<string>();
        bool resized = false;
        int sourceWidth = buffer.Width;
        int sourceHeight = buffer.Height;
        if (buffer.Width != project.Pixels.CanvasWidth || buffer.Height != project.Pixels.CanvasHeight)
        {
            if (!options.AllowResize)
            {
                throw new AssetImportException(
                    $"Canvas của PNG ({buffer.Width}x{buffer.Height}) khác canvas project " +
                    $"({project.Pixels.CanvasWidth}x{project.Pixels.CanvasHeight}). " +
                    "Bật AllowResize nếu muốn resize NEAREST.");
            }

            buffer = PixelOps.NearestResize(buffer, project.Pixels.CanvasWidth, project.Pixels.CanvasHeight);
            resized = true;
            warnings.Add($"Đã resize NEAREST {sourceWidth}x{sourceHeight} → {buffer.Width}x{buffer.Height}.");
        }

        string fileName = $"{id}.png";
        string assetsDir = AssetsDirectory(projectRoot);
        Directory.CreateDirectory(assetsDir);
        string targetPath = Path.Combine(assetsDir, fileName);
        using (Stream output = File.Create(targetPath))
        {
            PngCodec.Encode(buffer, output);
        }

        if (options.OverwriteExisting)
        {
            AssetDefinition? old = project.Assets.FirstOrDefault(a => a.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (old is not null)
            {
                project.Assets.Remove(old);
                DeleteThumbnails(projectRoot, old.Id);
            }
        }

        var definition = new AssetDefinition
        {
            Id = id,
            DisplayName = string.IsNullOrWhiteSpace(options.DisplayName) ? id : options.DisplayName.Trim(),
            Type = string.IsNullOrWhiteSpace(options.Type) ? AssetTypes.Custom : options.Type,
            File = fileName,
            CanvasWidth = buffer.Width,
            CanvasHeight = buffer.Height,
            Tags = options.Tags.Select(t => t.Trim()).Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            Views = options.Views.Select(v => v.Trim()).Where(v => v.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            Anchor = options.Anchor,
            ZIndex = Math.Max(0, options.ZIndex),
            ImportedAtUtc = DateTime.UtcNow,
        };

        project.Assets.RemoveAll(a => a.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        project.Assets.Add(definition);
        return new AssetImportResult { Definition = definition, Warnings = warnings, Resized = resized };
    }

    /// <summary>Removes the definition and its stored PNG/thumbnails.</summary>
    public void Delete(Project project, string projectRoot, string assetId)
    {
        ArgumentNullException.ThrowIfNull(project);
        AssetDefinition? definition = project.Assets.FirstOrDefault(a => a.Id.Equals(assetId, StringComparison.OrdinalIgnoreCase));
        if (definition is null)
        {
            return;
        }

        project.Assets.Remove(definition);
        string file = Path.Combine(AssetsDirectory(projectRoot), definition.File);
        if (File.Exists(file))
        {
            File.Delete(file);
        }

        DeleteThumbnails(projectRoot, definition.Id);
    }

    /// <summary>Full path of the stored PNG for an asset.</summary>
    public string ResolvePath(string projectRoot, AssetDefinition definition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        string combined = Path.Combine(AssetsDirectory(projectRoot), definition.File.Replace('/', Path.DirectorySeparatorChar));
        string fullRoot = Path.GetFullPath(AssetsDirectory(projectRoot));
        string full = Path.GetFullPath(combined);
        if (!full.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new AssetImportException($"Đường dẫn asset '{definition.File}' thoát khỏi thư mục assets.");
        }

        return full;
    }

    /// <summary>Decodes the stored PNG of an asset (UI-independent; used by renderer/exporters).</summary>
    public PixelBuffer LoadPixels(string projectRoot, AssetDefinition definition)
    {
        string path = ResolvePath(projectRoot, definition);
        if (!File.Exists(path))
        {
            throw new AssetImportException($"File asset không tồn tại: '{path}'.");
        }

        return PngCodec.Decode(File.OpenRead(path));
    }

    /// <summary>Removes thumbnails for a regenerated asset so the next browser refresh reads the new PNG.</summary>
    public void InvalidateThumbnails(string projectRoot, string assetId) => DeleteThumbnails(projectRoot, assetId);

    /// <summary>Search by substring (id/displayName), required tags (AND) and exact type. Case-insensitive.</summary>
    public IReadOnlyList<AssetDefinition> Search(Project project, string? query = null, IEnumerable<string>? tags = null, string? type = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        IEnumerable<AssetDefinition> assets = project.Assets;

        if (!string.IsNullOrWhiteSpace(query))
        {
            string q = query.Trim();
            assets = assets.Where(a =>
                a.Id.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                a.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        if (tags is not null)
        {
            List<string> tagList = tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToList();
            foreach (string tag in tagList)
            {
                assets = assets.Where(a => a.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase));
            }
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            assets = assets.Where(a => a.Type.Equals(type.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        return assets.OrderBy(a => a.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Checks every definition against the rules and disk state (file exists, canvas matches stored PNG).</summary>
    public IReadOnlyList<string> ValidateOnDisk(Project project, string projectRoot)
    {
        var issues = new List<string>();
        foreach (AssetDefinition definition in project.Assets)
        {
            issues.AddRange(Domain.Assets.AssetRules.Validate(project, definition));
            string path = Path.Combine(AssetsDirectory(projectRoot), definition.File.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                issues.Add($"Asset '{definition.Id}' mất file trên đĩa: '{definition.File}'.");
                continue;
            }

            PixelBuffer buffer = PngCodec.Decode(File.OpenRead(path));
            if (buffer.Width != definition.CanvasWidth || buffer.Height != definition.CanvasHeight)
            {
                issues.Add($"Asset '{definition.Id}' khai báo canvas {definition.CanvasWidth}x{definition.CanvasHeight} " +
                           $"nhưng file là {buffer.Width}x{buffer.Height}.");
            }
        }

        return issues;
    }

    public static string AssetsDirectory(string projectRoot) => Path.Combine(projectRoot, AssetsFolderName);

    private static PixelBuffer DecodeSource(string path)
    {
        try
        {
            return PngCodec.Decode(File.OpenRead(path));
        }
        catch (Exception ex) when (ex is InvalidDataException or NotSupportedException or IOException)
        {
            throw new AssetImportException($"File không phải PNG hợp lệ: '{path}' ({ex.Message}).");
        }
    }

    private static void EnsureUniqueId(Project project, string id, bool overwrite)
    {
        bool exists = project.Assets.Any(a => a.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (exists && !overwrite)
        {
            throw new AssetImportException($"Đã có asset id '{id}'. Đặt AssetId khác hoặc bật OverwriteExisting.");
        }
    }

    private static void DeleteThumbnails(string projectRoot, string assetId)
    {
        string thumbs = Path.Combine(AssetsDirectory(projectRoot), "thumbs");
        if (!Directory.Exists(thumbs))
        {
            return;
        }

        foreach (string file in Directory.GetFiles(thumbs, $"{assetId}_*.png"))
        {
            File.Delete(file);
        }
    }

    internal static string? Slugify(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var chars = input.Trim().Select(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' ? c : '-').ToArray();
        string slug = new string(chars).Trim('-');
        while (slug.Contains("--"))
        {
            slug = slug.Replace("--", "-");
        }

        return slug.Length == 0 ? null : (slug.Length > 64 ? slug[..64].Trim('-') : slug);
    }
}
