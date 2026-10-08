using PixelGameStudio.Domain.Animation;
using Xunit;

namespace PixelGameStudio.Domain.Tests;

public class AnimationPlaybackTests
{
    [Fact]
    public void FrameDuration_UsesTicksAtAnimationFps()
    {
        AnimationDefinition animation = new()
        {
            Fps = 8,
            Frames = [new AnimationFrame { PoseId = "pose.idle", DurationTicks = 3 }],
        };

        Assert.Equal(375, AnimationPlayback.FrameDurationMilliseconds(animation, 0));
        Assert.Equal(0.375, animation.DurationSeconds, 3);
    }

    [Fact]
    public void NonLoopPlayback_StopsOnLastFrame_WhileLoopWraps()
    {
        AnimationDefinition animation = new()
        {
            Loop = false,
            Frames = [new AnimationFrame(), new AnimationFrame()],
        };

        Assert.Equal((1, true), AnimationPlayback.Advance(animation, 0));
        Assert.Equal((1, false), AnimationPlayback.Advance(animation, 1));
        animation.Loop = true;
        Assert.Equal((0, true), AnimationPlayback.Advance(animation, 1));
    }
}
