namespace PixelGameStudio.Domain.Profiles;

/// <summary>
/// Native pixel canvas of the project. Every sprite in the project shares this
/// native resolution; the values are plain data — no part of the core hard-codes
/// a canvas size.
/// </summary>
public sealed class PixelProfile
{
    public int CanvasWidth { get; set; } = 64;

    public int CanvasHeight { get; set; } = 64;

    /// <summary>Art pixel unit (1 = one native pixel; 2 = chunky pixels of 2x2 native pixels).</summary>
    public int PixelSize { get; set; } = 1;
}
