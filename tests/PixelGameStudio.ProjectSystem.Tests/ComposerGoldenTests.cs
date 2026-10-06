using PixelGameStudio.Assets;
using PixelGameStudio.Assets.Composition;
using PixelGameStudio.Core.Primitives;
using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Animation;
using PixelGameStudio.Domain.Assets;
using PixelGameStudio.Domain.Character;
using PixelGameStudio.Domain.Templates;
using PixelGameStudio.ProjectSystem;
using PixelGameStudio.Rendering;
using PixelGameStudio.Rendering.Procedural;
using Xunit;
using Xunit.Abstractions;

namespace PixelGameStudio.ProjectSystem.Tests;

/// <summary>
/// The asset-driven composer must reproduce the legacy procedural render
/// pixel-for-pixel from starter content (the bridge from prototype to the new
/// architecture). Also covers palette overrides (Phase 4), equipment slots
/// (Phase 5), mirrors, pose offsets and hidden parts.
/// </summary>
public class ComposerGoldenTests : IDisposable
{
    private readonly string _root;
    private readonly ProjectStore _store = new();
    private readonly AssetLibraryService _library = new();
    private readonly RigSpriteComposer _composer;
    private readonly ITestOutputHelper _output;

    public ComposerGoldenTests(ITestOutputHelper output)
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

    private Project CreateProjectWithStarterContent()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Name = "Composer Golden";
        _store.Save(project, _root);
        StarterContentResult result = StarterContentFactory.CreateHumanoidDemo(project, _root, _library);
        Assert.Equal("char.hero", result.CharacterId);
        Assert.True(result.AssetCount > 40);
        Assert.Empty(project.Validate());
        return project;
    }

    [Theory]
    [InlineData("Down")]
    [InlineData("Up")]
    [InlineData("Left")]
    [InlineData("Right")]
    public void Compose_DefaultHero_MatchesLegacyRenderPixelForPixel(string view)
    {
        Project project = CreateProjectWithStarterContent();
        CharacterEntity hero = project.Characters.Single(c => c.Id == "char.hero");

        PixelBuffer composed = _composer.Compose(project, _root, hero, new CompositionOptions { View = view });

        var legacySpec = new LegacySpriteSpec { Direction = view };
        PixelBuffer legacy = new LegacySpriteRenderer().Render(legacySpec, LegacyPosePresets.Poses["idle_0"]);

        AssertPixelsEqual($"view {view}", legacy, composed);
    }

    [Fact]
    public void Compose_Unequipped_MatchesLegacyBareBody()
    {
        Project project = CreateProjectWithStarterContent();
        CharacterEntity hero = project.Characters.Single(c => c.Id == "char.hero");
        hero.Equipment.Clear();

        PixelBuffer composed = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });

        var legacySpec = LegacyStarterHelper.NeutralLegacySpec();
        PixelBuffer legacy = new LegacySpriteRenderer().Render(legacySpec, LegacyPosePresets.Poses["idle_0"]);

        AssertPixelsEqual("unequipped", legacy, composed);
    }

    [Fact]
    public void Compose_DifferentEquipment_MatchesLegacyRender()
    {
        Project project = CreateProjectWithStarterContent();
        CharacterEntity hero = project.Characters.Single(c => c.Id == "char.hero");
        hero.Equipment =
        [
            new EquippedItem("main_hand", "eq.main_hand.thuong.down"),
            new EquippedItem("off_hand", "eq.off_hand.ho-lo.down"),
            new EquippedItem("outer", "eq.outer.dan-tu.down"),
            new EquippedItem("cape", "eq.cape.ao-choang-dai.down"),
            new EquippedItem("head", "eq.head.mu-giap.down"),
            new EquippedItem("gloves", "eq.gloves.bao-tay-vai.down"),
        ];

        PixelBuffer composed = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });

        var legacySpec = new LegacySpriteSpec();
        foreach (string key in legacySpec.Equipment.Keys.ToList())
        {
            legacySpec.Equipment[key] = "Không";
        }

        legacySpec.Equipment["main_hand"] = "Thương";
        legacySpec.Equipment["off_hand"] = "Hồ lô";
        legacySpec.Equipment["outer"] = "Đan tu";
        legacySpec.Equipment["cape"] = "Áo choàng dài";
        legacySpec.Equipment["head"] = "Mũ giáp";
        legacySpec.Equipment["gloves"] = "Bao tay vải";
        PixelBuffer legacy = new LegacySpriteRenderer().Render(legacySpec, LegacyPosePresets.Poses["idle_0"]);

        AssertPixelsEqual("mixed equipment", legacy, composed);
    }

    [Fact]
    public void Compose_PaletteOverride_RecolorsOnlyTargetPart()
    {
        Project project = CreateProjectWithStarterContent();
        CharacterEntity hero = project.Characters.Single(c => c.Id == "char.hero");
        PixelBuffer baseline = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });

        hero.AppearanceOf("head")!.PaletteOverride.Add(new PaletteMapping("#FFDEC8FF", "#FF4060FF")); // da → đỏ
        hero.AppearanceOf("torso")!.PaletteOverride.Add(new PaletteMapping("#FFDEC8FF", "#FF4060FF"));

        PixelBuffer recolored = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });

        // head skin pixel changed
        Assert.Equal(new Rgba32(255, 64, 96, 255), recolored[16, 15]);
        Assert.NotEqual(baseline[16, 15], recolored[16, 15]);
        // hair color untouched (not in the palette map)
        Assert.Equal(new Rgba32(44, 46, 78, 255), recolored[16, 1]); // topknot hair base
    }

    [Fact]
    public void Compose_Mirror_LeftEqualsMirroredRight()
    {
        Project project = CreateProjectWithStarterContent();
        var mirrored = new CharacterEntity
        {
            Id = "char.mirror",
            Name = "Mirror test",
            RigId = "rig.humanoid.topdown4",
            Appearance = project.Rigs[0].Parts.Where(p => p.Id != "weapon").Select(p =>
            {
                var appearance = new PartAppearance { PartId = p.Id };
                appearance.ViewAssets["Right"] = new AssetReference { AssetId = $"part.{p.Id}.right" };
                appearance.ViewAssets["Left"] = new AssetReference { AssetId = $"part.{p.Id}.right" };
                appearance.ViewMirrors.Add("Left");
                return appearance;
            }).ToList(),
        };

        PixelBuffer left = _composer.Compose(project, _root, mirrored, new CompositionOptions { View = "Left", ApplyOutline = false });
        PixelBuffer right = _composer.Compose(project, _root, mirrored, new CompositionOptions { View = "Right", ApplyOutline = false });

        PixelBuffer mirroredRight = PixelOps.MirrorX(right);
        AssertPixelsEqual("mirror left==mirror(right)", left, mirroredRight);
    }

    [Fact]
    public void Compose_PoseOffsets_ShiftPartsIntegally()
    {
        Project project = CreateProjectWithStarterContent();
        CharacterEntity hero = project.Characters.Single(c => c.Id == "char.hero");
        var pose = new PoseDefinition
        {
            Id = "pose.head_down",
            Parts =
            {
                ["head"] = new PartPose(0, 2),
                ["face"] = new PartPose(0, 2),
                ["hair_front"] = new PartPose(0, 2),
                ["hair_back"] = new PartPose(0, 2),
            },
        };

        PixelBuffer baseline = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });
        PixelBuffer shifted = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Down", Pose = pose });

        // topknot hair column moved down by exactly 2 rows
        Assert.Equal(baseline[16, 0], shifted[16, 2]);
        Assert.Equal(baseline[16, 1], shifted[16, 3]);
        Assert.Equal(Rgba32.Transparent, baseline[16, 2] == Rgba32.Transparent ? baseline[16, 2] : Rgba32.Transparent);
    }

    [Fact]
    public void Compose_HiddenParts_RemovePartsButKeepEquipment()
    {
        Project project = CreateProjectWithStarterContent();
        CharacterEntity hero = project.Characters.Single(c => c.Id == "char.hero");
        var pose = new PoseDefinition { Id = "pose.hide_body" };
        foreach (PartNode part in project.Rigs[0].Parts)
        {
            pose.Parts[part.Id] = new PartPose(0, 0, Hidden: true);
        }

        PixelBuffer hidden = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Down", Pose = pose });

        // body parts gone (head area transparent), equipment (outer) still there
        Assert.Equal(Rgba32.Transparent, hidden[16, 8]); // head region
        PixelBuffer baseline = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });
        Assert.Equal(baseline[16, 28], hidden[16, 28]); // outer/armor region unchanged
    }

    [Fact]
    public void Compose_BrokenAssetReference_ThrowsCompositionException()
    {
        Project project = CreateProjectWithStarterContent();
        CharacterEntity hero = project.Characters.Single(c => c.Id == "char.hero");
        PartAppearance headAppearance = hero.AppearanceOf("head")!;
        headAppearance.ViewAssets.Clear();
        headAppearance.AssetRef = new AssetReference { AssetId = "missing.asset" };

        CompositionException ex = Assert.Throws<CompositionException>(
            () => _composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" }));
        Assert.Contains("missing.asset", ex.Message);
    }

    [Fact]
    public void StarterContent_ProjectRoundTrip_KeepsRigsAndCharacters()
    {
        Project project = CreateProjectWithStarterContent();
        _store.Save(project, _root);

        Project loaded = _store.Open(_root);

        RigDefinition rig = Assert.Single(loaded.Rigs);
        Assert.Equal("rig.humanoid.topdown4", rig.Id);
        Assert.Equal(10, rig.Parts.Count);
        Assert.Equal(16, rig.Anchors.Count);
        Assert.Equal(13, rig.EquipmentSlots.Count);
        CharacterEntity character = Assert.Single(loaded.Characters);
        Assert.Equal(9, character.Appearance.Count);
        Assert.Equal(6, character.Equipment.Count);
        Assert.Empty(loaded.Validate());
        Assert.Empty(_library.ValidateOnDisk(loaded, _root));
    }

    private void AssertPixelsEqual(string label, PixelBuffer expected, PixelBuffer actual)
    {
        Assert.Equal(expected.Width, actual.Width);
        Assert.Equal(expected.Height, actual.Height);
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

        _output.WriteLine($"{label}: {diffs.Count}+ diffs");
        Assert.True(diffs.Count == 0, $"{label}\n{string.Join("\n", diffs)}");
    }
}

/// <summary>Builds the legacy spec matching the starter hero's unequipped look.</summary>
internal static class LegacyStarterHelper
{
    public static LegacySpriteSpec NeutralLegacySpec()
    {
        var spec = new LegacySpriteSpec();
        foreach (string key in spec.Equipment.Keys.ToList())
        {
            spec.Equipment[key] = "Không";
        }

        return spec;
    }
}
