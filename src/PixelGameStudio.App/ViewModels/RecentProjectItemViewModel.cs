using System.ComponentModel;
using System.Windows.Input;

namespace PixelGameStudio.App.ViewModels;

/// <summary>One row on the launcher: a recently opened project.</summary>
public sealed class RecentProjectItemViewModel
{
    public RecentProjectItemViewModel(
        string path, string name, string lastOpenedText, bool exists, ICommand openCommand, ICommand removeCommand)
    {
        Path = path;
        Name = name;
        LastOpenedText = lastOpenedText;
        Exists = exists;
        OpenCommand = openCommand;
        RemoveCommand = removeCommand;
    }

    public string Path { get; }

    public string Name { get; }

    public bool Exists { get; }

    public string LastOpenedText { get; }

    public ICommand OpenCommand { get; }

    public ICommand RemoveCommand { get; }
}
