using PixelGameStudio.Assets;
using PixelGameStudio.Assets.Composition;
using PixelGameStudio.App.Rendering;
using PixelGameStudio.Behaviors;
using PixelGameStudio.Core.Primitives;
using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Animation;
using PixelGameStudio.Domain.Behaviors;
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

    [Fact]
    public void PreviewBackdrop_ChangesDisplayOnly_AndPreservesSourceAlpha()
    {
        var source = new PixelBuffer(16, 8);
        source[0, 0] = new Rgba32(230, 20, 30, 255);
        PixelBuffer checker = PreviewBackdrop.Apply(source, "Ô caro");
        PixelBuffer light = PreviewBackdrop.Apply(source, "Sáng");
        PixelBuffer dark = PreviewBackdrop.Apply(source, "Tối");

        Assert.Equal(source[0, 0], checker[0, 0]);
        Assert.NotEqual(checker[1, 0], checker[9, 0]);
        Assert.NotEqual(light[1, 0], dark[1, 0]);
        Assert.Equal((byte)255, checker[1, 0].A);
        Assert.Equal((byte)0, source[1, 0].A);
    }

    [Fact]
    public void ActionCatalog_ProvidesRealMotionFamilies_AndReportsCompatibility()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        new ProjectStore().Save(project, _root);
        StarterContentFactory.CreateHumanoidDemo(project, _root, new AssetLibraryService());
        CharacterEntity character = Assert.Single(project.Characters);
        IReadOnlyList<ActionTemplateDefinition> templates = ActionTemplateCatalog.All;
        Assert.Contains(templates, template => template.Id == "run" && template.Frames.Count > 1);
        Assert.Contains(templates, template => template.Id == "slash" && template.RequiredSlotId == "main_hand");
        Assert.Contains(templates, template => template.Id == "pick_up" && template.RequiredAnchorId == "anchor.interact");
        Assert.Contains(templates, template => template.Id == "eat" && template.Group == "Daily");

        foreach (string templateId in new[] { "idle", "walk", "run", "sit", "lie", "punch", "slash", "eat", "pick_up" })
        {
            ActionAvailability availability = ActionTemplateCatalog.Evaluate(project, character)
                .Single(item => item.Template.Id == templateId);
            if (availability.IsAvailable)
            {
                AnimationDefinition output = ActionTemplateCatalog.Generate(project, character, templateId);
                Assert.True(output.Frames.Count > 0);
                Assert.All(output.Frames, frame => Assert.StartsWith(output.Id + ".", frame.PoseId));
            }
            else
            {
                Assert.False(string.IsNullOrWhiteSpace(availability.Reason));
            }
        }

        Assert.NotEqual(
            project.Animations.Single(animation => animation.Id == CharacterActionIds.Animation(character.Id, "walk")).Frames[0].PoseId,
            project.Animations.Single(animation => animation.Id == CharacterActionIds.Animation(character.Id, "run")).Frames[0].PoseId);

        var generatedIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (ActionAvailability item in ActionTemplateCatalog.Evaluate(project, character))
        {
            if (!item.IsAvailable) continue;
            AnimationDefinition animation = ActionTemplateCatalog.Generate(project, character, item.Template.Id);
            Assert.True(generatedIds.Add(animation.Id));
            Assert.NotEmpty(animation.Frames);
        }
        Assert.All(new[] { "Movement", "Adventure", "Combat", "Daily", "Interaction" }, group =>
            Assert.Contains(ActionTemplateCatalog.All, template => template.Group == group));
    }

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

        Assert.Equal(CharacterActionIds.Animation(character.Id, "jump"), animation.Id);
        Assert.Equal(3, animation.Frames.Count);
        Assert.All(animation.Frames, frame => Assert.StartsWith(animation.Id + ".", frame.PoseId));
        Assert.Equal(3, project.Poses.Count(pose => pose.Id.StartsWith(animation.Id + ".", StringComparison.Ordinal)));
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
        Assert.DoesNotContain(project.Animations, animation => animation.Id == CharacterActionIds.Animation(character.Id, "jump"));
        Assert.DoesNotContain(project.Poses, pose => pose.Id.StartsWith(CharacterActionIds.Animation(character.Id, "jump") + ".", StringComparison.Ordinal));
        Assert.Empty(character.ActionIds);
    }

    [Fact]
    public void ActionGenerator_IsDeterministic_AndDoesNotMutateSharedTemplate()
    {
        static (Project Project, CharacterEntity Character) Create()
        {
            Project project = ProjectTemplates.ClassicTopDown46();
            project.Rigs.Add(RigTemplates.HumanoidTopDown4());
            CharacterEntity character = new()
            {
                Id = "char.stable",
                Name = "Stable",
                RigId = RigTemplates.HumanoidTopDownId,
            };
            project.Characters.Add(character);
            return (project, character);
        }

        (Project firstProject, CharacterEntity firstCharacter) = Create();
        (Project secondProject, CharacterEntity secondCharacter) = Create();
        AnimationDefinition first = ActionTemplateCatalog.Generate(firstProject, firstCharacter, "jump");
        AnimationDefinition second = ActionTemplateCatalog.Generate(secondProject, secondCharacter, "jump");
        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.Frames.Select(frame => frame.PoseId), second.Frames.Select(frame => frame.PoseId));
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(firstProject.Poses[0], ProjectStore.JsonOptions),
            System.Text.Json.JsonSerializer.Serialize(secondProject.Poses[0], ProjectStore.JsonOptions));

        PoseDefinition generatedPose = firstProject.Poses[0];
        generatedPose.Parts["head"] = generatedPose.Parts["head"] with { OffsetY = 7 };
        PoseDefinition freshPreview = ActionTemplateCatalog.CreatePreviewPose("jump");
        Assert.NotEqual(7, freshPreview.Parts["head"].OffsetY);
        Assert.NotEqual(7, secondProject.Poses[0].Parts["head"].OffsetY);
    }

    [Fact]
    public void Regenerate_PreservesHandEditsByDefault_AndNewVersionUpdatesBinding()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Rigs.Add(RigTemplates.HumanoidTopDown4());
        CharacterEntity character = new() { Id = "char.regen", Name = "Regen", RigId = RigTemplates.HumanoidTopDownId };
        project.Characters.Add(character);
        AnimationDefinition original = ActionTemplateCatalog.Generate(project, character, "jump");
        PoseDefinition edited = project.Poses.Single(pose => pose.Id == original.Frames[0].PoseId);
        edited.Parts["head"] = edited.Parts["head"] with { OffsetY = 7 };
        string originalFingerprint = Assert.Single(character.GeneratedActionBindings).SourceFingerprint;

        character.Build.ArmLengthPx = 17;
        Assert.Contains("Cần tạo lại", ActionTemplateCatalog.Evaluate(project, character)
            .Single(item => item.Template.Id == "jump").Reason);
        Assert.Same(original, ActionTemplateCatalog.Generate(project, character, "jump"));
        Assert.Equal(7, edited.Parts["head"].OffsetY);

        AnimationDefinition version = ActionTemplateCatalog.Generate(project, character, "jump",
            mode: ActionRegenerationMode.CreateNewVersion);
        Assert.EndsWith(".v2", version.Id);
        Assert.Equal(7, edited.Parts["head"].OffsetY);
        Assert.NotEqual(original.Frames[0].PoseId, version.Frames[0].PoseId);
        GeneratedActionBinding binding = Assert.Single(character.GeneratedActionBindings);
        Assert.Equal(version.Id, binding.AnimationId);
        Assert.NotEqual(originalFingerprint, binding.SourceFingerprint);
        Assert.Contains("nguồn còn khớp", ActionTemplateCatalog.Evaluate(project, character)
            .Single(item => item.Template.Id == "jump").Reason);

        var store = new ProjectStore();
        store.Save(project, _root);
        GeneratedActionBinding restored = Assert.Single(Assert.Single(store.Open(_root).Characters).GeneratedActionBindings);
        Assert.Equal(binding.AnimationId, restored.AnimationId);
        Assert.Equal(binding.SourceFingerprint, restored.SourceFingerprint);
    }

    [Fact]
    public void ReplaceAction_IsExplicitAndAtomic_AndRejectsSharedPose()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Rigs.Add(RigTemplates.HumanoidTopDown4());
        CharacterEntity character = new() { Id = "char.replace", Name = "Replace", RigId = RigTemplates.HumanoidTopDownId };
        project.Characters.Add(character);
        AnimationDefinition original = ActionTemplateCatalog.Generate(project, character, "jump");
        PoseDefinition edited = project.Poses.Single(pose => pose.Id == original.Frames[0].PoseId);
        edited.Parts["head"] = edited.Parts["head"] with { OffsetY = 7 };
        AnimationDefinition shared = new() { Id = "shared.copy", DisplayName = "Shared" };
        shared.Frames.Add(new AnimationFrame { PoseId = edited.Id });
        project.Animations.Add(shared);

        Assert.Throws<InvalidOperationException>(() => ActionTemplateCatalog.Generate(project, character, "jump",
            mode: ActionRegenerationMode.Replace));
        Assert.Same(original, project.Animations.Single(animation => animation.Id == original.Id));
        Assert.Equal(7, project.Poses.Single(pose => pose.Id == edited.Id).Parts["head"].OffsetY);

        project.Animations.Remove(shared);
        AnimationDefinition replaced = ActionTemplateCatalog.Generate(project, character, "jump",
            mode: ActionRegenerationMode.Replace);
        Assert.Equal(original.Id, replaced.Id);
        Assert.NotSame(original, replaced);
        Assert.NotEqual(7, project.Poses.Single(pose => pose.Id == edited.Id).Parts["head"].OffsetY);
        Assert.Equal(3, project.Poses.Count(pose => pose.Id.StartsWith(original.Id + ".", StringComparison.Ordinal)));
    }

    [Fact]
    public void Regenerate_CancellationAndUndoRedo_RestorePreviousBindings()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Rigs.Add(RigTemplates.HumanoidTopDown4());
        CharacterEntity character = new() { Id = "char.history", Name = "History", RigId = RigTemplates.HumanoidTopDownId };
        project.Characters.Add(character);
        AnimationDefinition original = ActionTemplateCatalog.Generate(project, character, "jump");
        var history = new UndoRedoService(new ProjectStore());
        history.Attach(project);
        history.Checkpoint(project);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => ActionTemplateCatalog.Generate(project, character, "jump",
            cancellation.Token, mode: ActionRegenerationMode.CreateNewVersion));
        Assert.Single(project.Animations);
        Assert.Equal(original.Id, Assert.Single(character.GeneratedActionBindings).AnimationId);

        AnimationDefinition version = ActionTemplateCatalog.Generate(project, character, "jump",
            mode: ActionRegenerationMode.CreateNewVersion);
        Project undone = history.Undo(project)!;
        Assert.Single(undone.Animations);
        Assert.Equal(original.Id, Assert.Single(Assert.Single(undone.Characters).GeneratedActionBindings).AnimationId);
        Project redone = history.Redo(undone)!;
        Assert.Contains(redone.Animations, animation => animation.Id == version.Id);
        Assert.Equal(version.Id, Assert.Single(Assert.Single(redone.Characters).GeneratedActionBindings).AnimationId);
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
    public void CustomEquipmentSlot_RendersInFourViews_AndUndoIsolatesCharacters()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        new ProjectStore().Save(project, _root);
        StarterContentFactory.CreateHumanoidDemo(project, _root, new AssetLibraryService());
        RigDefinition rig = Assert.Single(project.Rigs);
        CharacterEntity first = Assert.Single(project.Characters);
        CharacterEntity second = new()
        {
            Id = "char.second",
            Name = "Second",
            RigId = first.RigId,
            Appearance = first.Appearance.Select(CloneAppearance).ToList(),
            Equipment = [.. first.Equipment],
        };
        project.Characters.Add(second);
        var composer = new RigSpriteComposer(new AssetLibraryService());
        string[] views = ["Down", "Up", "Left", "Right"];
        PixelBuffer[] before = views.Select(view => composer.Compose(project, _root, second,
            new CompositionOptions { View = view })).ToArray();
        var history = new UndoRedoService(new ProjectStore());
        history.Attach(project);
        history.Checkpoint(project);

        string anchorId = rig.Anchors.First(anchor => anchor.PartId == "torso").Id;
        var slot = new EquipmentSlotDef
        {
            Id = "custom.badge",
            DisplayName = "Custom badge",
            AnchorId = anchorId,
            ZIndex = 30,
        };
        rig.EquipmentSlots.Add(slot);
        string assetId = project.Assets.First(asset => asset.Id.StartsWith("eq.head.", StringComparison.Ordinal)
            && asset.Id.EndsWith(".down", StringComparison.Ordinal)).Id;
        string assetPrefix = assetId[..^"down".Length];
        var itemWithViews = new EquippedItem(slot.Id, assetId)
        {
            ViewAssets = views.ToDictionary(view => view,
                view => project.Assets.Single(asset => asset.Id == assetPrefix + view.ToLowerInvariant()).Id),
        };
        first.Equipment.Add(itemWithViews);
        Assert.NotNull(rig.FindSlot(slot.Id));
        Assert.Empty(rig.Validate());
        for (int i = 0; i < views.Length; i++)
        {
            PixelBuffer unchanged = composer.Compose(project, _root, second,
                new CompositionOptions { View = views[i] });
            Assert.Equal(0, CountDifferent(before[i], unchanged));
            PixelBuffer equipped = composer.Compose(project, _root, first,
                new CompositionOptions { View = views[i] });
            Assert.True(CountDifferent(unchanged, equipped) > 0);
        }

        Project undone = history.Undo(project)!;
        Assert.Null(undone.Rigs.Single().FindSlot(slot.Id));
        Assert.DoesNotContain(undone.Characters.Single(character => character.Id == first.Id).Equipment,
            item => item.SlotId == slot.Id);
        Project redone = history.Redo(undone)!;
        Assert.NotNull(redone.Rigs.Single().FindSlot(slot.Id));
        Assert.Contains(redone.Characters.Single(character => character.Id == first.Id).Equipment,
            item => item.SlotId == slot.Id);
        Assert.DoesNotContain(redone.Characters.Single(character => character.Id == second.Id).Equipment,
            item => item.SlotId == slot.Id);
    }

    [Fact]
    public void CustomAnimation_CreateDuplicateAndSaveTemplate_RoundTripWithoutSharedPoses()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Rigs.Add(RigTemplates.HumanoidTopDown4());
        CharacterEntity first = new() { Id = "char.one", Name = "One", RigId = RigTemplates.HumanoidTopDownId };
        CharacterEntity second = new() { Id = "char.two", Name = "Two", RigId = RigTemplates.HumanoidTopDownId };
        project.Characters.AddRange([first, second]);
        PoseDefinition seed = PoseDefinition.Identity("shared.seed");
        seed.Parts["head"] = new PartPose(1, 2);
        project.Poses.Add(seed);

        AnimationDefinition created = CharacterAnimationEditor.Create(project, first, seed);
        AnimationDefinition copy = CharacterAnimationEditor.Duplicate(project, second, created);
        Assert.NotEqual(created.Id, copy.Id);
        Assert.NotEqual(created.Frames[0].PoseId, copy.Frames[0].PoseId);
        project.Poses.Single(pose => pose.Id == copy.Frames[0].PoseId).Parts["head"] = new PartPose(5, 6);
        Assert.Equal((1, 2), (project.Poses.Single(pose => pose.Id == created.Frames[0].PoseId).Parts["head"].OffsetX,
            project.Poses.Single(pose => pose.Id == created.Frames[0].PoseId).Parts["head"].OffsetY));
        Assert.Equal((1, 2), (seed.Parts["head"].OffsetX, seed.Parts["head"].OffsetY));
        var template = CharacterAnimationEditor.SaveAsBehaviorTemplate(project, copy);
        Assert.Equal(copy.Id, template.AnimationId);
        Assert.Contains(ActionTemplateCatalog.Evaluate(project, first), item => item.Template.Id == template.Id && item.IsAvailable);
        AnimationDefinition generated = ActionTemplateCatalog.Generate(project, first, template.Id);
        Assert.NotEqual(copy.Id, generated.Id);
        Assert.NotEqual(copy.Frames[0].PoseId, generated.Frames[0].PoseId);
        Assert.Equal(5, project.Poses.Single(pose => pose.Id == generated.Frames[0].PoseId).Parts["head"].OffsetX);
        Assert.Equal(5, project.Poses.Single(pose => pose.Id == copy.Frames[0].PoseId).Parts["head"].OffsetX);
        project.Poses.Single(pose => pose.Id == copy.Frames[0].PoseId).Parts["head"] = new PartPose(7, 8);
        Assert.Equal(5, project.Poses.Single(pose => pose.Id == generated.Frames[0].PoseId).Parts["head"].OffsetX);
        Assert.Contains("Cần tạo lại", ActionTemplateCatalog.Evaluate(project, first)
            .Single(item => item.Template.Id == template.Id).Reason);
        project.Behaviors.Add(new BehaviorDefinition { Id = "behavior.custom.broken", DisplayName = "Broken" });
        Assert.False(ActionTemplateCatalog.Evaluate(project, first)
            .Single(item => item.Template.Id == "behavior.custom.broken").IsAvailable);

        new ProjectStore().Save(project, _root);
        Project loaded = new ProjectStore().Open(_root);
        Assert.Contains(loaded.Animations, animation => animation.Id == copy.Id);
        Assert.Contains(loaded.Behaviors, behavior => behavior.Id == template.Id && behavior.AnimationId == copy.Id);
        Assert.Equal(7, loaded.Poses.Single(pose => pose.Id == copy.Frames[0].PoseId).Parts["head"].OffsetX);
        Assert.Contains(loaded.Animations, animation => animation.Id == generated.Id);
        Assert.Equal(1, loaded.Poses.Single(pose => pose.Id == created.Frames[0].PoseId).Parts["head"].OffsetX);
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
        Assert.Contains(loaded.Animations, animation => animation.Id == CharacterActionIds.Animation(character.Id, "jump"));
    }

    [Fact]
    public void SameActionOnTwoCharacters_UsesDistinctPoses_AndRoundTrips()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Rigs.Add(RigTemplates.HumanoidTopDown4());
        CharacterEntity first = new() { Id = "char.first", Name = "First", RigId = RigTemplates.HumanoidTopDownId };
        CharacterEntity second = new() { Id = "char.second", Name = "Second", RigId = RigTemplates.HumanoidTopDownId };
        first.SelectedActionIds.Add("jump");
        second.SelectedActionIds.AddRange(["jump", "wave"]);
        project.Characters.AddRange([first, second]);

        AnimationDefinition firstAnimation = ActionTemplateCatalog.Generate(project, first, "jump");
        AnimationDefinition secondAnimation = ActionTemplateCatalog.Generate(project, second, "jump");
        ActionTemplateCatalog.Generate(project, second, "wave");
        Assert.NotEqual(firstAnimation.Id, secondAnimation.Id);
        Assert.Empty(firstAnimation.Frames.Select(frame => frame.PoseId)
            .Intersect(secondAnimation.Frames.Select(frame => frame.PoseId)));

        PoseDefinition firstPose = project.Poses.Single(pose => pose.Id == firstAnimation.Frames[0].PoseId);
        PoseDefinition secondPose = project.Poses.Single(pose => pose.Id == secondAnimation.Frames[0].PoseId);
        firstPose.Parts["head"] = firstPose.Parts["head"] with
        {
            OffsetY = firstPose.Parts["head"].OffsetY + 3,
        };
        Assert.NotEqual(firstPose.Parts["head"].OffsetY, secondPose.Parts["head"].OffsetY);

        var store = new ProjectStore();
        store.Save(project, _root);
        Project restored = store.Open(_root);
        Assert.Contains(restored.Animations, animation => animation.Id == firstAnimation.Id);
        Assert.Contains(restored.Animations, animation => animation.Id == secondAnimation.Id);
        Assert.Equal(["jump"], restored.Characters.Single(character => character.Id == first.Id).ActionIds);
        Assert.Equal(["jump", "wave"], restored.Characters.Single(character => character.Id == second.Id).ActionIds);
        Assert.Equal(["jump"], restored.Characters.Single(character => character.Id == first.Id).SelectedActionIds);
        Assert.Equal(["jump", "wave"], restored.Characters.Single(character => character.Id == second.Id).SelectedActionIds);
    }

    [Fact]
    public void LegacyGeneratedAction_RemainsReadableWithoutOverwritingIt()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Rigs.Add(RigTemplates.HumanoidTopDown4());
        CharacterEntity character = new() { Id = "char.legacy", Name = "Legacy", RigId = RigTemplates.HumanoidTopDownId };
        character.ActionIds.Add("jump");
        project.Characters.Add(character);
        AnimationDefinition legacy = new() { Id = "action.jump", DisplayName = "Hand edited", Fps = 7 };
        legacy.Frames.Add(new AnimationFrame { PoseId = "legacy.pose" });
        project.Animations.Add(legacy);

        Assert.Same(legacy, ActionTemplateCatalog.Generate(project, character, "jump"));
        Assert.Single(project.Animations);
        Assert.Equal("Hand edited", legacy.DisplayName);
    }

    [Fact]
    public void Export_RejectsAnotherCharactersGeneratedAction()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Style.CharacterRenderer = "ReferenceGrid";
        new ProjectStore().Save(project, _root);
        StarterContentFactory.CreateBaseBody(project, _root, new AssetLibraryService());
        CharacterEntity first = project.Characters.Single();
        CharacterEntity second = new()
        {
            Id = "char.second",
            Name = "Second",
            RigId = first.RigId,
        };
        project.Characters.Add(second);
        AnimationDefinition firstAnimation = ActionTemplateCatalog.Generate(project, first, "wave");
        AnimationDefinition secondAnimation = ActionTemplateCatalog.Generate(project, second, "wave");
        ExportService exporter = new(new RigSpriteComposer(new AssetLibraryService()));
        string wrongOutput = Path.Combine(_root, "wrong-character");

        Assert.Throws<InvalidOperationException>(() => exporter.ExportCharacter(
            project, _root, first,
            new CharacterExportOptions { AnimationId = secondAnimation.Id, Views = ["Down"] }, wrongOutput));
        Assert.False(Directory.Exists(wrongOutput));

        string correctOutput = Path.Combine(_root, "correct-character");
        exporter.ExportCharacter(project, _root, first,
            new CharacterExportOptions { AnimationId = firstAnimation.Id, Views = ["Down"] }, correctOutput);
        Assert.Contains(firstAnimation.Id, File.ReadAllText(Path.Combine(correctOutput, "package.json")));
        Assert.DoesNotContain(secondAnimation.Id, File.ReadAllText(Path.Combine(correctOutput, "package.json")));

        string batchOutput = Path.Combine(_root, "different-actions");
        BatchExportResult batch = exporter.ExportBatch(project, _root, [first, second],
            [firstAnimation.Id, secondAnimation.Id],
            new CharacterExportOptions { Views = ["Down"] }, batchOutput);
        Assert.Equal(2, batch.Packages.Count);
        Assert.Equal(2, batch.Skipped.Count);
        Assert.True(File.Exists(Path.Combine(batchOutput, "characters", first.Id,
            $"animation-{firstAnimation.Id}", "package.json")));
        Assert.True(File.Exists(Path.Combine(batchOutput, "characters", second.Id,
            $"animation-{secondAnimation.Id}", "package.json")));
        Assert.False(Directory.Exists(Path.Combine(batchOutput, "characters", first.Id,
            $"animation-{secondAnimation.Id}")));

        AnimationDefinition version = ActionTemplateCatalog.Generate(project, first, "wave",
            mode: ActionRegenerationMode.CreateNewVersion);
        string versionOutput = Path.Combine(_root, "version-export");
        exporter.ExportCharacter(project, _root, first,
            new CharacterExportOptions { AnimationId = version.Id, Views = ["Down"] }, versionOutput);
        JsonNode action = JsonNode.Parse(File.ReadAllText(Path.Combine(versionOutput, "package.json")))!["actions"]![0]!;
        Assert.Equal(version.Id, action["animation"]!.GetValue<string>());
        Assert.NotEmpty(action["sourceFingerprint"]!.GetValue<string>());
    }

    [Fact]
    public void BatchExport_WritesSeparateCharacterAnimationPackages_WithFrameMetadata()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Style.CharacterRenderer = "ReferenceGrid";
        new ProjectStore().Save(project, _root);
        StarterContentFactory.CreateBaseBody(project, _root, new AssetLibraryService());
        new BehaviorLibraryService().InstallPresets(project);
        CharacterEntity first = project.Characters.Single();
        CharacterEntity second = new()
        {
            Id = "char.second",
            Name = "Second",
            RigId = first.RigId,
        };
        project.Characters.Add(second);
        ExportService exporter = new(new RigSpriteComposer(new AssetLibraryService()));
        string output = Path.Combine(_root, "batch");

        BatchExportResult result = exporter.ExportBatch(project, _root, [first, second],
            [null, "walk"], new CharacterExportOptions
            {
                Views = ["Down", "Up"],
                IncludeFrames = true,
                IncludeSpritesheet = true,
                IncludeLayers = true,
                IncludeGodot = true,
            }, output);

        Assert.Equal(4, result.Packages.Count);
        Assert.Equal(20, result.FrameCount);
        Assert.Equal(8, result.SheetCount);
        foreach (CharacterEntity character in new[] { first, second })
        {
            string characterRoot = Path.Combine(output, "characters", character.Id);
            string defaultPackage = Path.Combine(characterRoot, "default-pose", "package.json");
            string walkPackage = Path.Combine(characterRoot, "animation-walk", "package.json");
            Assert.True(File.Exists(defaultPackage));
            Assert.True(File.Exists(walkPackage));
            Assert.Equal(character.Id, JsonNode.Parse(File.ReadAllText(walkPackage))!["character"]!["id"]!.GetValue<string>());
            JsonNode walk = JsonNode.Parse(File.ReadAllText(walkPackage))!["animation"]!;
            Assert.Equal(4, walk["frames"]!.AsArray().Count);
            Assert.True(walk["loop"]!.GetValue<bool>());
            Assert.Equal(1, walk["frames"]![0]!["durationTicks"]!.GetValue<int>());
            Assert.Contains(walk["frames"]!.AsArray(), frame => frame!["markers"]!.AsArray().Count > 0);
            Assert.True(File.Exists(Path.Combine(characterRoot, "animation-walk", "godot", "sprite_frames.tres")));
            Assert.True(Directory.Exists(Path.Combine(characterRoot, "animation-walk", "layers", "down", "down_00")));
            string manifest = File.ReadAllText(Path.Combine(characterRoot, "animation-walk", "manifest.json"));
            Assert.Contains("down_03", manifest);
            Assert.Contains("up_03", manifest);
        }
    }

    [Fact]
    public void AutosaveRecovery_PreservesCharacterBuildAndGeneratedActions()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Rigs.Add(RigTemplates.HumanoidTopDown4());
        CharacterEntity character = new() { Id = "char.autosave", Name = "Autosave", RigId = RigTemplates.HumanoidTopDownId };
        project.Characters.Add(character);
        var store = new ProjectStore();
        store.Save(project, _root);

        character.Build.ArmLengthPx = 15;
        character.SelectedActionIds.Add("wave");
        ActionTemplateCatalog.Generate(project, character, "wave");
        store.Autosave(project, _root);
        File.SetLastWriteTimeUtc(ProjectPaths.AutosaveFile(_root), DateTime.UtcNow.AddMinutes(1));

        Assert.True(store.TryRecover(_root, out Project? recovered));
        CharacterEntity restored = Assert.Single(recovered!.Characters);
        Assert.Equal(15, restored.Build.ArmLengthPx);
        Assert.Contains("wave", restored.ActionIds);
        Assert.Contains("wave", restored.SelectedActionIds);
        Assert.Equal(CharacterActionIds.Animation(restored.Id, "wave"),
            Assert.Single(restored.GeneratedActionBindings).AnimationId);
        Assert.Contains(recovered.Animations,
            animation => animation.Id == CharacterActionIds.Animation(restored.Id, "wave"));
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
        legacyCharacter.Remove("selectedActionIds");
        File.WriteAllText(ProjectPaths.ProjectFile(_root), json.ToJsonString(ProjectStore.JsonOptions));

        CharacterEntity restored = Assert.Single(store.Open(_root).Characters);
        Assert.Equal(CharacterBuildProfile.DefaultHeadHeightPx, restored.Build.HeadHeightPx);
        Assert.Equal("Unspecified", restored.Build.Gender);
        Assert.Empty(restored.ActionIds);
        Assert.Empty(restored.SelectedActionIds);
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
