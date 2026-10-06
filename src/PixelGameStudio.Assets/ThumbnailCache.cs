using System.Security.Cryptography;
using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Assets;
using PixelGameStudio.Rendering;

namespace PixelGameStudio.Assets;

/// <summary>
/// Disk cache of browser thumbnails under &lt;projectRoot&gt;/assets/thumbs.
/// Cache key embeds a short content hash of the source PNG, so edited assets
/// invalidate automatically. Scaling is nearest-neighbor only (pixel-art rule):
/// prefer integer up-scale, fall back to fractional nearest down-scale when the
/// native canvas exceeds the box.
/// </summary>
public sealed class ThumbnailCache
{
    private readonly AssetLibraryService _library;

    public ThumbnailCache(AssetLibraryService library) => _library = library;

    /// <summary>Returns the cached thumbnail path for the asset at the given box size, creating it if needed.</summary>
    public string GetOrCreate(Project project, string projectRoot, AssetDefinition definition, int boxSize = 96)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentNullException.ThrowIfNull(definition);
        if (boxSize < 8)
        {
            throw new ArgumentOutOfRangeException(nameof(boxSize));
        }

        string sourcePath = _library.ResolvePath(projectRoot, definition);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Asset file missing: '{sourcePath}'.");
        }

        string hash = ShortHash(sourcePath);
        string thumbsDir = Path.Combine(AssetLibraryService.AssetsDirectory(projectRoot), "thumbs");
        Directory.CreateDirectory(thumbsDir);
        string cachePath = Path.Combine(thumbsDir, $"{definition.Id}_{boxSize}_{hash}.png");
        if (File.Exists(cachePath))
        {
            return cachePath;
        }

        PixelBuffer source = _library.LoadPixels(projectRoot, definition);
        PixelBuffer thumb = FitToBox(source, boxSize);
        using (Stream output = File.Create(cachePath))
        {
            PngCodec.Encode(thumb, output);
        }

        PruneStale(thumbsDir, definition.Id, Path.GetFileName(cachePath));
        return cachePath;
    }

    /// <summary>Nearest-neighbor fit into a square box (integer up-scale preferred).</summary>
    internal static PixelBuffer FitToBox(PixelBuffer source, int boxSize)
    {
        int width = source.Width;
        int height = source.Height;
        int scale = Math.Max(1, Math.Min(boxSize / width, boxSize / height));
        int targetW = width * scale;
        int targetH = height * scale;
        if (targetW > boxSize || targetH > boxSize)
        {
            double factor = Math.Min((double)boxSize / width, (double)boxSize / height);
            targetW = Math.Max(1, (int)Math.Round(width * factor));
            targetH = Math.Max(1, (int)Math.Round(height * factor));
        }

        return targetW == width && targetH == height
            ? source.Clone()
            : PixelOps.NearestResize(source, targetW, targetH);
    }

    private static string ShortHash(string filePath)
    {
        byte[] hash = SHA256.HashData(File.ReadAllBytes(filePath));
        return Convert.ToHexString(hash, 0, 4).ToLowerInvariant();
    }

    private static void PruneStale(string thumbsDir, string assetId, string keepFile)
    {
        foreach (string file in Directory.GetFiles(thumbsDir, $"{assetId}_*.png"))
        {
            if (!Path.GetFileName(file).Equals(keepFile, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(file);
            }
        }
    }
}
