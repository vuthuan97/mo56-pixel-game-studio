using PixelGameStudio.Domain.Animation;

namespace PixelGameStudio.Domain.Templates;

/// <summary>Preset animations ported from the legacy prototype as pure data (frames + markers + loop rules).</summary>
public static class AnimationPresets
{
    public static IReadOnlyList<AnimationDefinition> All => BuildAll().Select(a => a.Clone()).ToList();

    private static IReadOnlyList<AnimationDefinition> BuildAll() =>
    [
        Simple("idle", "Đứng", ["idle_0", "idle_1"]),
        Simple("walk", "Đi", ["walk_0", "walk_1", "walk_2", "walk_3"],
            footstepsOn: [1, 3]),
        Simple("attack", "Tấn công", ["attack_0", "attack_1", "attack_2", "attack_3"],
            frameMarkers: new[] { (2, FrameMarkerTypes.Hit, (string?)"hit"), (2, FrameMarkerTypes.Sound, "slash") }),
        Simple("hurt", "Bị đánh", ["hurt_0", "hurt_1"]),
        Simple("cast", "Ném kỹ năng", ["cast_0", "cast_1"]),
        Simple("death", "Chết", ["death_0", "death_1"], loop: false),
    ];

    public static AnimationDefinition? Find(string id) => BuildAll().FirstOrDefault(a => a.Id == id)?.Clone();

    private static AnimationDefinition Simple(
        string id,
        string displayName,
        IReadOnlyList<string> poseIds,
        bool loop = true,
        int[]? footstepsOn = null,
        (int Frame, string Type, string? Value)[]? frameMarkers = null)
    {
        var animation = new AnimationDefinition
        {
            Id = id,
            DisplayName = displayName,
            Loop = loop,
        };
        for (int i = 0; i < poseIds.Count; i++)
        {
            var frame = new AnimationFrame { PoseId = poseIds[i] };
            if (footstepsOn is not null && footstepsOn.Contains(i))
            {
                frame.Markers.Add(new FrameMarker(FrameMarkerTypes.Footstep));
            }

            if (frameMarkers is not null)
            {
                foreach ((int markerFrame, string type, string? value) in frameMarkers)
                {
                    if (markerFrame == i)
                    {
                        frame.Markers.Add(new FrameMarker(type, value));
                    }
                }
            }

            animation.Frames.Add(frame);
        }

        return animation;
    }
}
