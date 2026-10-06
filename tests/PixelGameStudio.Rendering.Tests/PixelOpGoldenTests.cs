using System.Text.Json;
using PixelGameStudio.Core.Primitives;
using PixelGameStudio.Rendering;
using Xunit;
using Xunit.Abstractions;

namespace PixelGameStudio.Rendering.Tests;

/// <summary>Golden tests for outline, grayscale and nearest-neighbor resize against prototype fixtures.</summary>
public class PixelOpGoldenTests
{
    private readonly ITestOutputHelper _output;

    public PixelOpGoldenTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void Outline_MatchesPillow()
    {
        using var doc = FixturePath.LoadJson(Path.Combine(FixturePath.Primitives, "manifest.json"));
        var cases = doc.RootElement.GetProperty("outline");
        Assert.NotEmpty(cases.EnumerateArray());

        foreach (var entry in cases.EnumerateArray())
        {
            string file = entry.GetProperty("file").GetString()!;
            int[] outlineColor = entry.GetProperty("outlineColor").EnumerateArray().Select(v => v.GetInt32()).ToArray();
            var mask = new PixelBuffer(32, 46);
            foreach (var point in entry.GetProperty("mask").EnumerateArray())
            {
                mask[point[0].GetInt32(), point[1].GetInt32()] = new Rgba32(255, 255, 255, 255);
            }

            PixelBuffer actual = PixelOps.Outline(mask, new Rgba32(
                (byte)outlineColor[0], (byte)outlineColor[1], (byte)outlineColor[2], (byte)outlineColor[3]));
            PixelBuffer expected = PngCodec.Decode(File.OpenRead(Path.Combine(FixturePath.Primitives, file)));
            AssertPixelsEqual(file, expected, actual);
        }
    }

    [Fact]
    public void Grayscale_MatchesPillow()
    {
        using var doc = FixturePath.LoadJson(Path.Combine(FixturePath.Primitives, "manifest.json"));
        var entry = doc.RootElement.GetProperty("grayscale").EnumerateArray().Single();
        string file = entry.GetProperty("file").GetString()!;

        // Rebuild the source strip, convert, compare against the fixture PNG.
        var source = new PixelBuffer(entry.GetProperty("colors").GetArrayLength(), 1);
        int index = 0;
        foreach (var color in entry.GetProperty("colors").EnumerateArray())
        {
            source[index++, 0] = new Rgba32(
                (byte)color[0].GetInt32(), (byte)color[1].GetInt32(),
                (byte)color[2].GetInt32(), (byte)color[3].GetInt32());
        }

        PixelBuffer actual = PixelOps.ToGrayscale(source);
        PixelBuffer expected = PngCodec.Decode(File.OpenRead(Path.Combine(FixturePath.Primitives, file)));
        AssertPixelsEqual(file, expected, actual);
    }

    [Theory]
    [InlineData("sprite_2x.png", 64, 92)]
    [InlineData("sprite_4x.png", 128, 184)]
    [InlineData("sprite_10x.png", 320, 460)]
    [InlineData("marker_13x8.png", 13, 8)]
    [InlineData("marker_3x2.png", 3, 2)]
    [InlineData("marker_7x4.png", 7, 4)]
    [InlineData("marker_50x30.png", 50, 30)]
    public void NearestResize_MatchesPillow(string file, int width, int height)
    {
        string sourceFile = file.StartsWith("sprite", StringComparison.Ordinal)
            ? "src_32x46.png"
            : "src_5x3.png";
        PixelBuffer source = PngCodec.Decode(File.OpenRead(Path.Combine(FixturePath.Primitives, "nearest", sourceFile)));
        PixelBuffer actual = PixelOps.NearestResize(source, width, height);
        PixelBuffer expected = PngCodec.Decode(File.OpenRead(Path.Combine(FixturePath.Primitives, "nearest", file)));
        AssertPixelsEqual(file, expected, actual);
    }

    private void AssertPixelsEqual(string name, PixelBuffer expected, PixelBuffer actual)
    {
        Assert.Equal(expected.Width, actual.Width);
        Assert.Equal(expected.Height, actual.Height);
        var diffs = new List<string>();
        for (int y = 0; y < expected.Height && diffs.Count < 10; y++)
        {
            for (int x = 0; x < expected.Width && diffs.Count < 10; x++)
            {
                if (!expected[x, y].SameColor(actual[x, y]))
                {
                    diffs.Add($"({x},{y}) expected={expected[x, y]} got={actual[x, y]}");
                }
            }
        }

        _output.WriteLine($"{name}: {diffs.Count}+ diffs");
        Assert.True(diffs.Count == 0, string.Join("\n", diffs));
    }
}
