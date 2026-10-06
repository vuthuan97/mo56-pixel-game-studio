using PixelGameStudio.Core.Primitives;
using PixelGameStudio.Assets;
using PixelGameStudio.Assets.Composition;
using PixelGameStudio.Behaviors;
using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Character;
using PixelGameStudio.Domain.Templates;
using PixelGameStudio.Export;
using PixelGameStudio.ProjectSystem;
using PixelGameStudio.Rendering;
using PixelGameStudio.Validation;
using Xunit;

namespace PixelGameStudio.ProjectSystem.Tests;

/// <summary>Phase 10 (QA extensions) + Phase 12 (layers, package, Godot).</summary>
public class QaAndExportTests : IDisposable
{
    private readonly string _root;
    private readonly ProjectStore _store = new();
    private readonly AssetLibraryService _library = new();
    private readonly RigSpriteComposer _composer = new(new AssetLibraryService());

    public QaAndExportTests()
    {
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
        new BehaviorLibraryService().InstallPresets(project);
        CharacterEntity hero = project.Characters.Single(c => c.Id == "char.hero");
        return (project, hero);
    }

    [Theory]
    [InlineData(0.0, false)]
    [InlineData(1.0, true)]
    public void PixelNoise_SolidVsScattered(double expectedMinOrExact, bool scattered)
    {
        var buffer = scattered
            ? new PixelBuffer(4, 4)
            : new PixelBuffer(4, 4);
        if (scattered)
        {
            // checkerboard of single pixels — every opaque pixel isolated
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    if ((x + y) % 2 == 0)
                    {
                        buffer[x, y] = new Rgba32(255, 0, 0, 255);
                    }
                }
            }
        }
        else
        {
            for (int y = 1; y < 3; y++)
            {
                for (int x = 1; x < 3; x++)
                {
                    buffer[x, y] = new Rgba32(255, 0, 0, 255);
                }
            }
        }

        double noise = CharacterQaService.MeasurePixelNoise(buffer);
        if (scattered)
        {
            Assert.True(noise >= expectedMinOrExact - 0.01, $"noise={noise}");
        }
        else
        {
            Assert.Equal(expectedMinOrExact, noise, 2);
        }
    }

    [Fact]
    public void CheckCharacter_FlagsMissingFaceUpView_AndCleanOtherwise()
    {
        (Project project, CharacterEntity hero) = Create();

        var issues = CharacterQaService.CheckCharacter(project, _root, hero, _library, _composer);

        // starter face art legitimately lacks the Up view — QA surfaces it
        Assert.Contains(issues, i => i.Contains("'face' thiếu art cho view 'Up'"));
        Assert.DoesNotContain(issues, i => i.Contains("không compose được"));
    }

    [Fact]
    public void ExportCharacter_LayersPackageGodot_AllWritten()
    {
        (Project project, CharacterEntity hero) = Create();
        var service = new ExportService(_composer);
        string outDir = Path.Combine(_root, "export");

        service.ExportCharacter(project, _root, hero, new CharacterExportOptions
        {
            Views = ["Down"],
            AnimationId = "walk",
            IncludeFrames = true,
            IncludeSpritesheet = true,
            IncludeLayers = true,
            IncludeGodot = true,
        }, outDir);

        // layers: 4 frames × per-part layer PNGs
        string layerDir = Path.Combine(outDir, "layers", "down", "down_00");
        Assert.True(Directory.Exists(layerDir));
        string[] layers = Directory.GetFiles(layerDir, "*.png");
        Assert.Contains(layers, f => f.EndsWith("torso.png"));
        Assert.Contains(layers, f => f.EndsWith("slot.main_hand.png"));

        // package.json carries character + animation + markers
        string package = File.ReadAllText(Path.Combine(outDir, "package.json"));
        Assert.Contains("char.hero", package);
        Assert.Contains("walk", package);
        Assert.Contains("markers", package);

        // godot integration
        string tres = File.ReadAllText(Path.Combine(outDir, "godot", "sprite_frames.tres"));
        Assert.Contains("load_steps=5", tres); // 1 view x 4 frames + resource
        Assert.Contains("&\"walk_down\"", tres);
        Assert.True(File.Exists(Path.Combine(outDir, "godot", "character_sprite_example.gd")));
        Assert.True(File.Exists(Path.Combine(outDir, "godot", "README_GODOT.md")));
    }
}
