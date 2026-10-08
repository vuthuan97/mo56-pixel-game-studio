namespace PixelGameStudio.Domain.Animation;

/// <summary>Pure playback timing rules shared by preview and tests.</summary>
public static class AnimationPlayback
{
    public static double FrameDurationMilliseconds(AnimationDefinition animation, int frameIndex)
    {
        ArgumentNullException.ThrowIfNull(animation);
        if (animation.Fps <= 0 || frameIndex < 0 || frameIndex >= animation.Frames.Count)
        {
            throw new InvalidOperationException("Animation FPS hoặc frame index không hợp lệ.");
        }

        return 1000.0 * Math.Max(1, animation.Frames[frameIndex].DurationTicks) / animation.Fps;
    }

    public static (int FrameIndex, bool ContinuePlaying) Advance(AnimationDefinition animation, int currentFrameIndex)
    {
        ArgumentNullException.ThrowIfNull(animation);
        if (animation.Frames.Count == 0)
        {
            return (0, false);
        }

        int next = Math.Clamp(currentFrameIndex + 1, 0, animation.Frames.Count);
        if (next < animation.Frames.Count)
        {
            return (next, true);
        }

        return animation.Loop ? (0, true) : (animation.Frames.Count - 1, false);
    }
}
