using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Animation;
using PixelGameStudio.Domain.Behaviors;
using PixelGameStudio.Domain.Character;
using PixelGameStudio.Domain.Templates;

namespace PixelGameStudio.Behaviors;

public sealed record ActionTemplateDefinition(
    string Id,
    string DisplayName,
    string Group,
    IReadOnlyList<(int BodyDy, int HeadDy, string LeftArm, string RightArm)> Frames,
    int Fps = 8,
    bool Loop = false,
    string? RequiredSlotId = null,
    string? RequiredAnchorId = null);

public sealed record ActionAvailability(
    ActionTemplateDefinition Template,
    bool IsAvailable,
    string Reason);

/// <summary>
/// Data-driven action catalog. Each generated action owns generated poses and
/// an animation; unsupported actions are never silently aliased to idle/walk.
/// </summary>
public static class ActionTemplateCatalog
{
    public static IReadOnlyList<ActionTemplateDefinition> All { get; } =
    [
        T("jump", "Jump", "Movement", [(0, -2, "up", "up"), (-2, -3, "up", "up"), (0, -1, "down", "down")]),
        T("fall", "Fall", "Movement", [(1, 1, "back", "back"), (3, 2, "back", "back")]),
        T("land", "Land", "Movement", [(2, 1, "down", "down"), (0, 0, "down", "down")]),
        T("dodge", "Dodge", "Movement", [(0, 0, "back", "forward"), (-1, 0, "forward", "back")]),
        T("roll", "Roll", "Movement", [(0, 1, "back", "forward"), (1, 2, "forward", "back"), (0, 0, "down", "down")]),
        T("crawl", "Crawl", "Movement", [(1, 1, "forward", "back"), (1, 1, "back", "forward")], loop: true),
        T("climb", "Climb", "Movement", [(0, -1, "up", "down"), (0, 0, "down", "up")], loop: true),
        T("slide", "Slide", "Movement", [(1, 0, "back", "forward"), (1, 1, "forward", "back")]),
        T("block", "Block", "Combat", [(0, 0, "up", "up")], requiredSlotId: "off_hand"),
        T("throw", "Throw", "Combat", [(0, 0, "back", "back"), (-1, -1, "up", "back"), (0, 0, "forward", "back")]),
        T("wave", "Wave", "Adventure", [(0, 0, "up", "down"), (0, 0, "forward", "down")]),
        T("point", "Point", "Adventure", [(0, 0, "forward", "down")]),
        T("sit", "Sit", "Adventure", [(2, 2, "down", "down")]),
        T("open_door", "Open door", "Adventure", [(0, 0, "forward", "back"), (-1, -1, "up", "back")], requiredAnchorId: "anchor.interact"),
    ];

    public static IReadOnlyList<ActionAvailability> Evaluate(Project project, CharacterEntity character)
    {
        return All.Select(template =>
        {
            RigDefinition? rig = project.Rigs.FirstOrDefault(r => r.Id == character.RigId);
            if (rig is null)
            {
                return new ActionAvailability(template, false, "Character rig is missing.");
            }

            if (template.RequiredAnchorId is not null && rig.FindAnchor(template.RequiredAnchorId) is null)
            {
                return new ActionAvailability(template, false, $"Missing anchor: {template.RequiredAnchorId}.");
            }

            if (template.RequiredSlotId is not null &&
                !character.Equipment.Any(item => item.SlotId == template.RequiredSlotId))
            {
                return new ActionAvailability(template, false, $"Equip slot '{template.RequiredSlotId}' first.");
            }

            string animationId = GeneratedAnimationId(template);
            bool ready = project.Animations.Any(animation => animation.Id == animationId);
            return new ActionAvailability(template, true, ready ? "Generated and ready." : "Ready to generate.");
        }).ToList();
    }

    public static AnimationDefinition Generate(
        Project project,
        CharacterEntity character,
        string templateId,
        CancellationToken cancellationToken = default,
        IProgress<double>? progress = null)
    {
        ActionTemplateDefinition template = All.FirstOrDefault(t => t.Id == templateId)
            ?? throw new InvalidOperationException($"Action template '{templateId}' was not found.");
        ActionAvailability availability = Evaluate(project, character).First(item => item.Template.Id == templateId);
        if (!availability.IsAvailable)
        {
            throw new InvalidOperationException(availability.Reason);
        }

        string animationId = GeneratedAnimationId(template);
        AnimationDefinition? existing = project.Animations.FirstOrDefault(animation => animation.Id == animationId);
        if (existing is not null)
        {
            AddActionBinding(character, template.Id);
            return existing;
        }

        var animation = new AnimationDefinition
        {
            Id = animationId,
            DisplayName = template.DisplayName,
            Fps = template.Fps,
            Loop = template.Loop,
        };
        var generatedPoses = new List<PoseDefinition>();
        for (int i = 0; i < template.Frames.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (int bodyDy, int headDy, string leftArm, string rightArm) = template.Frames[i];
            string poseId = $"action.{template.Id}.{i}";
            PoseDefinition pose = PosePresets.Legacy(poseId, bodyDy: bodyDy, headDy: headDy,
                leftArm: leftArm, rightArm: rightArm);
            pose.DisplayName = $"{template.DisplayName} {i + 1}";
            generatedPoses.Add(pose);
            animation.Frames.Add(new AnimationFrame { PoseId = poseId, DurationTicks = 1 });
            progress?.Report((i + 1) / (double)template.Frames.Count);
        }

        cancellationToken.ThrowIfCancellationRequested();
        project.Poses.AddRange(generatedPoses);
        project.Animations.Add(animation);
        BehaviorDefinition? behavior = project.Behaviors.FirstOrDefault(item => item.Id == $"beh.{template.Id}");
        if (behavior is not null)
        {
            behavior.AnimationId = animation.Id;
        }

        AddActionBinding(character, template.Id);
        return animation;
    }

    private static void AddActionBinding(CharacterEntity character, string templateId)
    {
        if (!character.ActionIds.Contains(templateId, StringComparer.Ordinal))
        {
            character.ActionIds.Add(templateId);
        }
    }

    private static string GeneratedAnimationId(ActionTemplateDefinition template) => $"action.{template.Id}";

    private static ActionTemplateDefinition T(
        string id,
        string name,
        string group,
        IReadOnlyList<(int BodyDy, int HeadDy, string LeftArm, string RightArm)> frames,
        int fps = 8,
        bool loop = false,
        string? requiredSlotId = null,
        string? requiredAnchorId = null) =>
        new(id, name, group, frames, fps, loop, requiredSlotId, requiredAnchorId);
}
