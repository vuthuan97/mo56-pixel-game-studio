using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PixelGameStudio.Rendering;

namespace PixelGameStudio.App.Rendering;

/// <summary>
/// UI-side adapter: uploads a renderer PixelBuffer into an Avalonia
/// WriteableBitmap (BGRA8888). Lives in the App project — the renderer itself
/// stays free of Avalonia types.
/// </summary>
public static class PixelBufferBitmapExtensions
{
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
                        byte a = p.A;
                        uint b = (uint)((p.B * a) / 255);
                        uint g = (uint)((p.G * a) / 255);
                        uint r = (uint)((p.R * a) / 255);
                        line[x] = ((uint)a << 24) | (b << 16) | (g << 8) | r;
                    }
                }
            }
        }

        return bitmap;
    }
}
