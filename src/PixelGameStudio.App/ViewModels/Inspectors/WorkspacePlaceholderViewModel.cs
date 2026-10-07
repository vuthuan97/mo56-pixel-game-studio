namespace PixelGameStudio.App.ViewModels.Inspectors;

/// <summary>Honest placeholder for a top-level workspace not implemented yet.</summary>
public sealed class WorkspacePlaceholderViewModel
{
    public WorkspacePlaceholderViewModel(string title, string description)
    {
        Title = title;
        Description = description;
    }

    public string Title { get; }

    public string Description { get; }
}
