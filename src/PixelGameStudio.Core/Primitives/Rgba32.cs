namespace PixelGameStudio.Core.Primitives;

/// <summary>Non-premultiplied RGBA color, 8 bit per channel.</summary>
public readonly partial record struct Rgba32(byte R, byte G, byte B, byte A)
{
    public static readonly Rgba32 Transparent = new(0, 0, 0, 0);

    public bool IsOpaque => A == 255;

    public bool IsTransparent => A == 0;

    public static Rgba32 FromBytes(byte r, byte g, byte b, byte a) => new(r, g, b, a);

    public bool SameColor(Rgba32 other) => R == other.R && G == other.G && B == other.B && A == other.A;
}
