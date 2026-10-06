using System.Text.Json;
using PixelGameStudio.Core.Primitives;
using PixelGameStudio.Rendering;
using Xunit;
using Xunit.Abstractions;

namespace PixelGameStudio.Rendering.Tests;

/// <summary>
/// Golden-master tests for the alpha_composite port. Expected values were
/// generated from the legacy prototype's Pillow 12.3.0 Image.alpha_composite.
/// </summary>
public class CompositeGoldenTests
{
    private readonly ITestOutputHelper _output;

    public CompositeGoldenTests(ITestOutputHelper output) => _output = output;

    public static IEnumerable<object[]> Cases()
    {
        using var doc = FixturePath.LoadJson(FixturePath.CompositeCases);
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            yield return new object[]
            {
                element.GetProperty("base").GetRawText(),
                element.GetProperty("over").GetRawText(),
                element.GetProperty("out").GetRawText(),
            };
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Composite_MatchesPillow(string baseJson, string overJson, string outJson)
    {
        var expected = Deserialize(outJson);
        var baseBuffer = new PixelBuffer(1, 1);
        baseBuffer[0, 0] = Deserialize(baseJson);
        var overlay = new PixelBuffer(1, 1);
        overlay[0, 0] = Deserialize(overJson);

        PixelOps.Composite(baseBuffer, overlay);

        Assert.True(baseBuffer[0, 0].SameColor(expected),
            $"base={baseJson} over={overJson} expected={outJson} got={baseBuffer[0, 0]}");
    }

    private static Rgba32 Deserialize(string json)
    {
        int[] values = JsonSerializer.Deserialize<int[]>(json)!;
        return new Rgba32((byte)values[0], (byte)values[1], (byte)values[2], (byte)values[3]);
    }
}
