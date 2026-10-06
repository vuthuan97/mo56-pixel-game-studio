using PixelGameStudio.Behaviors;
using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Animation;
using PixelGameStudio.Domain.Behaviors;
using PixelGameStudio.Domain.Templates;
using PixelGameStudio.ProjectSystem;
using Xunit;

namespace PixelGameStudio.ProjectSystem.Tests;

/// <summary>Phase 6-8: pose/animation/behavior presets, validation and behavior template import/export.</summary>
public class AnimationBehaviorTests : IDisposable
{
    private readonly string _root;
    private readonly ProjectStore _store = new();
    private readonly BehaviorLibraryService _behaviors = new();

    public AnimationBehaviorTests()
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

    private Project CreateProject()
    {
        var project = ProjectTemplates.ClassicTopDown46();
        project.Rigs.Add(RigTemplates.HumanoidTopDown4()); // behaviors bind to rig slots/anchors
        Directory.CreateDirectory(_root);
        File.WriteAllText(ProjectPaths.ProjectFile(_root), _store.Serialize(project));
        return project;
    }

    [Fact]
    public void InstallPresets_AddsPosesAnimationsBehaviorsOnce()
    {
        Project project = CreateProject();

        int first = _behaviors.InstallPresets(project);
        int second = _behaviors.InstallPresets(project);

        Assert.True(first > 0);
        Assert.Equal(0, second);
        Assert.Equal(16, project.Poses.Count);
        Assert.Equal(6, project.Animations.Count);
        Assert.Equal(66, project.Behaviors.Count);
        Assert.Empty(project.Validate());
    }

    [Fact]
    public void PresetAnimations_ReferenceExistingPoses_AndCarryMarkers()
    {
        Project project = CreateProject();
        _behaviors.InstallPresets(project);

        AnimationDefinition walk = project.Animations.Single(a => a.Id == "walk");
        Assert.Equal(4, walk.Frames.Count);
        Assert.True(walk.Loop);
        Assert.Equal(2, walk.Frames.Count(f => f.Markers.Any(m => m.Type == FrameMarkerTypes.Footstep)));

        AnimationDefinition attack = project.Animations.Single(a => a.Id == "attack");
        Assert.Contains(attack.Frames[2].Markers, m => m.Type == FrameMarkerTypes.Hit);

        AnimationDefinition death = project.Animations.Single(a => a.Id == "death");
        Assert.False(death.Loop);

        Assert.Empty(AnimationRules.Validate(project));
    }

    [Fact]
    public void PresetBehavior_ChopTree_BindsAnimationItemAnchorAndMarkers()
    {
        Project project = CreateProject();
        _behaviors.InstallPresets(project);

        BehaviorDefinition chop = project.Behaviors.Single(b => b.Id == "beh.chop_tree");

        Assert.Equal("attack", chop.AnimationId);
        Assert.Equal("main_hand", chop.HeldItemSlotId);
        Assert.Equal("anchor.interact", chop.InteractionAnchorId);
        Assert.Contains(chop.Markers, m => m.Type == BehaviorMarkerTypes.Gameplay && m.Name == "HitResource" && m.FrameIndex == 2);
    }

    [Fact]
    public void Behaviors_GroupByGroup_FromProductOverview()
    {
        Project project = CreateProject();
        _behaviors.InstallPresets(project);

        var groups = project.Behaviors.Select(b => b.Group).Distinct().ToHashSet(StringComparer.Ordinal);
        Assert.Subset(groups, new HashSet<string>
        {
            BehaviorGroups.Movement, BehaviorGroups.Combat, BehaviorGroups.Survival,
            BehaviorGroups.Management, BehaviorGroups.Adventure,
        });
        Assert.Equal(11, _behaviors.ListByGroup(project, BehaviorGroups.Movement).Count);
    }

    [Fact]
    public void CreateCustom_Duplicates_AndDeleteProtectsPresets()
    {
        Project project = CreateProject();
        _behaviors.InstallPresets(project);

        BehaviorDefinition custom = _behaviors.CreateCustom(project, "beh.chop_tree", "beh.chop_tree_big", "Chặt cây lớn");
        Assert.Equal("attack", custom.AnimationId);
        Assert.Equal(3, custom.Markers.Count);

        Assert.Throws<InvalidOperationException>(() => _behaviors.Delete(project, "beh.chop_tree"));
        _behaviors.Delete(project, "beh.chop_tree_big");
        Assert.DoesNotContain(project.Behaviors, b => b.Id == "beh.chop_tree_big");
    }

    [Fact]
    public void BehaviorTemplate_ExportImport_RoundTrips()
    {
        Project project = CreateProject();
        _behaviors.InstallPresets(project);
        BehaviorDefinition source = project.Behaviors.Single(b => b.Id == "beh.chop_tree");

        string templatePath = Path.Combine(_root, "chop_template.json");
        _behaviors.ExportTemplate(source, templatePath);

        BehaviorDefinition imported = _behaviors.ImportTemplate(templatePath);
        BehaviorDefinition installed = _behaviors.Install(project, imported, "beh.chop_tree_imported");

        Assert.Equal("beh.chop_tree_imported", installed.Id);
        Assert.Equal(source.AnimationId, installed.AnimationId);
        Assert.Equal(source.HeldItemSlotId, installed.HeldItemSlotId);
        Assert.Equal(source.InteractionAnchorId, installed.InteractionAnchorId);
        Assert.Equal(source.Markers.Count, installed.Markers.Count);
        Assert.Equal(2, installed.Markers.First(m => m.Type == BehaviorMarkerTypes.Gameplay).FrameIndex);
    }

    [Fact]
    public void AnimationFrames_SupportReorderDuplicateCopyPasteSemantics()
    {
        Project project = CreateProject();
        _behaviors.InstallPresets(project);
        AnimationDefinition walk = project.Animations.Single(a => a.Id == "walk");

        // duplicate + reorder as data operations (the timeline editor mutates the list)
        AnimationFrame copy = new AnimationFrame
        {
            PoseId = walk.Frames[0].PoseId,
            DurationTicks = walk.Frames[0].DurationTicks,
            Markers = walk.Frames[0].Markers.Select(m => new FrameMarker(m.Type, m.Value)).ToList(),
        };
        walk.Frames.Insert(2, copy);
        Assert.Equal(5, walk.Frames.Count);

        AnimationFrame removed = walk.Frames[2];
        walk.Frames.RemoveAt(2);
        Assert.Equal(copy.PoseId, removed.PoseId);
        Assert.Equal(4, walk.Frames.Count);
    }

    [Fact]
    public void ProjectRoundTrip_KeepsPosesAnimationsBehaviors()
    {
        Project project = CreateProject();
        _behaviors.InstallPresets(project);
        _store.Save(project, _root);

        Project loaded = _store.Open(_root);

        Assert.Equal(16, loaded.Poses.Count);
        Assert.Equal(6, loaded.Animations.Count);
        Assert.Equal(66, loaded.Behaviors.Count);
        AnimationDefinition attack = loaded.Animations.Single(a => a.Id == "attack");
        Assert.Contains(attack.Frames[2].Markers, m => m.Type == FrameMarkerTypes.Sound && m.Value == "slash");
        Assert.Empty(loaded.Validate());
    }

    [Fact]
    public void Validation_CatchesBrokenAnimationAndBehaviorRefs()
    {
        Project project = CreateProject();
        _behaviors.InstallPresets(project);

        project.Animations.Single(a => a.Id == "walk").Frames[0].PoseId = "pose.missing";
        project.Behaviors.Single(b => b.Id == "beh.stand").AnimationId = "anim.missing";
        project.Behaviors.Single(b => b.Id == "beh.walk").HeldItemSlotId = "slot.missing";

        IReadOnlyList<string> issues = project.Validate();
        Assert.Contains(issues, i => i.Contains("pose không tồn tại 'pose.missing'"));
        Assert.Contains(issues, i => i.Contains("animation không tồn tại 'anim.missing'"));
        Assert.Contains(issues, i => i.Contains("held item slot không tồn tại 'slot.missing'"));
    }
}
