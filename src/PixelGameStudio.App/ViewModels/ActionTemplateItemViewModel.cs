using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Media.Imaging;

namespace PixelGameStudio.App.ViewModels;

public partial class ActionTemplateItemViewModel : ObservableObject
{
    public ActionTemplateItemViewModel(string id, string displayName, string group, bool isAvailable,
        string reason, bool isSelected = false)
    {
        Id = id;
        DisplayName = displayName;
        Group = group;
        IsAvailable = isAvailable;
        Reason = reason;
        IsSelected = isSelected;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string Group { get; }
    public bool IsAvailable { get; }
    public string Reason { get; }

    public WriteableBitmap? Thumbnail { get; set; }

    [ObservableProperty]
    private bool _isSelected;
}
