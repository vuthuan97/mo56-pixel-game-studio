using System.Text.Json;
using PixelGameStudio.Rendering;
using PixelGameStudio.Rendering.Procedural;
using PixelGameStudio.Validation;
using Xunit;
using Xunit.Abstractions;

namespace PixelGameStudio.Rendering.Tests;

/// <summary>
/// The core Phase 0 gate: the C# renderer must reproduce the legacy prototype
/// sprite pixel-for-pixel across a matrix of specs, poses, directions and
/// preview modes, and the SpriteAnalyzer must reproduce the legacy Art QA
/// numbers for every fixture.
/// </summary>
public class SpriteGoldenTests
{
    private readonly ITestOutputHelper _output;

    public SpriteGoldenTests(ITestOutputHelper output) => _output = output;

    public static IEnumerable<object[]> SpriteCases()
    {
        using var doc = FixturePath.LoadJson(Path.Combine(FixturePath.Sprites, "manifest.json"));
        foreach (var entry in doc.RootElement.EnumerateArray())
        {
            string file = entry.GetProperty("file").GetString()!;
            yield return new object[] { file, entry.Clone() };
        }
    }

    [Theory]
    [MemberData(nameof(SpriteCases))]
    public void Renderer_MatchesPrototype(string file, JsonElement entry)
    {
        var renderer = new LegacySpriteRenderer();
        renderer.SetAnchorStore(new LegacyAnchorStore());

        var spec = BuildSpec(entry);
        LegacyPose pose = BuildPose(entry);
        var mode = entry.GetProperty("mode").GetString() switch
        {
            "Grayscale" => LegacyRenderMode.Grayscale,
            "Silhouette" => LegacyRenderMode.Silhouette,
            "Part Debug" => LegacyRenderMode.PartDebug,
            "Anchor Debug" => LegacyRenderMode.AnchorDebug,
            _ => LegacyRenderMode.Normal,
        };

        PixelBuffer actual = renderer.Render(spec, pose, mode);
        PixelBuffer expected = PngCodec.Decode(File.OpenRead(Path.Combine(FixturePath.Sprites, file)));

        var diffs = new List<string>();
        for (int y = 0; y < expected.Height && diffs.Count < 12; y++)
        {
            for (int x = 0; x < expected.Width && diffs.Count < 12; x++)
            {
                if (!expected[x, y].SameColor(actual[x, y]))
                {
                    diffs.Add($"({x},{y}) expected={expected[x, y]} got={actual[x, y]}");
                }
            }
        }

        _output.WriteLine($"{file}: {diffs.Count}+ diffs");
        Assert.True(diffs.Count == 0, $"{file}\n{string.Join("\n", diffs)}");
    }

    [Theory]
    [MemberData(nameof(SpriteCases))]
    public void Analyzer_MatchesPrototypeArtQa(string file, JsonElement entry)
    {
        PixelBuffer buffer = PngCodec.Decode(File.OpenRead(Path.Combine(FixturePath.Sprites, file)));
        SpriteAnalysis analysis = SpriteAnalyzer.Analyze(buffer);

        JsonElement qa = entry.GetProperty("qa");
        Assert.True(analysis.PaletteCount == qa.GetProperty("palette_count").GetInt32(),
            $"{file} palette_count: expected {qa.GetProperty("palette_count").GetInt32()} got {analysis.PaletteCount}");
        Assert.True(analysis.VisibleWidth == qa.GetProperty("visible_size")[0].GetInt32(),
            $"{file} visible width: expected {qa.GetProperty("visible_size")[0].GetInt32()} got {analysis.VisibleWidth}");
        Assert.True(analysis.VisibleHeight == qa.GetProperty("visible_size")[1].GetInt32(),
            $"{file} visible height: expected {qa.GetProperty("visible_size")[1].GetInt32()} got {analysis.VisibleHeight}");
        Assert.True(analysis.ValueRange == qa.GetProperty("value_range").GetInt32(),
            $"{file} value_range: expected {qa.GetProperty("value_range").GetInt32()} got {analysis.ValueRange}");

        string[] expectedWarnings = qa.GetProperty("warnings").EnumerateArray()
            .Select(w => w.GetString()!).ToArray();
        Assert.True(expectedWarnings.SequenceEqual(analysis.Warnings),
            $"{file} warnings: expected [{string.Join("; ", expectedWarnings)}] got [{string.Join("; ", analysis.Warnings)}]");
    }

    internal static LegacySpriteSpec BuildSpec(JsonElement entry)
    {
        var spec = new LegacySpriteSpec { Direction = entry.GetProperty("direction").GetString()! };
        foreach (var property in entry.GetProperty("spec").EnumerateObject())
        {
            string value = property.Value.GetString()!;
            switch (property.Name)
            {
                case "gender": spec.Gender = value; break;
                case "class_type": spec.ClassType = value; break;
                case "body_type": spec.BodyType = value; break;
                case "skin_tone": spec.SkinTone = value; break;
                case "head_shape": spec.HeadShape = value; break;
                case "eye_style": spec.EyeStyle = value; break;
                case "nose_style": spec.NoseStyle = value; break;
                case "mouth_style": spec.MouthStyle = value; break;
                case "hair_style": spec.HairStyle = value; break;
                case "hair_color": spec.HairColor = value; break;
                case "skin_variant": spec.SkinVariant = value; break;
                case "aura": spec.Aura = value; break;
                case "effect": spec.Effect = value; break;
                case "element": spec.Element = value; break;
            }
        }

        foreach (var property in entry.GetProperty("equipment").EnumerateObject())
        {
            spec.Equipment[property.Name] = property.Value.GetString()!;
        }

        return spec;
    }

    internal static LegacyPose BuildPose(JsonElement entry)
    {
        if (entry.TryGetProperty("pose", out JsonElement poseName) &&
            poseName.ValueKind == JsonValueKind.String)
        {
            return LegacyPosePresets.Poses[poseName.GetString()!];
        }

        var pose = new LegacyPose("golden_override");
        if (entry.TryGetProperty("poseOverride", out JsonElement overrides))
        {
            foreach (var property in overrides.EnumerateObject())
            {
                switch (property.Name)
                {
                    case "body_dx": pose = pose with { BodyDx = property.Value.GetInt32() }; break;
                    case "body_dy": pose = pose with { BodyDy = property.Value.GetInt32() }; break;
                    case "head_dx": pose = pose with { HeadDx = property.Value.GetInt32() }; break;
                    case "head_dy": pose = pose with { HeadDy = property.Value.GetInt32() }; break;
                    case "left_arm": pose = pose with { LeftArm = property.Value.GetString()! }; break;
                    case "right_arm": pose = pose with { RightArm = property.Value.GetString()! }; break;
                    case "left_leg": pose = pose with { LeftLeg = property.Value.GetString()! }; break;
                    case "right_leg": pose = pose with { RightLeg = property.Value.GetString()! }; break;
                    case "hair_state": pose = pose with { HairState = property.Value.GetString()! }; break;
                    case "weapon_state": pose = pose with { WeaponState = property.Value.GetString()! }; break;
                    case "torso_tilt": pose = pose with { TorsoTilt = property.Value.GetString()! }; break;
                }
            }
        }

        return pose;
    }
}
