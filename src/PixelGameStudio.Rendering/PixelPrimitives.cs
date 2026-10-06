using PixelGameStudio.Core.Primitives;

namespace PixelGameStudio.Rendering;

/// <summary>
/// Palette swap and spritesheet packing primitives (the last two items of
/// prompt 05's primitive checklist). Both integer-only, no interpolation.
/// </summary>
public static class PixelPrimitives
{
    /// <summary>
    /// Replaces exact RGBA color matches per the map, in place. Pixels not in
    /// the map are untouched; alpha participates in the match.
    /// </summary>
    public static void PaletteSwap(PixelBuffer buffer, IReadOnlyDictionary<Rgba32, Rgba32> map)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(map);
        if (map.Count == 0)
        {
            return;
        }

        for (int y = 0; y < buffer.Height; y++)
        {
            for (int x = 0; x < buffer.Width; x++)
            {
                if (map.TryGetValue(buffer[x, y], out Rgba32 replacement))
                {
                    buffer[x, y] = replacement;
                }
            }
        }
    }

    /// <summary>
    /// Packs frames into a grid spritesheet. Cell size = largest frame; empty
    /// cells stay transparent. The returned cell list is the frame map — every
    /// frame is addressable by key (fixes the legacy exporter's missing map).
    /// </summary>
    public static SpritesheetPackResult SpritesheetPack(
        IReadOnlyList<(string Key, PixelBuffer Frame)> frames,
        int columns = 0)
    {
        ArgumentNullException.ThrowIfNull(frames);
        if (frames.Count == 0)
        {
            throw new ArgumentException("Need at least one frame.", nameof(frames));
        }

        int cellWidth = frames.Max(f => f.Frame.Width);
        int cellHeight = frames.Max(f => f.Frame.Height);
        int cols = columns > 0 ? columns : frames.Count;
        int rows = (frames.Count + cols - 1) / cols;

        var sheet = new PixelBuffer(cols * cellWidth, rows * cellHeight);
        var cells = new List<SpritesheetCell>(frames.Count);
        for (int i = 0; i < frames.Count; i++)
        {
            (string key, PixelBuffer frame) = frames[i];
            int col = i % cols;
            int row = i / cols;
            PixelOps.Composite(sheet, frame, col * cellWidth, row * cellHeight);
            cells.Add(new SpritesheetCell(key, col, row));
        }

        return new SpritesheetPackResult(sheet, cells, cellWidth, cellHeight);
    }
}
