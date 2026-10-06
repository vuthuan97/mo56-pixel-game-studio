using PixelGameStudio.Core.Primitives;
using PixelGameStudio.Rendering;
using Xunit;
using Xunit.Abstractions;

namespace PixelGameStudio.Rendering.Tests;

/// <summary>Prompt 05 checklist: palette swap and spritesheet pack primitives.</summary>
public class PixelPrimitiveTests
{
    private readonly ITestOutputHelper _output;

    public PixelPrimitiveTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void PaletteSwap_ReplacesExactMatches_KeepsOthers()
    {
        var buffer = new PixelBuffer(3, 1);
        buffer[0, 0] = new Rgba32(255, 222, 200, 255); // skin base
        buffer[1, 0] = new Rgba32(232, 178, 158, 255); // skin shadow
        buffer[2, 0] = new Rgba32(22, 16, 30, 255);    // outline (not in map)

        var map = new Dictionary<Rgba32, Rgba32>
        {
            [new Rgba32(255, 222, 200, 255)] = new(190, 145, 110, 255), // Sáng → Ngăm
            [new Rgba32(232, 178, 158, 255)] = new(154, 105, 78, 255),
        };

        PixelPrimitives.PaletteSwap(buffer, map);

        Assert.Equal(new Rgba32(190, 145, 110, 255), buffer[0, 0]);
        Assert.Equal(new Rgba32(154, 105, 78, 255), buffer[1, 0]);
        Assert.Equal(new Rgba32(22, 16, 30, 255), buffer[2, 0]); // untouched
    }

    [Fact]
    public void PaletteSwap_AlphaParticipatesInMatch()
    {
        var buffer = new PixelBuffer(2, 1);
        buffer[0, 0] = new Rgba32(100, 200, 100, 180);
        buffer[1, 0] = new Rgba32(100, 200, 100, 255);

        PixelPrimitives.PaletteSwap(buffer, new Dictionary<Rgba32, Rgba32>
        {
            [new Rgba32(100, 200, 100, 255)] = new(0, 0, 255, 255),
        });

        Assert.Equal(new Rgba32(100, 200, 100, 180), buffer[0, 0]); // alpha differs → no match
        Assert.Equal(new Rgba32(0, 0, 255, 255), buffer[1, 0]);
    }

    [Fact]
    public void ApplyMask_MultipliesAlpha_KeepsRgb()
    {
        var target = new PixelBuffer(2, 1);
        target[0, 0] = new Rgba32(200, 100, 50, 255);
        target[1, 0] = new Rgba32(200, 100, 50, 200);
        var mask = new PixelBuffer(2, 1);
        mask[0, 0] = new Rgba32(0, 0, 0, 128);
        mask[1, 0] = new Rgba32(0, 0, 0, 0);

        PixelOps.ApplyMask(target, mask);

        // DIV255(255*128) = 128; DIV255(200*0) = 0
        Assert.Equal(128, target[0, 0].A);
        Assert.Equal(0, target[1, 0].A);
        Assert.Equal((byte)200, target[0, 0].R); // RGB untouched
        Assert.Equal((byte)50, target[0, 0].B);
    }

    [Fact]
    public void ApplyMask_SizeMismatch_Throws()
    {
        var target = new PixelBuffer(2, 1);
        var mask = new PixelBuffer(3, 1);
        Assert.Throws<ArgumentException>(() => PixelOps.ApplyMask(target, mask));
    }

    [Fact]
    public void SpritesheetPack_ProducesGridAndFrameMap()
    {
        var a = new PixelBuffer(32, 46);
        a.Fill(new Rgba32(255, 0, 0, 255));
        var b = new PixelBuffer(32, 46);
        b.Fill(new Rgba32(0, 255, 0, 255));
        var c = new PixelBuffer(32, 46);
        c.Fill(new Rgba32(0, 0, 255, 255));

        SpritesheetPackResult pack = PixelPrimitives.SpritesheetPack(
        [
            ("idle_0", a),
            ("idle_1", b),
            ("walk_0", c),
        ], columns: 2);

        Assert.Equal(64, pack.Sheet.Width);   // 2 cols
        Assert.Equal(92, pack.Sheet.Height);  // 2 rows
        Assert.Equal(32, pack.CellWidth);
        Assert.Equal(46, pack.CellHeight);

        Assert.Equal((0, 0), (pack.Cells[0].Column, pack.Cells[0].Row));
        Assert.Equal((1, 0), (pack.Cells[1].Column, pack.Cells[1].Row));
        Assert.Equal((0, 1), (pack.Cells[2].Column, pack.Cells[2].Row));

        // frames landed at their cells
        Assert.Equal(new Rgba32(255, 0, 0, 255), pack.Sheet[0, 0]);
        Assert.Equal(new Rgba32(0, 255, 0, 255), pack.Sheet[32, 0]);
        Assert.Equal(new Rgba32(0, 0, 255, 255), pack.Sheet[0, 46]);
        // empty cell (1,1) stays transparent
        Assert.Equal(Rgba32.Transparent, pack.Sheet[63, 91]);
    }

    [Fact]
    public void SpritesheetPack_HandlesRaggedFrameSizes()
    {
        var small = new PixelBuffer(8, 8);
        small.Fill(new Rgba32(255, 255, 255, 255));
        var large = new PixelBuffer(16, 12);
        large.Fill(new Rgba32(255, 0, 0, 255));

        SpritesheetPackResult pack = PixelPrimitives.SpritesheetPack(
        [
            ("s", small),
            ("l", large),
        ]);

        Assert.Equal(32, pack.Sheet.Width);  // 2 columns of 16
        Assert.Equal(12, pack.Sheet.Height);
        Assert.Equal(16, pack.CellWidth);
        Assert.Equal(12, pack.CellHeight);
        Assert.Equal(new Rgba32(255, 255, 255, 255), pack.Sheet[0, 0]);
        Assert.Equal(Rgba32.Transparent, pack.Sheet[8, 0]);  // small frame leaves gutter in its cell
        Assert.Equal(new Rgba32(255, 0, 0, 255), pack.Sheet[16, 0]); // large frame at col 1
    }
}
