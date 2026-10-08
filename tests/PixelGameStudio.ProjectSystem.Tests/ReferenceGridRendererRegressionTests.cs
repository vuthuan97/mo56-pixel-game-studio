using PixelGameStudio.App.Rendering;
using PixelGameStudio.Assets;
using PixelGameStudio.Assets.Composition;
using PixelGameStudio.Core.Primitives;
using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Assets;
using PixelGameStudio.Domain.Character;
using PixelGameStudio.Domain.Templates;
using PixelGameStudio.Rendering;
using PixelGameStudio.Rendering.Procedural;
using Xunit;

namespace PixelGameStudio.ProjectSystem.Tests;

public sealed class ReferenceGridRendererRegressionTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "pgs-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void NullDirectionFromCreateFlowFallsBackToDown()
    {
        var spec = new LegacySpriteSpec { Direction = null! };
        Assert.Equal("Down", spec.Direction);
        var layers = new ReferenceGridSpriteRenderer().RenderLayers(spec, LegacyPosePresets.Poses["idle_0"]);
        Assert.NotEmpty(layers);
        Assert.All(layers, layer =>
        {
            Assert.Equal(32, layer.Image.Width);
            Assert.Equal(46, layer.Image.Height);
        });
    }

    [Fact]
    public void LegsAreIndependent_AndMovingOneDoesNotRestoreItsOldPixels()
    {
        var renderer = new ReferenceGridSpriteRenderer();
        var spec = new LegacySpriteSpec();
        LegacyPose neutral = LegacyPosePresets.Poses["idle_0"];
        LegacyPose leftForward = neutral with { LeftLeg = "forward" };

        PixelBuffer rightNeutral = Layer(renderer.RenderLayers(spec, neutral), "right_leg");
        PixelBuffer rightWhenLeftMoves = Layer(renderer.RenderLayers(spec, leftForward), "right_leg");
        AssertPixelsEqual(rightNeutral, rightWhenLeftMoves);

        PixelBuffer leftNeutral = Layer(renderer.RenderLayers(spec, neutral), "left_leg");
        PixelBuffer leftMoved = Layer(renderer.RenderLayers(spec, leftForward), "left_leg");
        Assert.True(CountDifferent(leftNeutral, leftMoved) > 0);
        Assert.DoesNotContain(Enumerable.Range(0, leftMoved.Width).SelectMany(x =>
            Enumerable.Range(0, leftMoved.Height).Select(y => (x, y))), point =>
            point.x >= 16 && leftMoved[point.x, point.y].A > 0);
    }

    [Fact]
    public void BodyVariantsDirectionsAndUpFaceProduceDifferentArt()
    {
        var renderer = new ReferenceGridSpriteRenderer();
        var standard = new LegacySpriteSpec { BodyType = "Tiêu chuẩn", Direction = "Down" };
        var slim = new LegacySpriteSpec { BodyType = "Mảnh", Direction = "Down" };
        PixelBuffer standardBody = Layer(renderer.RenderLayers(standard, LegacyPosePresets.Poses["idle_0"]), "body");
        PixelBuffer slimBody = Layer(renderer.RenderLayers(slim, LegacyPosePresets.Poses["idle_0"]), "body");
        Assert.True(CountDifferent(standardBody, slimBody) > 0);

        PixelBuffer upFace = Layer(renderer.RenderLayers(new LegacySpriteSpec { Direction = "Up" },
            LegacyPosePresets.Poses["idle_0"]), "face");
        Assert.Equal(0, CountOpaque(upFace));

        PixelBuffer left = Layer(renderer.RenderLayers(new LegacySpriteSpec { Direction = "Left" },
            LegacyPosePresets.Poses["idle_0"]), "head");
        PixelBuffer right = Layer(renderer.RenderLayers(new LegacySpriteSpec { Direction = "Right" },
            LegacyPosePresets.Poses["idle_0"]), "head");
        Assert.True(CountDifferent(left, right) > 0);
    }

    [Fact]
    public void RendererUsesConfiguredCanvasForNonClassicTemplates()
    {
        var renderer = new ReferenceGridSpriteRenderer();
        var spec = new LegacySpriteSpec { CanvasWidth = 48, CanvasHeight = 32, Direction = "Down" };
        foreach ((string _, PixelBuffer image) in renderer.RenderLayers(spec, LegacyPosePresets.Poses["idle_0"]))
        {
            Assert.Equal(48, image.Width);
            Assert.Equal(32, image.Height);
        }
    }

    [Fact]
    public void StarterVersionIsCommittedOnlyAfterGeneration_AndAssetsCarryProvenance()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Style.CharacterRenderer = "ReferenceGrid";
        project.Style.CharacterRendererVersion = 0;
        var store = new ProjectStore();
        store.Save(project, _root);
        var library = new AssetLibraryService();

        StarterContentFactory.CreateBaseBody(project, _root, library, generateFiles: false);
        Assert.Equal(0, project.Style.CharacterRendererVersion);

        StarterContentFactory.CreateBaseBody(project, _root, library);
        Assert.Equal(ReferenceGridSpriteRenderer.CurrentVersion, project.Style.CharacterRendererVersion);
        Assert.NotEmpty(project.Assets);
        Assert.All(project.Assets, asset =>
            Assert.Contains("source:starter-template", asset.Tags, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void RefreshDoesNotOverwriteAnImportedOrManuallyEditedAsset()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Style.CharacterRenderer = "ReferenceGrid";
        new ProjectStore().Save(project, _root);
        var library = new AssetLibraryService();
        StarterContentFactory.CreateBaseBody(project, _root, library);

        AssetDefinition manual = project.Assets.Single(a => a.Id == "part.torso.down");
        manual.Tags.RemoveAll(tag => tag.Equals("source:starter-template", StringComparison.OrdinalIgnoreCase));
        manual.Notes = "Edited by user";
        var custom = new PixelBuffer(32, 46);
        custom[16, 28] = new Rgba32(255, 0, 0, 255);
        using (Stream output = File.Create(Path.Combine(_root, "assets", manual.File)))
        {
            PngCodec.Encode(custom, output);
        }

        project.Style.CharacterRendererVersion = 0;
        StarterContentFactory.CreateBaseBody(project, _root, library);

        PixelBuffer preserved = library.LoadPixels(_root, manual);
        Assert.Equal(new Rgba32(255, 0, 0, 255), preserved[16, 28]);
        Assert.Equal(0, project.Style.CharacterRendererVersion);
    }

    [Fact]
    public void PreviewPackingKeepsRedGreenBlueChannelsAndPremultipliesAlpha()
    {
        Assert.Equal(0xFFFF0000u, PixelBufferBitmapExtensions.PackPremultipliedBgra(new Rgba32(255, 0, 0, 255)));
        Assert.Equal(0xFF00FF00u, PixelBufferBitmapExtensions.PackPremultipliedBgra(new Rgba32(0, 255, 0, 255)));
        Assert.Equal(0xFF0000FFu, PixelBufferBitmapExtensions.PackPremultipliedBgra(new Rgba32(0, 0, 255, 255)));
        Assert.Equal(0x80800000u, PixelBufferBitmapExtensions.PackPremultipliedBgra(new Rgba32(255, 0, 0, 128)));
    }

    [Fact]
    public void BuildParametersChangeComposedSilhouette()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Style.CharacterRenderer = "ReferenceGrid";
        new ProjectStore().Save(project, _root);
        StarterContentFactory.CreateBaseBody(project, _root, new AssetLibraryService());
        var hero = project.Characters.Single(c => c.Id == "char.hero");
        hero.Equipment.Clear();
        var composer = new RigSpriteComposer(new AssetLibraryService());
        PixelBuffer baseline = composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });

        hero.Build = new CharacterBuildProfile
        {
            BodyType = "Broad",
            TorsoHeightPx = 20,
            ArmLengthPx = 16,
            LegLengthPx = 20,
            FootWidthPx = 8,
        };
        PixelBuffer changed = composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });
        Assert.True(CountDifferent(baseline, changed) > 0);
    }

    [Fact]
    public void GenderTemplateChangesComposedSilhouetteAndRoundTrips()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Style.CharacterRenderer = "ReferenceGrid";
        new ProjectStore().Save(project, _root);
        StarterContentFactory.CreateBaseBody(project, _root, new AssetLibraryService());
        var hero = project.Characters.Single(c => c.Id == "char.hero");
        hero.Equipment.Clear();
        var composer = new RigSpriteComposer(new AssetLibraryService());
        hero.Build.Gender = "Female";
        PixelBuffer female = composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });
        hero.Build.Gender = "Male";
        PixelBuffer male = composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });
        Assert.True(CountDifferent(female, male) > 0);

        new ProjectStore().Save(project, _root);
        Project restored = new ProjectStore().Open(_root);
        Assert.Equal("Male", restored.Characters.Single().Build.Gender);
    }

    private static PixelBuffer Layer(List<(string Name, PixelBuffer Image)> layers, string name) =>
        layers.Single(x => x.Name == name).Image;

    private static int CountOpaque(PixelBuffer buffer)
    {
        int count = 0;
        for (int y = 0; y < buffer.Height; y++)
        {
            for (int x = 0; x < buffer.Width; x++)
            {
                if (buffer[x, y].A > 0)
                {
                    count++;
                }
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
                if (!first[x, y].SameColor(second[x, y]))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static void AssertPixelsEqual(PixelBuffer first, PixelBuffer second)
    {
        Assert.Equal(first.Width, second.Width);
        Assert.Equal(first.Height, second.Height);
        for (int y = 0; y < first.Height; y++)
        {
            for (int x = 0; x < first.Width; x++)
            {
                Assert.True(first[x, y].SameColor(second[x, y]), $"Different pixel ({x},{y}).");
            }
        }
    }
}
