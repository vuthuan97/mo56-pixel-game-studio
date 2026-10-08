using CommunityToolkit.Mvvm.ComponentModel;

namespace PixelGameStudio.App.ViewModels;

/// <summary>One explicit export choice; selection never depends on the preview's active character.</summary>
public partial class ExportChoiceItemViewModel : ObservableObject
{
    public ExportChoiceItemViewModel(string? id, string displayName, bool isSelected = false)
    {
        Id = id;
        DisplayName = displayName;
        IsSelected = isSelected;
    }

    public string? Id { get; }
    public string DisplayName { get; }

    [ObservableProperty]
    private bool _isSelected;
}
