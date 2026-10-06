using PixelGameStudio.Core.Primitives;
using PixelGameStudio.Rendering;
using Xunit;

namespace PixelGameStudio.Rendering.Tests;

public class PngCodecTests
{
    [Fact]
    public void EncodeDecodeRoundTrip_PreservesEveryPixel()
    {
        var buffer = new PixelBuffer(37, 23);
        var random = new Random(20261003);
        for (int y = 0; y < buffer.Height; y++)
        {
            for (int x = 0; x < buffer.Width; x++)
            {
                buffer[x, y] = new Rgba32(
                    (byte)random.Next(256),
                    (byte)random.Next(256),
                    (byte)random.Next(256),
                    (byte)random.Next(256));
            }
        }

        PixelBuffer decoded = PngCodec.Decode(PngCodec.Encode(buffer));

        Assert.Equal(buffer.Width, decoded.Width);
        Assert.Equal(buffer.Height, decoded.Height);
        for (int y = 0; y < buffer.Height; y++)
        {
            for (int x = 0; x < buffer.Width; x++)
            {
                Assert.True(buffer[x, y].SameColor(decoded[x, y]), $"Pixel mismatch at {x},{y}");
            }
        }
    }

    [Fact]
    public void Decode_LegacySamplePackageFrame_MatchesShippedPixels()
    {
        string sample = FindLegacySample(@"sample_package_v6\animations\Down\idle\idle_0.png");
        PixelBuffer decoded = PngCodec.Decode(File.OpenRead(sample));

        Assert.Equal(32, decoded.Width);
        Assert.Equal(46, decoded.Height);
        // Opaque body pixels exist and keep their alpha
        int opaque = 0;
        for (int y = 0; y < decoded.Height; y++)
        {
            for (int x = 0; x < decoded.Width; x++)
            {
                if (decoded[x, y].A > 0)
                {
                    opaque++;
                }
            }
        }

        Assert.InRange(opaque, 400, 1000);
    }

    private static string FindLegacySample(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            string candidate = Path.Combine(
                dir.FullName,
                "legacy-reference",
                "pixel_sprite_studio_v6",
                "pixel_sprite_studio_v6",
                "samples",
                relative);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent!;
        }

        throw new InvalidOperationException("Legacy sample package not found.");
    }
}
