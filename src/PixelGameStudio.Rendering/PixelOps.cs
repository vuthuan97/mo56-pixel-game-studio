using PixelGameStudio.Core.Primitives;

namespace PixelGameStudio.Rendering;

/// <summary>
/// Pixel operations ported bit-exact from the Pillow operations used by the
/// legacy prototype: alpha compositing (AlphaComposite.c integer precision),
/// 1px outline, recolor, ITU-R 601-2 grayscale and nearest-neighbor scaling.
/// </summary>
public static class PixelOps
{
    /// <summary>
    /// Composites <paramref name="overlay"/> over <paramref name="baseBuffer"/>
    /// at integer offset (Pillow Image.alpha_composite semantics). Regions
    /// falling outside the base are clipped. Mutates the base buffer.
    /// </summary>
    public static void Composite(PixelBuffer baseBuffer, PixelBuffer overlay, int offsetX = 0, int offsetY = 0)
    {
        ArgumentNullException.ThrowIfNull(baseBuffer);
        ArgumentNullException.ThrowIfNull(overlay);

        int startX = Math.Max(0, offsetX);
        int startY = Math.Max(0, offsetY);
        int endX = Math.Min(baseBuffer.Width, offsetX + overlay.Width);
        int endY = Math.Min(baseBuffer.Height, offsetY + overlay.Height);

        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                Rgba32 src = overlay[x - offsetX, y - offsetY];
                if (src.A == 0)
                {
                    continue; // Pillow copies the destination pixel
                }

                Rgba32 dst = baseBuffer[x, y];

                // AlphaComposite.c: integer implementation with 7 extra precision bits.
                uint blend = (uint)(dst.A * (255 - src.A));
                uint outA255 = (uint)((src.A * 255) + blend);
                uint coef1 = (uint)((uint)src.A * 255 * 255 * 128 / outA255);
                uint coef2 = (uint)(255 * 128) - coef1;

                baseBuffer[x, y] = new Rgba32(
                    (byte)(ShiftForDiv255((uint)((src.R * coef1) + (dst.R * coef2)) + 0x4000) >> 7),
                    (byte)(ShiftForDiv255((uint)((src.G * coef1) + (dst.G * coef2)) + 0x4000) >> 7),
                    (byte)(ShiftForDiv255((uint)((src.B * coef1) + (dst.B * coef2)) + 0x4000) >> 7),
                    (byte)ShiftForDiv255(outA255 + 0x80));
            }
        }
    }

    private static uint ShiftForDiv255(uint a) => ((a >> 8) + a) >> 8;

    /// <summary>
    /// 1px external outline: transparent pixels adjacent (4-neighborhood) to a
    /// pixel with alpha > 0 receive the outline color. Bit-exact port of the
    /// legacy prototype outline pass.
    /// </summary>
    public static PixelBuffer Outline(PixelBuffer source, Rgba32 outlineColor)
    {
        var result = source.Clone();
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                if (source[x, y].A != 0)
                {
                    continue;
                }

                if ((x + 1 < source.Width && source[x + 1, y].A > 0) ||
                    (x - 1 >= 0 && source[x - 1, y].A > 0) ||
                    (y + 1 < source.Height && source[x, y + 1].A > 0) ||
                    (y - 1 >= 0 && source[x, y - 1].A > 0))
                {
                    result[x, y] = outlineColor;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Reference-style one-pixel outline. The edge keeps a tint of the adjacent
    /// pixel and only shifts it toward a dark purple, avoiding a heavy solid
    /// contour around every internal part.
    /// </summary>
    public static PixelBuffer SelectiveOutline(PixelBuffer source, Rgba32 shadowColor, byte sourceWeight = 184)
    {
        var result = source.Clone();
        int[] dx = [0, -1, 1, 0];
        int[] dy = [1, 0, 0, -1];
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                if (source[x, y].A != 0)
                {
                    continue;
                }

                for (int i = 0; i < dx.Length; i++)
                {
                    int nx = x + dx[i];
                    int ny = y + dy[i];
                    if (!source.Contains(nx, ny) || source[nx, ny].A == 0)
                    {
                        continue;
                    }

                    Rgba32 edge = source[nx, ny];
                    int t = sourceWeight;
                    result[x, y] = new Rgba32(
                        (byte)((shadowColor.R * (255 - t) + edge.R * t) / 255),
                        (byte)((shadowColor.G * (255 - t) + edge.G * t) / 255),
                        (byte)((shadowColor.B * (255 - t) + edge.B * t) / 255),
                        255);
                    break;
                }
            }
        }

        return result;
    }

    /// <summary>Replaces every pixel with alpha > 0 by <paramref name="color"/> (all channels).</summary>
    public static void Recolor(PixelBuffer buffer, Rgba32 color)
    {
        for (int y = 0; y < buffer.Height; y++)
        {
            for (int x = 0; x < buffer.Width; x++)
            {
                if (buffer[x, y].A > 0)
                {
                    buffer[x, y] = color;
                }
            }
        }
    }

    /// <summary>
    /// Grayscale via Pillow's ITU-R 601-2 luma (L24 rounded), alpha preserved.
    /// </summary>
    public static PixelBuffer ToGrayscale(PixelBuffer source)
    {
        var result = new PixelBuffer(source.Width, source.Height);
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                Rgba32 p = source[x, y];
                int l = ((p.R * 19595) + (p.G * 38470) + (p.B * 7471) + 0x8000) >> 16;
                result[x, y] = new Rgba32((byte)l, (byte)l, (byte)l, p.A);
            }
        }

        return result;
    }

    /// <summary>Silhouette: every pixel with alpha > 0 becomes solid black.</summary>
    public static void ToSilhouette(PixelBuffer buffer) => Recolor(buffer, new Rgba32(0, 0, 0, 255));

    /// <summary>
    /// Nearest-neighbor resize to an arbitrary size; destination pixel (x, y)
    /// samples the source at floor((x + 0.5) * srcWidth / dstWidth) — the
    /// Pillow NEAREST mapping.
    /// </summary>
    public static PixelBuffer NearestResize(PixelBuffer source, int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Target size must be positive.");
        }

        var result = new PixelBuffer(width, height);
        for (int y = 0; y < height; y++)
        {
            int sy = (int)(((2L * y) + 1) * source.Height / (2L * height));
            if (sy >= source.Height)
            {
                sy = source.Height - 1;
            }

            for (int x = 0; x < width; x++)
            {
                int sx = (int)(((2L * x) + 1) * source.Width / (2L * width));
                if (sx >= source.Width)
                {
                    sx = source.Width - 1;
                }

                result[x, y] = source[sx, sy];
            }
        }

        return result;
    }

    /// <summary>
    /// Applies an alpha mask to the target in place: each target pixel's alpha
    /// is multiplied by the mask pixel's alpha (DIV255 rounding) — RGB stays
    /// untouched. The primitive behind "paste through a mask" operations.
    /// </summary>
    public static void ApplyMask(PixelBuffer target, PixelBuffer mask)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(mask);
        if (mask.Width != target.Width || mask.Height != target.Height)
        {
            throw new ArgumentException("Mask must match the target size.");
        }

        for (int y = 0; y < target.Height; y++)
        {
            for (int x = 0; x < target.Width; x++)
            {
                Rgba32 t = target[x, y];
                Rgba32 m = mask[x, y];
                target[x, y] = t with
                {
                    A = (byte)ShiftForDiv255((uint)((t.A * m.A) + 128)),
                };
            }
        }
    }

    /// <summary>Integer horizontal mirror (discrete flip — no interpolation).</summary>
    public static PixelBuffer MirrorX(PixelBuffer source)
    {
        var result = new PixelBuffer(source.Width, source.Height);
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                result[x, y] = source[source.Width - 1 - x, y];
            }
        }

        return result;
    }

    /// <summary>
    /// Fades every channel toward black by the given 0..255 mask using
    /// Pillow's BLEND rounding (DIV255(c*mask)) — the primitive behind onion
    /// skin (blend a faded previous frame under the current one).
    /// </summary>
    public static PixelBuffer Fade(PixelBuffer source, int mask)
    {
        if (mask is < 0 or > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(mask));
        }

        var result = new PixelBuffer(source.Width, source.Height);
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                Rgba32 p = source[x, y];
                result[x, y] = new Rgba32(
                    (byte)ShiftForDiv255((uint)((p.R * mask) + 128)),
                    (byte)ShiftForDiv255((uint)((p.G * mask) + 128)),
                    (byte)ShiftForDiv255((uint)((p.B * mask) + 128)),
                    (byte)ShiftForDiv255((uint)((p.A * mask) + 128)));
            }
        }

        return result;
    }

    /// <summary>Integer-factor nearest-neighbor scale (preview zoom).</summary>
    public static PixelBuffer NearestScale(PixelBuffer source, int factor) =>
        NearestResize(source, source.Width * factor, source.Height * factor);
}
