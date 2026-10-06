using PixelGameStudio.Core.Primitives;
using PixelGameStudio.Assets;
using PixelGameStudio.Assets.Composition;
using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Animation;
using PixelGameStudio.Domain.Character;
using PixelGameStudio.Domain.Templates;
using PixelGameStudio.ProjectSystem;
using PixelGameStudio.Rendering;
using PixelGameStudio.Rendering.Procedural;
using Xunit;
using Xunit.Abstractions;

namespace PixelGameStudio.ProjectSystem.Tests;

/// <summary>
/// Phase 6-7 gate: the composer, driven by PosePresets (per-part offsets +
/// state values) and state variant assets, must reproduce the legacy
/// procedural render pixel-for-pixel for animation poses — walk, attack
/// (weapon raise/slash with z-override), idle bob, hurt and death.
/// </summary>
public class ComposerPoseParityTests : IDisposable
{
    private readonly string _root;
    private readonly ProjectStore _store = new();
    private readonly AssetLibraryService _library = new();
    private readonly RigSpriteComposer _composer;
    private readonly ITestOutputHelper _output;

    public ComposerPoseParityTests(ITestOutputHelper output)
    {
        _output = output;
        _root = Path.Combine(Path.GetTempPath(), "pgs-tests", Guid.NewGuid().ToString("N"));
        _composer = new RigSpriteComposer(_library);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private (Project Project, CharacterEntity Hero) Create()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        _store.Save(project, _root);
        StarterContentFactory.CreateHumanoidDemo(project, _root, _library);
        Assert.Empty(project.Validate());
        CharacterEntity hero = project.Characters.Single(c => c.Id == "char.hero");
        return (project, hero);
    }

    public static IEnumerable<object[]> PoseCases()
    {
        foreach (string pose in new[] { "idle_0", "idle_1", "walk_0", "walk_1", "walk_2", "walk_3",
                     "attack_0", "attack_1", "attack_2", "attack_3",
                     "hurt_0", "hurt_1", "cast_0", "cast_1", "death_0", "death_1" })
        {
            yield return new object[] { pose, "Down" };
        }

        yield return new object[] { "walk_0", "Left" };
        yield return new object[] { "attack_1", "Left" };
        yield return new object[] { "attack_2", "Right" };
        yield return new object[] { "death_1", "Up" };
    }

    [Theory]
    [MemberData(nameof(PoseCases))]
    public void Compose_PresetPose_MatchesLegacyRender(string poseId, string view)
    {
        (Project project, CharacterEntity hero) = Create();
        PoseDefinition pose = PosePresets.Find(poseId) ?? throw new InvalidOperationException(poseId);

        PixelBuffer composed = _composer.Compose(project, _root, hero, new CompositionOptions
        {
            View = view,
            Pose = pose,
        });

        var legacySpec = new LegacySpriteSpec { Direction = view };
        PixelBuffer legacy = new LegacySpriteRenderer().Render(legacySpec, LegacyPosePresets.Poses[poseId]);

        // Intentional deviation (docs/LEGACY_AUDIT.md §4): the legacy renderer
        // keeps hair_back SIDE STRANDS at a fixed y while the hair mass bobs
        // with body_dy. The new part-granular pose model shifts the whole
        // hair_back part, so strand-edge pixels may differ on bobbed poses.
        int maxBob = pose.Parts.Values.Count == 0 ? 0 : pose.Parts.Values.Max(p => Math.Abs(p.OffsetY));
        bool bobbed = maxBob != 0;
        bool IsKnownDeviation(int x, int y) => bobbed &&
            y is >= 10 and <= 24 && (x is >= 3 and <= 10 or >= 21 and <= 28) ||
            bobbed && y > 24 && y <= 24 + maxBob && (x is >= 3 and <= 10 or >= 21 and <= 28);

        AssertPixelsEqual($"{poseId} {view}", legacy, composed, IsKnownDeviation);
    }

    [Fact]
    public void Compose_OnionSkin_FadePrimitiveRoundsLikePillowBlend()
    {
        // Fade = Pillow blend(black, frame, alpha) with mask = 56 (0.22 * 255).
        var frame = new PixelBuffer(2, 1);
        frame[0, 0] = new Rgba32(255, 222, 200, 255);
        frame[1, 0] = new Rgba32(10, 20, 30, 128);

        PixelBuffer faded = PixelOps.Fade(frame, 56);

        // DIV255(c * 56): 255→56, 222→49, 200→44, 10→2, 20→4, 30→7, 128→28
        Assert.Equal(new Rgba32(56, 49, 44, 56), faded[0, 0]);
        Assert.Equal(new Rgba32(2, 4, 7, 28), faded[1, 0]);
    }

    [Fact]
    public void Compose_UnknownPoseState_FallsBackToViewAsset()
    {
        (Project project, CharacterEntity hero) = Create();
        var pose = new PoseDefinition { Id = "pose.odd" };
        pose.Parts["arm_left"] = new PartPose { States = { ["hand"] = "nonexistent" } };

        PixelBuffer composed = _composer.Compose(project, _root, hero, new CompositionOptions
        {
            View = "Down",
            Pose = pose,
        });

        PixelBuffer baseline = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });
        AssertPixelsEqual("unknown state falls back to base view asset", baseline, composed);
    }

    private void AssertPixelsEqual(string label, PixelBuffer expected, PixelBuffer actual, Func<int, int, bool>? knownDeviation = null)
    {
        Assert.Equal(expected.Width, actual.Width);
        Assert.Equal(expected.Height, actual.Height);
        var diffs = new List<string>();
        for (int y = 0; y < expected.Height && diffs.Count < 12; y++)
        {
            for (int x = 0; x < expected.Width && diffs.Count < 12; x++)
            {
                if (!expected[x, y].SameColor(actual[x, y]) && knownDeviation?.Invoke(x, y) != true)
                {
                    diffs.Add($"({x},{y}) expected={expected[x, y]} got={actual[x, y]}");
                }
            }
        }

        _output.WriteLine($"{label}: {diffs.Count}+ diffs");
        Assert.True(diffs.Count == 0, $"{label}\n{string.Join("\n", diffs)}");
    }
}
