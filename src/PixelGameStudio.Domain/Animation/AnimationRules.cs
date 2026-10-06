using PixelGameStudio.Domain.Behaviors;

namespace PixelGameStudio.Domain.Animation;

/// <summary>Validation for pose/animation/behavior data across a project.</summary>
public static class AnimationRules
{
    public static IReadOnlyList<string> Validate(Project project)
    {
        var issues = new List<string>();
        var poseIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (PoseDefinition pose in project.Poses)
        {
            if (!Domain.Assets.AssetRules.IsValidId(pose.Id) || !poseIds.Add(pose.Id))
            {
                issues.Add($"Pose id không hợp lệ hoặc bị trùng: '{pose.Id}'.");
            }

            if (string.IsNullOrWhiteSpace(pose.DisplayName))
            {
                issues.Add($"Pose '{pose.Id}' thiếu DisplayName.");
            }
        }

        var animationIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (AnimationDefinition animation in project.Animations)
        {
            if (!Domain.Assets.AssetRules.IsValidId(animation.Id) || !animationIds.Add(animation.Id))
            {
                issues.Add($"Animation id không hợp lệ hoặc bị trùng: '{animation.Id}'.");
            }

            if (animation.Fps is < 1 or > 60)
            {
                issues.Add($"Animation '{animation.Id}' FPS phải trong 1..60.");
            }

            if (animation.Frames.Count == 0)
            {
                issues.Add($"Animation '{animation.Id}' chưa có frame nào.");
            }

            for (int i = 0; i < animation.Frames.Count; i++)
            {
                AnimationFrame frame = animation.Frames[i];
                if (frame.DurationTicks < 1)
                {
                    issues.Add($"Animation '{animation.Id}' frame {i} DurationTicks phải >= 1.");
                }

                if (!poseIds.Contains(frame.PoseId))
                {
                    issues.Add($"Animation '{animation.Id}' frame {i} trỏ tới pose không tồn tại '{frame.PoseId}'.");
                }

                foreach (FrameMarker marker in frame.Markers)
                {
                    if (string.IsNullOrWhiteSpace(marker.Type))
                    {
                        issues.Add($"Animation '{animation.Id}' frame {i} có marker rỗng.");
                    }
                }
            }
        }

        foreach (BehaviorDefinition behavior in project.Behaviors)
        {
            if (!Domain.Assets.AssetRules.IsValidId(behavior.Id))
            {
                issues.Add($"Behavior id không hợp lệ: '{behavior.Id}'.");
            }

            if (string.IsNullOrWhiteSpace(behavior.DisplayName))
            {
                issues.Add($"Behavior '{behavior.Id}' thiếu DisplayName.");
            }

            if (behavior.AnimationId is not null &&
                project.Animations.FirstOrDefault(a => a.Id == behavior.AnimationId) is null)
            {
                issues.Add($"Behavior '{behavior.Id}' trỏ tới animation không tồn tại '{behavior.AnimationId}'.");
            }

            if (behavior.HeldItemSlotId is not null &&
                !project.Rigs.Any(r => r.FindSlot(behavior.HeldItemSlotId) is not null))
            {
                issues.Add($"Behavior '{behavior.Id}' gắn held item slot không tồn tại '{behavior.HeldItemSlotId}'.");
            }

            if (behavior.HeldItemAssetId is not null &&
                project.Assets.FirstOrDefault(a => a.Id.Equals(behavior.HeldItemAssetId, StringComparison.Ordinal)) is null)
            {
                issues.Add($"Behavior '{behavior.Id}' gắn held item asset không tồn tại '{behavior.HeldItemAssetId}'.");
            }

            if (behavior.InteractionAnchorId is not null &&
                !project.Rigs.Any(r => r.FindAnchor(behavior.InteractionAnchorId) is not null))
            {
                issues.Add($"Behavior '{behavior.Id}' gắn interaction anchor không tồn tại '{behavior.InteractionAnchorId}'.");
            }

            // Markers are inert until an animation is bound, so an unbound
            // animation with markers is valid (behaviors are defined incrementally).
            AnimationDefinition? boundAnimation = behavior.AnimationId is null
                ? null
                : project.Animations.FirstOrDefault(a => a.Id == behavior.AnimationId);
            foreach (BehaviorMarker marker in behavior.Markers)
            {
                if (boundAnimation is not null)
                    if (marker.FrameIndex < 0 || marker.FrameIndex >= boundAnimation.Frames.Count)
                    {
                        issues.Add($"Behavior '{behavior.Id}' marker '{marker.Name}' frame index {marker.FrameIndex} ngoài phạm vi animation.");
                    }
            }
        }

        return issues;
    }
}
