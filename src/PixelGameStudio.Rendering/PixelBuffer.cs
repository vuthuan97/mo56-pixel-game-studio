using PixelGameStudio.Core.Primitives;

namespace PixelGameStudio.Rendering;

/// <summary>
/// Dense RGBA pixel buffer. All pixel operations work at native resolution
/// with integer coordinates only; no interpolation, no anti-aliasing.
/// </summary>
public sealed class PixelBuffer
{
    private readonly Rgba32[] _pixels;

    public PixelBuffer(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Canvas size must be positive.");
        }

        Width = width;
        Height = height;
        _pixels = new Rgba32[width * height];
    }

    public int Width { get; }

    public int Height { get; }

    public Rgba32 this[int x, int y]
    {
        get => _pixels[(y * Width) + x];
        set => _pixels[(y * Width) + x] = value;
    }

    public bool Contains(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    public void Fill(Rgba32 color)
    {
        Array.Fill(_pixels, color);
    }

    public PixelBuffer Clone()
    {
        var copy = new PixelBuffer(Width, Height);
        Array.Copy(_pixels, copy._pixels, _pixels.Length);
        return copy;
    }

    /// <summary>Collects opaque pixels into a dictionary keyed by color, with occurrence counts.</summary>
    public int CountDistinctOpaqueColors()
    {
        var seen = new HashSet<Rgba32>();
        foreach (var p in _pixels)
        {
            if (p.A > 0)
            {
                seen.Add(p);
            }
        }

        return seen.Count;
    }
}
