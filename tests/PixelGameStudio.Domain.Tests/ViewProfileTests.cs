using PixelGameStudio.Domain.Profiles;
using Xunit;

namespace PixelGameStudio.Domain.Tests;

public class ViewProfileTests
{
    [Fact]
    public void TopDown4_HasFourUniqueDirections()
    {
        ViewProfile view = ViewProfile.TopDown4();

        Assert.Equal(KnownPerspectives.TopDown4, view.Perspective);
        Assert.Equal(["Down", "Up", "Left", "Right"], view.Directions);
        Assert.Equal("Down", view.DefaultDirection);
    }

    [Fact]
    public void TopDown8_HasEightUniqueDirections()
    {
        ViewProfile view = ViewProfile.TopDown8();

        Assert.Equal(8, view.Directions.Count);
        Assert.Equal(view.Directions.Count, view.Directions.Distinct().Count());
    }

    [Fact]
    public void SideView2_UsesLeftRight()
    {
        ViewProfile view = ViewProfile.SideView2();

        Assert.Equal(KnownPerspectives.SideView2, view.Perspective);
        Assert.Equal(["Left", "Right"], view.Directions);
        Assert.Equal("Right", view.DefaultDirection);
    }

    [Fact]
    public void Custom_PreservesDirectionOrder()
    {
        ViewProfile view = ViewProfile.Custom(["A", "B", "C"], "B");

        Assert.Equal(KnownPerspectives.Custom, view.Perspective);
        Assert.Equal(["A", "B", "C"], view.Directions);
        Assert.Equal("B", view.DefaultDirection);
    }

    [Fact]
    public void Custom_RejectsEmptyDirections()
    {
        Assert.Throws<ArgumentException>(() => ViewProfile.Custom([], "A"));
    }

    [Fact]
    public void Custom_RejectsUnknownDefaultDirection()
    {
        Assert.Throws<ArgumentException>(() => ViewProfile.Custom(["A", "B"], "C"));
    }
}
