using PixelGameStudio.Domain.Behaviors;
using PixelGameStudio.Domain.Character;

namespace PixelGameStudio.Domain.Animation;

/// <summary>Creates editable, character-scoped animation data without sharing mutable poses.</summary>
public static class CharacterAnimationEditor
{
    public static AnimationDefinition Create(Project project, CharacterEntity character, PoseDefinition? seed = null)
    {
        ValidateOwner(project, character);
        string animationId = NextAnimationId(project, character);
        PoseDefinition pose = (seed ?? PoseDefinition.Identity("seed")).Clone();
        pose.Id = $"{animationId}.pose.0";
        pose.DisplayName = "Frame 1";
        var animation = new AnimationDefinition
        {
            Id = animationId,
            DisplayName = "Hoạt ảnh mới",
            Frames = [new AnimationFrame { PoseId = pose.Id }],
        };
        project.Poses.Add(pose);
        project.Animations.Add(animation);
        return animation;
    }

    public static AnimationDefinition Duplicate(Project project, CharacterEntity character, AnimationDefinition source)
    {
        ValidateOwner(project, character);
        if (!project.Animations.Contains(source))
        {
            throw new InvalidOperationException("Animation source is not in the project.");
        }

        var sourcePoses = source.Frames.Select(frame => project.Poses.FirstOrDefault(p => p.Id == frame.PoseId)
            ?? throw new InvalidOperationException($"Missing pose '{frame.PoseId}'.")).Distinct().ToList();
        string animationId = NextAnimationId(project, character);
        var poseMap = new Dictionary<string, string>(StringComparer.Ordinal);
        var clonedPoses = new List<PoseDefinition>();
        for (int i = 0; i < sourcePoses.Count; i++)
        {
            PoseDefinition pose = sourcePoses[i].Clone();
            pose.Id = $"{animationId}.pose.{i}";
            poseMap.Add(sourcePoses[i].Id, pose.Id);
            clonedPoses.Add(pose);
        }

        AnimationDefinition animation = source.Clone();
        animation.Id = animationId;
        animation.DisplayName = $"{source.DisplayName} (bản sao)";
        foreach (AnimationFrame frame in animation.Frames)
        {
            frame.PoseId = poseMap[frame.PoseId];
        }

        project.Poses.AddRange(clonedPoses);
        project.Animations.Add(animation);
        return animation;
    }

    public static BehaviorDefinition SaveAsBehaviorTemplate(Project project, AnimationDefinition animation)
    {
        if (!project.Animations.Contains(animation) || animation.Frames.Count == 0 ||
            animation.Frames.Any(frame => project.Poses.All(pose => pose.Id != frame.PoseId)))
        {
            throw new InvalidOperationException("Animation must have valid frames before saving a behavior template.");
        }

        const string prefix = "behavior.custom.";
        int suffix = 1;
        while (project.Behaviors.Any(behavior => behavior.Id == $"{prefix}{suffix}")) suffix++;
        var template = new BehaviorDefinition
        {
            Id = $"{prefix}{suffix}",
            DisplayName = animation.DisplayName,
            Group = BehaviorGroups.Adventure,
            AnimationId = animation.Id,
            Notes = "Mẫu tự biên soạn từ tab Hoạt ảnh.",
        };
        project.Behaviors.Add(template);
        return template;
    }

    private static string NextAnimationId(Project project, CharacterEntity character)
    {
        string prefix = $"custom.{CharacterActionIds.Animation(character.Id, "animation")}.";
        int suffix = 1;
        while (project.Animations.Any(animation => animation.Id == $"{prefix}{suffix}")) suffix++;
        return $"{prefix}{suffix}";
    }

    private static void ValidateOwner(Project project, CharacterEntity character)
    {
        if (!project.Characters.Contains(character))
        {
            throw new InvalidOperationException("Character is not in the project.");
        }
    }
}
