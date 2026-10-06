using CommunityToolkit.Mvvm.ComponentModel;

namespace PixelGameStudio.App.ViewModels;

/// <summary>One frame cell in the animation timeline strip.</summary>
public partial class FrameItemViewModel : ObservableObject
{
    public FrameItemViewModel(int index, string poseId, int durationTicks, string markers)
    {
        Index = index;
        PoseId = poseId;
        DurationTicks = durationTicks;
        Markers = markers;
        HasMarkers = markers.Length > 0;
        IsCurrent = index == 0;
    }

    public int Index { get; }

    public string PoseId { get; }

    public int DurationTicks { get; }

    public string Markers { get; }

    public bool HasMarkers { get; }

    [ObservableProperty]
    private bool _isCurrent;
}

/// <summary>One row in the behavior library list.</summary>
public class BehaviorItemViewModel
{
    public BehaviorItemViewModel(string id, string displayName, string group, string binding)
    {
        Id = id;
        DisplayName = displayName;
        Group = group;
        Binding = binding;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public string Group { get; }

    public string Binding { get; }
}
