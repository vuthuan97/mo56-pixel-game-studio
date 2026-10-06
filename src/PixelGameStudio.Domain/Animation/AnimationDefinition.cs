namespace PixelGameStudio.Domain.Animation;

/// <summary>Well-known frame marker types (docs/CHARACTER_AND_BEHAVIOR_SPEC.md).</summary>
public static class FrameMarkerTypes
{
    public const string Footstep = "Footstep";
    public const string Sound = "Sound";
    public const string Hit = "Hit";
    public const string SpawnEffect = "SpawnEffect";
    public const string SpawnProjectile = "SpawnProjectile";
    public const string Interaction = "Interaction";
    public const string PickUp = "PickUp";
    public const string Drop = "Drop";
    public const string Custom = "Custom";
}

/// <summary>One event marker attached to an animation frame.</summary>
public sealed record FrameMarker(string Type, string? Value = null);

/// <summary>One frame of an animation: which pose to show, for how long, with which markers.</summary>
public sealed class AnimationFrame
{
    public string PoseId { get; set; } = string.Empty;

    /// <summary>Duration in beats (1 = one tick at the animation FPS).</summary>
    public int DurationTicks { get; set; } = 1;

    public List<FrameMarker> Markers { get; set; } = [];
}

/// <summary>
/// An animation = an ordered list of frames (pose + duration + markers),
/// playback speed and looping. Frames may reference any PoseDefinition in the
/// project; reordering/copying frames is data manipulation (Phase 7 editor).
/// </summary>
public sealed class AnimationDefinition
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public int Fps { get; set; } = 6;

    public bool Loop { get; set; } = true;

    public List<AnimationFrame> Frames { get; set; } = [];

    /// <summary>Total length in seconds (durations at the animation FPS).</summary>
    public double DurationSeconds => Frames.Count == 0 || Fps <= 0
        ? 0
        : Frames.Sum(f => f.DurationTicks) / (double)Fps;

    /// <summary>Deep copy — presets return clones so editor mutations never touch shared template data.</summary>
    public AnimationDefinition Clone()
    {
        var copy = new AnimationDefinition { Id = Id, DisplayName = DisplayName, Fps = Fps, Loop = Loop };
        foreach (AnimationFrame frame in Frames)
        {
            copy.Frames.Add(new AnimationFrame
            {
                PoseId = frame.PoseId,
                DurationTicks = frame.DurationTicks,
                Markers = frame.Markers.Select(m => new FrameMarker(m.Type, m.Value)).ToList(),
            });
        }

        return copy;
    }
}
