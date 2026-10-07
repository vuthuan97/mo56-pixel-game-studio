using PixelGameStudio.Assets;
using PixelGameStudio.Assets.Composition;
using PixelGameStudio.Behaviors;
using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Animation;
using PixelGameStudio.Domain.Character;
using PixelGameStudio.Domain.Templates;
using PixelGameStudio.Export;
using System.Text.Json.Nodes;
using PixelGameStudio.ProjectSystem;
using PixelGameStudio.Rendering;
using Xunit;

namespace PixelGameStudio.ProjectSystem.Tests;

public sealed class LayoutActionsUpgradeTests : IDisposable
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
    public void ActionGenerator_CreatesDedicatedPosesAndAnimation()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Rigs.Add(RigTemplates.HumanoidTopDown4());
        CharacterEntity character = new() { Id = "char.hero", Name = "Hero", RigId = RigTemplates.HumanoidTopDownId };
        project.Characters.Add(character);

        AnimationDefinition animation = ActionTemplateCatalog.Generate(project, character, "jump");

        Assert.Equal("action.jump", animation.Id);
        Assert.Equal(3, animation.Frames.Count);
        Assert.All(animation.Frames, frame => Assert.StartsWith("action.jump.", frame.PoseId));
        Assert.Equal(3, project.Poses.Count(pose => pose.Id.StartsWith("action.jump.", StringComparison.Ordinal)));
        Assert.Contains("jump", character.ActionIds);
    }

    [Fact]
    public void ActionCatalog_ReportsMissingEquipmentInsteadOfGeneratingUnsupportedAction()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Rigs.Add(RigTemplates.HumanoidTopDown4());
        CharacterEntity character = new() { Id = "char.hero", Name = "Hero", RigId = RigTemplates.HumanoidTopDownId };
        project.Characters.Add(character);

        ActionAvailability block = ActionTemplateCatalog.Evaluate(project, character)
            .Single(item => item.Template.Id == "block");

        Assert.False(block.IsAvailable);
        Assert.Contains("off_hand", block.Reason, StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => ActionTemplateCatalog.Generate(project, character, "block"));
    }

    [Fact]
    public void ActionGenerator_CancellationIsAtomic()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Rigs.Add(RigTemplates.HumanoidTopDown4());
        CharacterEntity character = new() { Id = "char.hero", Name = "Hero", RigId = RigTemplates.HumanoidTopDownId };
        project.Characters.Add(character);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => ActionTemplateCatalog.Generate(
            project, character, "jump", cancellation.Token));
        Assert.DoesNotContain(project.Animations, animation => animation.Id == "action.jump");
        Assert.DoesNotContain(project.Poses, pose => pose.Id.StartsWith("action.jump.", StringComparison.Ordinal));
        Assert.Empty(character.ActionIds);
    }

    [Fact]
    public void CharacterBuildAndAppearanceAreIsolatedPerCharacter()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Style.CharacterRenderer = "ReferenceGrid";
        new ProjectStore().Save(project, _root);
        StarterContentFactory.CreateBaseBody(project, _root, new AssetLibraryService());
        CharacterEntity source = project.Characters.Single();
        CharacterEntity copy = new()
        {
            Id = "char.copy",
            Name = "Copy",
            RigId = source.RigId,
            Appearance = source.Appearance.Select(CloneAppearance).ToList(),
        };
        project.Characters.Add(copy);

        PixelBuffer before = new RigSpriteComposer(new AssetLibraryService()).Compose(
            project, _root, copy, new CompositionOptions { View = "Down" });

        source.Build.TorsoHeightPx = 20;
        source.Build.Normalize();
        AppearanceService.SetState(project, source, "face", "mouth", "cuoi");

        PixelBuffer unchanged = new RigSpriteComposer(new AssetLibraryService()).Compose(
            project, _root, copy, new CompositionOptions { View = "Down" });

        Assert.Equal(before.Width, unchanged.Width);
        Assert.Equal(before.Height, unchanged.Height);
        Assert.Equal(0, CountDifferent(before, unchanged));
        Assert.NotEqual("cuoi", copy.AppearanceOf("face")!.States["mouth"]);

        string sourceOut = Path.Combine(_root, "export-source");
        string copyOut = Path.Combine(_root, "export-copy");
        var exporter = new ExportService(new RigSpriteComposer(new AssetLibraryService()));
        exporter.ExportCharacter(project, _root, source, new CharacterExportOptions { Views = ["Down"] }, sourceOut);
        exporter.ExportCharacter(project, _root, copy, new CharacterExportOptions { Views = ["Down"] }, copyOut);
        Assert.Contains("char.hero", File.ReadAllText(Path.Combine(sourceOut, "package.json")));
        Assert.Contains("char.copy", File.ReadAllText(Path.Combine(copyOut, "package.json")));
    }

    [Fact]
    public void CharacterBuildAndGeneratedActions_RoundTripThroughProjectStore()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Rigs.Add(RigTemplates.HumanoidTopDown4());
        CharacterEntity character = new() { Id = "char.hero", Name = "Hero", RigId = RigTemplates.HumanoidTopDownId };
        project.Characters.Add(character);
        character.Build.Gender = "Male";
        character.Build.LegLengthPx = 20;
        ActionTemplateCatalog.Generate(project, character, "jump");

        var store = new ProjectStore();
        store.Save(project, _root);
        Project loaded = store.Open(_root);
        CharacterEntity restored = Assert.Single(loaded.Characters);

        Assert.Equal("Male", restored.Build.Gender);
        Assert.Equal(20, restored.Build.LegLengthPx);
        Assert.Contains("jump", restored.ActionIds);
        Assert.Contains(loaded.Animations, animation => animation.Id == "action.jump");
    }

    [Fact]
    public void LegacyCharacterWithoutBuildFields_MigratesToSafeDefaults()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Rigs.Add(RigTemplates.HumanoidTopDown4());
        project.Characters.Add(new CharacterEntity
        {
            Id = "char.legacy",
            Name = "Legacy",
            RigId = RigTemplates.HumanoidTopDownId,
        });
        var store = new ProjectStore();
        store.Save(project, _root);

        JsonNode json = JsonNode.Parse(store.Serialize(project))!;
        JsonObject legacyCharacter = json["characters"]![0]!.AsObject();
        legacyCharacter.Remove("build");
        legacyCharacter.Remove("actionIds");
        File.WriteAllText(ProjectPaths.ProjectFile(_root), json.ToJsonString(ProjectStore.JsonOptions));

        CharacterEntity restored = Assert.Single(store.Open(_root).Characters);
        Assert.Equal(CharacterBuildProfile.DefaultHeadHeightPx, restored.Build.HeadHeightPx);
        Assert.Equal("Unspecified", restored.Build.Gender);
        Assert.Empty(restored.ActionIds);
    }

    private static PartAppearance CloneAppearance(PartAppearance source) => new()
    {
        PartId = source.PartId,
        AssetRef = source.AssetRef,
        ViewAssets = new(source.ViewAssets, StringComparer.OrdinalIgnoreCase),
        ViewMirrors = [.. source.ViewMirrors],
        PaletteOverride = [.. source.PaletteOverride],
        States = new(source.States, StringComparer.OrdinalIgnoreCase),
        StateAssets = source.StateAssets.ToDictionary(
            pair => pair.Key,
            pair => new Dictionary<string, StateAssetVariant>(pair.Value, StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase),
    };

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
}
