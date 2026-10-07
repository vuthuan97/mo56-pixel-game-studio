namespace PixelGameStudio.App.ViewModels;

public sealed class ActionTemplateItemViewModel
{
    public ActionTemplateItemViewModel(string id, string displayName, string group, bool isAvailable, string reason)
    {
        Id = id;
        DisplayName = displayName;
        Group = group;
        IsAvailable = isAvailable;
        Reason = reason;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string Group { get; }
    public bool IsAvailable { get; }
    public string Reason { get; }
}
