using PixelGameStudio.Core.Primitives;
using PixelGameStudio.Rendering;

namespace PixelGameStudio.App.Rendering;

/// <summary>Composites a viewport-only backdrop; project assets and exports retain alpha.</summary>
public static class PreviewBackdrop
{
    public static PixelBuffer Apply(PixelBuffer sprite, string mode)
    {
        var result = new PixelBuffer(sprite.Width, sprite.Height);
        for (int y = 0; y < sprite.Height; y++)
        {
            for (int x = 0; x < sprite.Width; x++)
            {
                result[x, y] = mode switch
                {
                    "Sáng" => new Rgba32(245, 247, 250, 255),
                    "Tối" => new Rgba32(42, 46, 54, 255),
                    _ => ((x / 8 + y / 8) & 1) == 0
                        ? new Rgba32(246, 248, 250, 255)
                        : new Rgba32(220, 225, 230, 255),
                };
            }
        }

        PixelOps.Composite(result, sprite);
        return result;
    }
}
