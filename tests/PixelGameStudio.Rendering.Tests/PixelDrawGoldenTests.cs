using System.Text.Json;
using PixelGameStudio.Core.Primitives;
using PixelGameStudio.Rendering;
using Xunit;
using Xunit.Abstractions;

namespace PixelGameStudio.Rendering.Tests;

/// <summary>
/// Golden-master tests for the drawing primitives, diffed pixel-for-pixel
/// against PNG fixtures rendered by the legacy prototype (Pillow 12.3.0).
/// </summary>
public class PixelDrawGoldenTests
{
    private readonly ITestOutputHelper _output;

    public PixelDrawGoldenTests(ITestOutputHelper output) => _output = output;

    private delegate void DrawOp(PixelBuffer buffer, JsonElement entry);

    public static IEnumerable<object[]> EllipseCases() =>
        LoadCases("ellipse", e => (buffer, entry) =>
        {
            int[] bbox = entry.GetProperty("bbox").EnumerateArray().Select(v => v.GetInt32()).ToArray();
            PixelDraw.FillEllipse(buffer, bbox[0], bbox[1], bbox[2], bbox[3], new Rgba32(255, 90, 40, 255));
        });

    public static IEnumerable<object[]> LineCases() =>
        LoadCases("line", e => (buffer, entry) =>
        {
            int[] from = entry.GetProperty("from").EnumerateArray().Select(v => v.GetInt32()).ToArray();
            int[] to = entry.GetProperty("to").EnumerateArray().Select(v => v.GetInt32()).ToArray();
            PixelDraw.DrawLine(buffer, from[0], from[1], to[0], to[1], new Rgba32(60, 200, 120, 255));
        });

    public static IEnumerable<object[]> RectCases() =>
        LoadCases("rect", e => (buffer, entry) =>
        {
            int[] bbox = entry.GetProperty("bbox").EnumerateArray().Select(v => v.GetInt32()).ToArray();
            PixelDraw.FillRect(buffer, bbox[0], bbox[1], bbox[2], bbox[3], new Rgba32(90, 90, 220, 255));
        });

    public static IEnumerable<object[]> PolygonCases() =>
        LoadCases("polygon", e => (buffer, entry) =>
        {
            var points = entry.GetProperty("points").EnumerateArray()
                .Select(p => (p[0].GetInt32(), p[1].GetInt32()))
                .ToList();
            PixelDraw.FillPolygon(buffer, points, new Rgba32(230, 210, 90, 255));
        });

    private static IEnumerable<object[]> LoadCases(string kind, Func<JsonElement, Action<PixelBuffer, JsonElement>> opFactory)
    {
        using var doc = FixturePath.LoadJson(Path.Combine(FixturePath.Primitives, "manifest.json"));
        foreach (var entry in doc.RootElement.GetProperty(kind).EnumerateArray())
        {
            // Clone so the element outlives the JsonDocument; the op delegate captures it too.
            JsonElement cloned = entry.Clone();
            string file = cloned.GetProperty("file").GetString()!;
            yield return new object[] { file, cloned, opFactory(cloned) };
        }
    }

    [Theory]
    [MemberData(nameof(EllipseCases))]
    public void Ellipse_MatchesPillow(string file, JsonElement entry, Action<PixelBuffer, JsonElement> op) =>
        DiffAgainstFixture(file, op, entry);

    [Theory]
    [MemberData(nameof(LineCases))]
    public void Line_MatchesPillow(string file, JsonElement entry, Action<PixelBuffer, JsonElement> op) =>
        DiffAgainstFixture(file, op, entry);

    [Theory]
    [MemberData(nameof(RectCases))]
    public void Rect_MatchesPillow(string file, JsonElement entry, Action<PixelBuffer, JsonElement> op) =>
        DiffAgainstFixture(file, op, entry);

    [Theory]
    [MemberData(nameof(PolygonCases))]
    public void Polygon_MatchesPillow(string file, JsonElement entry, Action<PixelBuffer, JsonElement> op) =>
        DiffAgainstFixture(file, op, entry);

    private void DiffAgainstFixture(string file, Action<PixelBuffer, JsonElement> op, JsonElement entry)
    {
        PixelBuffer expected = PngCodec.Decode(File.OpenRead(Path.Combine(FixturePath.Primitives, file)));
        var actual = new PixelBuffer(expected.Width, expected.Height);
        op(actual, entry);

        List<string> diffs = [];
        for (int y = 0; y < expected.Height; y++)
        {
            for (int x = 0; x < expected.Width; x++)
            {
                if (!expected[x, y].SameColor(actual[x, y]))
                {
                    diffs.Add($"{entry}: ({x},{y}) expected={expected[x, y]} got={actual[x, y]}");
                    if (diffs.Count >= 10)
                    {
                        break;
                    }
                }
            }

            if (diffs.Count >= 10)
            {
                break;
            }
        }

        _output.WriteLine($"{file}: {diffs.Count}+ diffs");
        Assert.True(diffs.Count == 0, string.Join("\n", diffs));
    }
}
