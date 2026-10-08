using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PixelGameStudio.Core.Primitives;
using PixelGameStudio.Rendering;

namespace PixelGameStudio.App.Rendering;

/// <summary>
/// UI-side adapter: uploads a renderer PixelBuffer into an Avalonia
/// WriteableBitmap (BGRA8888). Lives in the App project — the renderer itself
/// stays free of Avalonia types.
/// </summary>
public static class PixelBufferBitmapExtensions
{
    /// <summary>Returns one premultiplied BGRA8888 pixel in native little-endian layout.</summary>
    public static uint PackPremultipliedBgra(Rgba32 pixel)
    {
        byte a = pixel.A;
        uint b = (uint)((pixel.B * a) / 255);
        uint g = (uint)((pixel.G * a) / 255);
        uint r = (uint)((pixel.R * a) / 255);
        return ((uint)a << 24) | (r << 16) | (g << 8) | b;
    }

    public static WriteableBitmap ToWriteableBitmap(this PixelBuffer buffer, double dpi = 96)
    {
        var bitmap = new WriteableBitmap(
            new PixelSize(buffer.Width, buffer.Height),
            new Vector(dpi, dpi),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        using (var frame = bitmap.Lock())
        {
            unsafe
            {
                var row = (uint*)frame.Address;
                int stride = frame.RowBytes / 4;
                for (int y = 0; y < buffer.Height; y++)
                {
                    uint* line = row + (y * stride);
                    for (int x = 0; x < buffer.Width; x++)
                    {
                        var p = buffer[x, y];
                        // straight alpha → premultiplied BGRA expected by the bitmap
                        // PixelFormat.Bgra8888 is laid out as B, G, R, A in
                        // little-endian memory. PackPremultipliedBgra keeps
                        // the straight-alpha PixelBuffer contract at the UI
                        // boundary without swapping red and blue.
                        line[x] = PackPremultipliedBgra(p);
                    }
                }
            }
        }

        return bitmap;
    }
}
