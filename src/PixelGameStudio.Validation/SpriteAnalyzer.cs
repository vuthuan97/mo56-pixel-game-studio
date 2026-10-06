using PixelGameStudio.Rendering;

namespace PixelGameStudio.Validation;

/// <summary>Result of analyzing one sprite frame for art QA.</summary>
public sealed record SpriteAnalysis(
    int PaletteCount,
    int VisibleWidth,
    int VisibleHeight,
    int ValueRange,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Pixel-level art QA ported from the legacy prototype analyze_sprite():
/// distinct opaque color count, visible (alpha) bounds, grayscale value range
/// and readability warnings. Thresholds are the legacy values; they move into
/// project StyleProfile rules in Phase 10.
/// </summary>
public static class SpriteAnalyzer
{
    private const int MaxRecommendedColors = 24;
    private const int MinValueRange = 90;
    private const int MaxVisibleWidth = 30;
    private const int MaxVisibleHeight = 44;

    public static SpriteAnalysis Analyze(PixelBuffer buffer)
    {
        int paletteCount = buffer.CountDistinctOpaqueColors();

        int minX = buffer.Width, minY = buffer.Height, maxX = -1, maxY = -1;
        byte minL = 255, maxL = 0;
        bool hasOpaque = false;

        for (int y = 0; y < buffer.Height; y++)
        {
            for (int x = 0; x < buffer.Width; x++)
            {
                var p = buffer[x, y];
                if (p.A <= 0)
                {
                    continue;
                }

                hasOpaque = true;
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);

                int l = ((p.R * 19595) + (p.G * 38470) + (p.B * 7471) + 0x8000) >> 16;
                minL = Math.Min(minL, (byte)l);
                maxL = Math.Max(maxL, (byte)l);
            }
        }

        int visibleWidth = hasOpaque ? maxX - minX + 1 : 0;
        int visibleHeight = hasOpaque ? maxY - minY + 1 : 0;
        int valueRange = hasOpaque ? maxL - minL : 0;

        var warnings = new List<string>();
        if (paletteCount > MaxRecommendedColors)
        {
            warnings.Add($"Palette đang có {paletteCount} màu; nên cân nhắc giảm.");
        }

        if (valueRange < MinValueRange)
        {
            warnings.Add("Độ tương phản sáng/tối thấp; sprite có thể khó đọc ở native 1x.");
        }

        if (visibleWidth > MaxVisibleWidth)
        {
            warnings.Add("Silhouette gần chạm biên ngang.");
        }

        if (visibleHeight > MaxVisibleHeight)
        {
            warnings.Add("Silhouette gần chạm biên dọc.");
        }

        return new SpriteAnalysis(paletteCount, visibleWidth, visibleHeight, valueRange, warnings);
    }
}
