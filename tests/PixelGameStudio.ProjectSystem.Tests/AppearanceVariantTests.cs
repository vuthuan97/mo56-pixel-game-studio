using PixelGameStudio.Assets;
using PixelGameStudio.Assets.Composition;
using PixelGameStudio.Core.Primitives;
using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Character;
using PixelGameStudio.Domain.Templates;
using PixelGameStudio.ProjectSystem;
using PixelGameStudio.Rendering;
using PixelGameStudio.Rendering.Procedural;
using Xunit;
using Xunit.Abstractions;

namespace PixelGameStudio.ProjectSystem.Tests;

/// <summary>Phase 4 (completed properly): body/eye/mouth/hair variants + skin tone overrides.</summary>
public class AppearanceVariantTests : IDisposable
{
    private readonly string _root;
    private readonly ProjectStore _store = new();
    private readonly AssetLibraryService _library = new();
    private readonly RigSpriteComposer _composer = new(new AssetLibraryService());
    private readonly ITestOutputHelper _output;

    public AppearanceVariantTests(ITestOutputHelper output)
    {
        _output = output;
        _root = Path.Combine(Path.GetTempPath(), "pgs-tests", Guid.NewGuid().ToString("N"));
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
        CharacterEntity hero = project.Characters.Single(c => c.Id == "char.hero");
        return (project, hero);
    }

    [Fact]
    public void Factory_RegistersVariantAssets_PerPart()
    {
        (Project project, CharacterEntity hero) = Create();

        PartAppearance torso = hero.AppearanceOf("torso")!;
        Assert.Equal(3, torso.StateAssets["body"].Count);       // Tiêu chuẩn / Mảnh / Đậm
        Assert.Equal(4, torso.StateAssets["body"]["tieu-chuan"].ViewAssets.Count);

        PartAppearance face = hero.AppearanceOf("face")!;
        Assert.Equal(5, face.StateAssets["eye"].Count);
        Assert.Equal(3, face.StateAssets["mouth"].Count);
        Assert.Equal(5 * 3, face.StateAssets["face"].Count);

        PartAppearance hairFront = hero.AppearanceOf("hair_front")!;
        Assert.Equal(AppearanceCatalog.HairStyles.Length, hairFront.StateAssets["hair"].Count);
        Assert.Equal(AppearanceCatalog.HairStyles.Length, hero.AppearanceOf("hair_back")!.StateAssets["hair"].Count);
        Assert.Contains("khong-toc", hairFront.StateAssets["hair"].Keys);
        Assert.Contains("hui-cua", hairFront.StateAssets["hair"].Keys);

        Assert.Empty(project.Validate());
    }

    [Fact]
    public void BodyVariant_ChangesRenderedSilhouette()
    {
        (Project project, CharacterEntity hero) = Create();
        // bare-body comparison: with the default loadout the outer robe covers the torso
        hero.Equipment.Clear();

        PixelBuffer standard = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });
        Assert.True(AppearanceService.SetState(project, hero, "torso", "body", "manh"));
        PixelBuffer slim = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });

        // torso right edge x=20: skin in Tiêu chuẩn (x 11..20), outline in Mảnh
        // (body ends at x=19, the 1px outline wraps the narrower silhouette)
        Assert.Equal(new Rgba32(22, 16, 30, 255), slim[20, 28]);
        Assert.Equal(new Rgba32(255, 222, 200, 255), standard[20, 28]);
    }

    [Fact]
    public void HairStyle_ChangesHairSilhouette()
    {
        (Project project, CharacterEntity hero) = Create();

        PixelBuffer bun = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });
        Assert.True(AppearanceService.SetState(project, hero, "hair_front", "hair", "toc-dai"));
        Assert.True(AppearanceService.SetState(project, hero, "hair_back", "hair", "toc-dai"));
        PixelBuffer longHair = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });

        // long hair strands reach lower than the bun
        Assert.Equal(Rgba32.Transparent, bun[6, 24]);
        Assert.NotEqual(Rgba32.Transparent, longHair[6, 24]);
    }

    [Fact]
    public void MaleAndBaldHairStyles_AreAvailableAndRender()
    {
        (Project project, CharacterEntity hero) = Create();
        PixelBuffer defaultHair = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });

        Assert.True(AppearanceService.SetState(project, hero, "hair_front", "hair", "hui-cua"));
        Assert.True(AppearanceService.SetState(project, hero, "hair_back", "hair", "hui-cua"));
        PixelBuffer maleHair = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });
        Assert.NotEqual(defaultHair[16, 0], maleHair[16, 0]);

        Assert.True(AppearanceService.SetState(project, hero, "hair_front", "hair", "khong-toc"));
        Assert.True(AppearanceService.SetState(project, hero, "hair_back", "hair", "khong-toc"));
        PixelBuffer bald = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });
        Assert.Equal(Rgba32.Transparent, bald[16, 0]);
    }

    [Fact]
    public void MouthStyle_ChangesRenderedFace()
    {
        (Project project, CharacterEntity hero) = Create();
        PixelBuffer neutral = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });

        Assert.True(AppearanceService.SetState(project, hero, "face", "mouth", "cuoi"));
        PixelBuffer smiling = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });

        int differentPixels = 0;
        for (int y = 0; y < neutral.Height; y++)
        {
            for (int x = 0; x < neutral.Width; x++)
            {
                if (!neutral[x, y].SameColor(smiling[x, y]))
                {
                    differentPixels++;
                }
            }
        }

        Assert.True(differentPixels > 0, "Changing mouth style must change the composed face.");
    }

    [Fact]
    public void ReferenceGridStyle_UsesSelectiveOutlineAndKeepsHairAndMouthDistinct()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Style.CharacterRenderer = "ReferenceGrid";
        project.Style.OutlineStyle = "Selective1px";
        _store.Save(project, _root);
        StarterContentFactory.CreateBaseBody(project, _root, _library);
        CharacterEntity hero = project.Characters.Single(c => c.Id == "char.hero");

        PixelBuffer baseImage = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });
        Assert.True(CountOpaque(baseImage) > 100, "Reference grid should produce a visible character.");

        PixelBuffer upWithHair = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Up" });
        Assert.True(AppearanceService.SetState(project, hero, "hair_front", "hair", "khong-toc"));
        Assert.True(AppearanceService.SetState(project, hero, "hair_back", "hair", "khong-toc"));
        PixelBuffer upBald = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Up" });
        Assert.NotEqual(upWithHair[16, 23], upBald[16, 23]);

        PixelBuffer bald = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });
        Assert.True(CountOpaque(bald) < CountOpaque(baseImage), "Không tóc must remove hair pixels.");

        Assert.True(AppearanceService.SetState(project, hero, "face", "mouth", "cuoi"));
        PixelBuffer smiling = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });
        Assert.True(CountDifferent(bald, smiling) > 0, "Reference mouth state must affect the composed face.");
    }

    [Fact]
    public void SkinOverride_RemapstoSelectedTone_AndResetWorks()
    {
        (Project project, CharacterEntity hero) = Create();
        PixelBuffer baseline = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });

        (Rgba32 baseFrom, Rgba32 shadowFrom) = LegacyCatalog.SkinTones["Sáng"];
        (Rgba32 baseTo, Rgba32 shadowTo) = LegacyCatalog.SkinTones["Ngăm"];
        AppearanceService.ApplySkinOverride(project, hero,
        [
            new PaletteMapping(baseFrom.ToHex(), baseTo.ToHex()),
            new PaletteMapping(shadowFrom.ToHex(), shadowTo.ToHex()),
        ]);

        PixelBuffer darker = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });
        Assert.Equal(baseTo, darker[16, 15]); // face skin pixel remapped
        Assert.Equal(190, darker[16, 15].R);

        AppearanceService.ApplySkinOverride(project, hero, []); // reset
        PixelBuffer reset = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });
        Assert.Equal(baseline[16, 15], reset[16, 15]);
    }

    private static int CountOpaque(PixelBuffer buffer)
    {
        int count = 0;
        for (int y = 0; y < buffer.Height; y++)
        {
            for (int x = 0; x < buffer.Width; x++)
            {
                if (buffer[x, y].A > 0) count++;
            }
        }

        return count;
    }

    private static int CountDifferent(PixelBuffer first, PixelBuffer second)
    {
        int count = 0;
        for (int y = 0; y < first.Height; y++)
        {
            for (int x = 0; x < first.Width; x++)
            {
                if (!first[x, y].SameColor(second[x, y])) count++;
            }
        }

        return count;
    }

    [Fact]
    public void DefaultVariants_PreserveLegacyParity()
    {
        // Default state values point at variants rendered identically to the base
        // assets, so the golden parity against the legacy renderer still holds.
        (Project project, CharacterEntity hero) = Create();

        PixelBuffer composed = _composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });
        var legacySpec = new LegacySpriteSpec();
        PixelBuffer legacy = new LegacySpriteRenderer().Render(legacySpec, LegacyPosePresets.Poses["idle_0"]);

        for (int y = 0; y < legacy.Height; y++)
        {
            for (int x = 0; x < legacy.Width; x++)
            {
                Assert.True(legacy[x, y].SameColor(composed[x, y]), $"({x},{y}) legacy={legacy[x, y]} composed={composed[x, y]}");
            }
        }
    }
}
