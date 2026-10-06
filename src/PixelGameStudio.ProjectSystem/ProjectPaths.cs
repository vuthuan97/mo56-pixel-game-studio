namespace PixelGameStudio.ProjectSystem;

/// <summary>Standard on-disk layout of a project folder.</summary>
public static class ProjectPaths
{
    public const string ProjectFileName = "project.pgsproj";

    public const string AssetsFolderName = "assets";

    public const string AutosaveFolderName = "autosave";

    public static string ProjectFile(string rootDirectory) => Path.Combine(rootDirectory, ProjectFileName);

    public static string AssetsDirectory(string rootDirectory) => Path.Combine(rootDirectory, AssetsFolderName);

    public static string AutosaveDirectory(string rootDirectory) => Path.Combine(rootDirectory, AutosaveFolderName);

    public static string AutosaveFile(string rootDirectory) =>
        Path.Combine(AutosaveDirectory(rootDirectory), ProjectFileName);
}
