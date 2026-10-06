using System.IO;
using System.Text.Json;

namespace PixelGameStudio.App.Services;

public sealed record RecentProjectEntry(string Path, string Name, DateTime LastOpenedUtc);

/// <summary>
/// Persists the "recent projects" list shown on the launcher
/// (%APPDATA%/PixelGameStudio/recent-projects.json).
/// </summary>
public sealed class RecentProjectsStore
{
    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PixelGameStudio",
        "recent-projects.json");

    private const int MaxEntries = 15;

    public IReadOnlyList<RecentProjectEntry> Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return [];
            }

            string json = File.ReadAllText(FilePath);
            List<RecentProjectEntry>? entries = JsonSerializer.Deserialize<List<RecentProjectEntry>>(json);
            return entries is null ? [] : entries;
        }
        catch
        {
            // Corrupt or unreadable recents file must never block startup.
            return [];
        }
    }

    public void Upsert(string path, string name)
    {
        List<RecentProjectEntry> entries = [.. Load()];
        entries.RemoveAll(e => e.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
        entries.Insert(0, new RecentProjectEntry(path, name, DateTime.UtcNow));
        Save(entries);
    }

    public void Remove(string path)
    {
        List<RecentProjectEntry> entries = [.. Load()];
        entries.RemoveAll(e => e.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
        Save(entries);
    }

    private void Save(List<RecentProjectEntry> entries)
    {
        try
        {
            entries.Sort((a, b) => b.LastOpenedUtc.CompareTo(a.LastOpenedUtc));
            if (entries.Count > MaxEntries)
            {
                entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(entries, new JsonSerializerOptions
            {
                WriteIndented = true,
            }));
        }
        catch
        {
            // Recents persistence is best-effort.
        }
    }
}
