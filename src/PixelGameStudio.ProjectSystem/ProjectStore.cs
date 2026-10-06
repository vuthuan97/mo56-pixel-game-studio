using System.Text.Json;
using System.Text.Json.Serialization;
using PixelGameStudio.Domain;

namespace PixelGameStudio.ProjectSystem;

/// <summary>Raised when a project file cannot be opened or deserialized.</summary>
public sealed class ProjectStoreException : Exception
{
    public ProjectStoreException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}

/// <summary>
/// Persistence for <see cref="Project"/>: New/Open/Save plus autosave and
/// recovery. A project is a folder containing <c>project.pgsproj</c> (JSON),
/// an <c>assets/</c> folder for binary payloads (Phase 2+) and an
/// <c>autosave/</c> folder for the recovery copy. JSON never embeds binary
/// image data — assets are files referenced by the library.
/// </summary>
public sealed class ProjectStore
{
    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Serializes a project to JSON (exposed for tests and export tooling).</summary>
    public string Serialize(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return JsonSerializer.Serialize(project, JsonOptions);
    }

    /// <summary>Deserializes a project from JSON (exposed for tests and import tooling).</summary>
    public Project Deserialize(string json)
    {
        Project? project;
        try
        {
            project = JsonSerializer.Deserialize<Project>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new ProjectStoreException("File project không phải JSON hợp lệ.", ex);
        }

        return project ?? throw new ProjectStoreException("File project rỗng hoặc sai định dạng.");
    }

    /// <summary>Saves the project into <paramref name="rootDirectory"/> (atomic write) and clears stale autosave.</summary>
    public string Save(Project project, string rootDirectory)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);

        Directory.CreateDirectory(rootDirectory);
        Directory.CreateDirectory(ProjectPaths.AutosaveDirectory(rootDirectory));
        Directory.CreateDirectory(ProjectPaths.AssetsDirectory(rootDirectory));

        project.ModifiedAtUtc = DateTime.UtcNow;
        string target = ProjectPaths.ProjectFile(rootDirectory);
        WriteAtomically(target, Serialize(project));

        // A fresh main save supersedes any recovery copy.
        string autosave = ProjectPaths.AutosaveFile(rootDirectory);
        if (File.Exists(autosave))
        {
            File.Delete(autosave);
        }

        return target;
    }

    /// <summary>Opens the project stored in <paramref name="rootDirectory"/>.</summary>
    public Project Open(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        string file = ProjectPaths.ProjectFile(rootDirectory);
        if (!File.Exists(file))
        {
            throw new ProjectStoreException($"Không tìm thấy {ProjectPaths.ProjectFileName} trong '{rootDirectory}'.");
        }

        return LoadFile(file);
    }

    /// <summary>Writes the recovery copy under <c>autosave/</c>. Does not touch the main file.</summary>
    public string Autosave(Project project, string rootDirectory)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);

        string dir = ProjectPaths.AutosaveDirectory(rootDirectory);
        Directory.CreateDirectory(dir);
        project.ModifiedAtUtc = DateTime.UtcNow;
        string target = ProjectPaths.AutosaveFile(rootDirectory);
        WriteAtomically(target, Serialize(project));
        return target;
    }

    /// <summary>
    /// Returns the autosaved project when it is the freshest copy (main file
    /// missing, or autosave written after the last main save). Returns false
    /// when there is nothing to recover.
    /// </summary>
    public bool TryRecover(string rootDirectory, out Project? recovered)
    {
        recovered = null;
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);

        string autosave = ProjectPaths.AutosaveFile(rootDirectory);
        if (!File.Exists(autosave))
        {
            return false;
        }

        string main = ProjectPaths.ProjectFile(rootDirectory);
        if (File.Exists(main) &&
            File.GetLastWriteTimeUtc(main) >= File.GetLastWriteTimeUtc(autosave))
        {
            return false;
        }

        try
        {
            recovered = LoadFile(autosave);
            return true;
        }
        catch (ProjectStoreException)
        {
            return false; // corrupt autosave must not block opening the main project
        }
    }

    private Project LoadFile(string file)
    {
        string json;
        try
        {
            json = File.ReadAllText(file);
        }
        catch (IOException ex)
        {
            throw new ProjectStoreException($"Không đọc được file '{file}'.", ex);
        }

        Project project = Deserialize(json);
        if (project.SchemaVersion > Project.CurrentSchemaVersion)
        {
            throw new ProjectStoreException(
                $"Project dùng schema v{project.SchemaVersion}, mới hơn mức ứng dụng hỗ trợ (v{Project.CurrentSchemaVersion}).");
        }

        return project;
    }

    private static void WriteAtomically(string target, string content)
    {
        string temp = target + ".tmp";
        File.WriteAllText(temp, content);
        File.Move(temp, target, overwrite: true);
    }
}
