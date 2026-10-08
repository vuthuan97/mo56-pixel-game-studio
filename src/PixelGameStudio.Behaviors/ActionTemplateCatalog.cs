using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    string? RequiredAnchorId = null,
    string? SourceAnimationId = null);

public sealed record ActionAvailability(
    ActionTemplateDefinition Template,
    bool IsAvailable,
    string Reason);

public enum ActionRegenerationMode
{
    Preserve,
    CreateNewVersion,
    Replace,
}

/// <summary>
/// Data-driven action catalog. Each generated action owns generated poses and
/// an animation; unsupported actions are never silently aliased to idle/walk.
/// </summary>
public static class ActionTemplateCatalog
{
    public static PoseDefinition CreatePreviewPose(string templateId, Project? project = null)
    {
        ActionTemplateDefinition template = (project is null ? All : Templates(project)).FirstOrDefault(item => item.Id == templateId)
            ?? throw new InvalidOperationException($"Action template '{templateId}' was not found.");
        if (template.SourceAnimationId is not null)
        {
            AnimationDefinition source = project!.Animations.First(animation => animation.Id == template.SourceAnimationId);
            return project.Poses.First(pose => pose.Id == source.Frames[0].PoseId).Clone();
        }
        return CreateTemplatePose(template, 0, $"preview.{template.Id}");
    }

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
        T("idle", "Đứng", "Movement", [(0, 0, "down", "down"), (1, 1, "down", "down")], loop: true),
        T("walk", "Đi bộ", "Movement", [(0, 0, "forward", "back"), (1, 0, "back", "forward"), (0, 0, "back", "forward"), (1, 0, "forward", "back")], loop: true),
        T("run", "Chạy", "Movement", [(-1, -1, "forward", "back"), (1, 0, "back", "forward"), (-1, -1, "back", "forward"), (1, 0, "forward", "back")], loop: true),
        T("turn", "Xoay vòng", "Movement", [(0, 0, "down", "down"), (0, 0, "back", "down"), (0, 0, "up", "up"), (0, 0, "down", "back")], loop: true),
        T("lie", "Nằm", "Adventure", [(3, 2, "back", "back"), (4, 3, "back", "back")]),
        T("hands_on_hips", "Chống nạnh", "Adventure", [(0, 0, "forward", "forward"), (0, 0, "back", "back")], loop: true),
        T("arms_crossed", "Khoanh tay", "Adventure", [(0, 0, "back", "forward"), (0, -1, "forward", "back")], loop: true),
        T("kneel", "Quỳ", "Adventure", [(2, 1, "down", "down"), (3, 2, "forward", "back")]),
        T("punch", "Đấm", "Combat", [(0, 0, "back", "down"), (-1, -1, "forward", "down"), (0, 0, "back", "down")]),
        T("kick", "Đá", "Combat", [(0, 0, "down", "back"), (1, -1, "down", "forward"), (0, 0, "down", "back")]),
        T("raise_left", "Giơ tay trái", "Combat", [(0, 0, "up", "down"), (-1, -1, "up", "down")]),
        T("raise_right", "Giơ tay phải", "Combat", [(0, 0, "down", "up"), (-1, -1, "down", "up")]),
        T("slash", "Chém", "Combat", [(0, 0, "back", "back"), (-1, -1, "up", "back"), (0, 0, "forward", "back")], requiredSlotId: "main_hand"),
        T("stab", "Đâm", "Combat", [(0, 0, "back", "back"), (0, -1, "forward", "back"), (0, 0, "back", "back")], requiredSlotId: "main_hand"),
        T("dodge_left", "Né trái", "Combat", [(0, 0, "back", "forward"), (-1, 0, "forward", "back")]),
        T("hit", "Bị đánh", "Combat", [(0, 0, "down", "down"), (1, 1, "back", "back")]),
        T("die", "Chết", "Combat", [(1, 1, "back", "back"), (3, 3, "back", "back"), (4, 4, "down", "down")]),
        T("eat", "Ăn", "Daily", [(0, 0, "down", "up"), (0, -1, "forward", "up"), (0, 0, "down", "up")]),
        T("drink", "Uống", "Daily", [(0, 0, "down", "up"), (-1, -1, "forward", "up"), (0, 0, "down", "up")]),
        T("sleep", "Ngủ", "Daily", [(3, 2, "back", "back"), (4, 3, "back", "back")], loop: true),
        T("read", "Đọc", "Daily", [(0, 0, "forward", "forward"), (1, 0, "forward", "forward")], loop: true),
        T("write", "Viết", "Daily", [(0, 0, "down", "forward"), (0, -1, "down", "forward")], loop: true),
        T("talk", "Nói chuyện", "Daily", [(0, 0, "down", "down"), (-1, 0, "forward", "down"), (0, 0, "down", "down")], loop: true),
        T("pick_up", "Nhặt", "Interaction", [(1, 1, "down", "down"), (2, 2, "forward", "down"), (0, 0, "down", "down")], requiredAnchorId: "anchor.interact"),
        T("carry", "Mang", "Interaction", [(0, 0, "forward", "forward"), (1, 0, "back", "back")], loop: true),
        T("repair", "Sửa chữa", "Interaction", [(0, 0, "down", "forward"), (1, 0, "forward", "down")], requiredAnchorId: "anchor.interact"),
        T("dig", "Đào", "Interaction", [(0, 0, "back", "down"), (1, 1, "forward", "down"), (0, 0, "back", "down")], requiredSlotId: "main_hand"),
        T("chop", "Chặt", "Interaction", [(0, 0, "back", "down"), (-1, -1, "up", "down"), (0, 0, "forward", "down")], requiredSlotId: "main_hand"),
    ];

    public static IReadOnlyList<ActionTemplateDefinition> Templates(Project project) =>
    [
        .. All,
        .. project.Behaviors.Where(behavior => behavior.Id.StartsWith("behavior.custom.", StringComparison.Ordinal)
                && !All.Any(template => template.Id == behavior.Id))
            .Select(behavior => new ActionTemplateDefinition(behavior.Id, behavior.DisplayName,
                behavior.Group, [], SourceAnimationId: behavior.AnimationId ?? string.Empty)),
    ];

    public static IReadOnlyList<ActionAvailability> Evaluate(Project project, CharacterEntity character)
    {
        return Templates(project).Select(template =>
        {
            RigDefinition? rig = project.Rigs.FirstOrDefault(r => r.Id == character.RigId);
            if (rig is null)
            {
                return new ActionAvailability(template, false, "Character rig is missing.");
            }

            if (template.SourceAnimationId is not null)
            {
                AnimationDefinition? source = project.Animations.FirstOrDefault(animation => animation.Id == template.SourceAnimationId);
                if (source is null || source.Frames.Count == 0 ||
                    source.Frames.Any(frame => project.Poses.All(pose => pose.Id != frame.PoseId)))
                {
                    return new ActionAvailability(template, false, "Mẫu tự biên soạn thiếu animation hoặc pose hợp lệ.");
                }
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

            AnimationDefinition? generated = FindGeneratedAnimation(project, character, template.Id);
            if (generated is null)
            {
                return new ActionAvailability(template, true, "Ready to generate.");
            }

            GeneratedActionBinding? binding = character.GeneratedActionBindings
                .FirstOrDefault(item => item.TemplateId == template.Id);
            if (binding is not null && binding.SourceFingerprint != SourceFingerprint(project, character, template))
            {
                return new ActionAvailability(template, true, "Cần tạo lại: nguồn nhân vật đã đổi.");
            }

            return new ActionAvailability(template, true, binding is null
                ? "Đã có output cũ (chưa có fingerprint nguồn)."
                : "Đã tạo và nguồn còn khớp.");
        }).ToList();
    }

    public static AnimationDefinition Generate(
        Project project,
        CharacterEntity character,
        string templateId,
        CancellationToken cancellationToken = default,
        IProgress<double>? progress = null,
        ActionRegenerationMode mode = ActionRegenerationMode.Preserve)
    {
        ActionTemplateDefinition template = Templates(project).FirstOrDefault(t => t.Id == templateId)
            ?? throw new InvalidOperationException($"Action template '{templateId}' was not found.");
        ActionAvailability availability = Evaluate(project, character).First(item => item.Template.Id == templateId);
        if (!availability.IsAvailable)
        {
            throw new InvalidOperationException(availability.Reason);
        }

        string baseId = CharacterActionIds.Animation(character.Id, template.Id);
        AnimationDefinition? existing = FindGeneratedAnimation(project, character, template.Id);
        if (existing is not null && mode == ActionRegenerationMode.Preserve)
        {
            AddActionBinding(character, template.Id);
            return existing;
        }

        if (existing is null && mode == ActionRegenerationMode.Replace)
        {
            throw new InvalidOperationException("Chưa có animation để thay thế.");
        }

        if (existing?.Id == CharacterActionIds.LegacyAnimation(template.Id) &&
            mode == ActionRegenerationMode.Replace)
        {
            throw new InvalidOperationException("Output legacy dùng chung; hãy tạo bản mới riêng cho nhân vật.");
        }

        string animationId = existing is null ? baseId : mode switch
        {
            ActionRegenerationMode.CreateNewVersion => NextVersionId(project, baseId),
            ActionRegenerationMode.Replace => existing.Id,
            _ => baseId,
        };

        string[] oldPoseIds = [];
        if (existing is not null && mode == ActionRegenerationMode.Replace)
        {
            oldPoseIds = project.Poses.Where(pose =>
                pose.Id.StartsWith(existing.Id + ".", StringComparison.Ordinal))
                .Select(pose => pose.Id).ToArray();
            if (existing.Frames.Any(frame => !oldPoseIds.Contains(frame.PoseId, StringComparer.Ordinal)) ||
                project.Animations.Where(item => !ReferenceEquals(item, existing))
                    .SelectMany(item => item.Frames).Any(frame => oldPoseIds.Contains(frame.PoseId, StringComparer.Ordinal)))
            {
                throw new InvalidOperationException("Pose đang được dùng chung/chỉnh tay; hãy tạo bản mới thay vì thay thế.");
            }
        }

        AnimationDefinition? authoredSource = template.SourceAnimationId is null ? null
            : project.Animations.First(animation => animation.Id == template.SourceAnimationId);
        AnimationDefinition animation = authoredSource?.Clone() ?? new AnimationDefinition
        {
            DisplayName = template.DisplayName,
            Fps = template.Fps,
            Loop = template.Loop,
        };
        animation.Id = animationId;
        var generatedPoses = new List<PoseDefinition>();
        int frameCount = authoredSource?.Frames.Count ?? template.Frames.Count;
        for (int i = 0; i < frameCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string poseId = $"{animationId}.{i}";
            PoseDefinition pose;
            if (authoredSource is not null)
            {
                pose = project.Poses.First(sourcePose => sourcePose.Id == authoredSource.Frames[i].PoseId).Clone();
                pose.Id = poseId;
                animation.Frames[i].PoseId = poseId;
            }
            else
            {
                pose = CreateTemplatePose(template, i, poseId);
                animation.Frames.Add(new AnimationFrame { PoseId = poseId, DurationTicks = 1 });
            }
            pose.DisplayName = $"{template.DisplayName} {i + 1}";
            generatedPoses.Add(pose);
            progress?.Report((i + 1) / (double)frameCount);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (existing is not null && mode == ActionRegenerationMode.Replace)
        {
            project.Animations.Remove(existing);
            project.Poses.RemoveAll(pose => oldPoseIds.Contains(pose.Id, StringComparer.Ordinal));
        }
        project.Poses.AddRange(generatedPoses);
        project.Animations.Add(animation);
        // Behavior templates are shared project data; binding one to a
        // character-owned animation would silently redirect other characters.
        AddActionBinding(character, template.Id);
        character.GeneratedActionBindings.RemoveAll(item => item.TemplateId == template.Id);
        character.GeneratedActionBindings.Add(new GeneratedActionBinding
        {
            TemplateId = template.Id,
            AnimationId = animation.Id,
            SourceFingerprint = SourceFingerprint(project, character, template),
        });
        return animation;
    }

    private static AnimationDefinition? FindGeneratedAnimation(Project project, CharacterEntity character, string templateId)
    {
        string? boundId = character.GeneratedActionBindings
            .FirstOrDefault(item => item.TemplateId == templateId)?.AnimationId;
        AnimationDefinition? bound = project.Animations.FirstOrDefault(animation => animation.Id == boundId);
        if (bound is not null) return bound;

        string scopedId = CharacterActionIds.Animation(character.Id, templateId);
        AnimationDefinition? scoped = project.Animations.FirstOrDefault(animation => animation.Id == scopedId);
        if (scoped is not null) return scoped;

        return character.ActionIds.Contains(templateId, StringComparer.Ordinal)
            ? project.Animations.FirstOrDefault(animation =>
                animation.Id == CharacterActionIds.LegacyAnimation(templateId))
            : null;
    }

    private static string NextVersionId(Project project, string baseId)
    {
        int version = 2;
        string candidate;
        do
        {
            candidate = $"{baseId}.v{version++}";
        }
        while (project.Animations.Any(animation => animation.Id == candidate));
        return candidate;
    }

    private static PoseDefinition CreateTemplatePose(ActionTemplateDefinition template, int index, string poseId)
    {
        (int bodyDy, int headDy, string leftArm, string rightArm) = template.Frames[index];
        string leftLeg = template.Id switch
        {
            "walk" or "run" or "turn" => index % 2 == 0 ? "forward" : "back",
            "kick" => index == 1 ? "forward" : "neutral",
            "lie" or "sleep" => "back",
            "kneel" => "back",
            _ => "neutral",
        };
        string rightLeg = template.Id switch
        {
            "walk" or "run" or "turn" => index % 2 == 0 ? "back" : "forward",
            "kick" => index == 1 ? "back" : "neutral",
            "lie" or "sleep" => "back",
            "kneel" => "back",
            _ => "neutral",
        };
        return PosePresets.Legacy(poseId, bodyDy: bodyDy, headDy: headDy,
            leftArm: leftArm, rightArm: rightArm, leftLeg: leftLeg, rightLeg: rightLeg);
    }

    private static string SourceFingerprint(Project project, CharacterEntity character, ActionTemplateDefinition template)
    {
        string json = JsonSerializer.Serialize(new
        {
            template.Id,
            frames = template.Frames.Select(frame => new
            {
                frame.BodyDy, frame.HeadDy, frame.LeftArm, frame.RightArm,
            }).ToArray(),
            template.Fps, template.Loop, template.RequiredSlotId, template.RequiredAnchorId,
            sourceAnimation = template.SourceAnimationId is null ? null
                : project.Animations.FirstOrDefault(animation => animation.Id == template.SourceAnimationId),
            sourcePoses = template.SourceAnimationId is null ? null
                : project.Animations.FirstOrDefault(animation => animation.Id == template.SourceAnimationId)?.Frames
                    .Select(frame => project.Poses.FirstOrDefault(pose => pose.Id == frame.PoseId)).ToArray(),
            character.RigId, character.Build, character.Appearance, character.Equipment,
            rig = project.Rigs.FirstOrDefault(item => item.Id == character.RigId),
            project.View.Perspective, project.View.Directions, project.Style,
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    private static void AddActionBinding(CharacterEntity character, string templateId)
    {
        if (!character.ActionIds.Contains(templateId, StringComparer.Ordinal))
        {
            character.ActionIds.Add(templateId);
        }
    }

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
