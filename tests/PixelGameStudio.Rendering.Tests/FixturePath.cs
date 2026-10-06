using System.Text.Json;

namespace PixelGameStudio.Rendering.Tests;

/// <summary>Locates the golden fixtures copied to the test output directory.</summary>
public static class FixturePath
{
    public static string Root { get; } = FindFixtureRoot();

    public static string Primitives => Path.Combine(Root, "primitives");

    public static string Sprites => Path.Combine(Root, "sprites");

    public static string CompositeCases => Path.Combine(Root, "composite_cases.json");

    public static JsonDocument LoadJson(string path)
    {
        using var stream = File.OpenRead(path);
        return JsonDocument.Parse(stream);
    }

    private static string FindFixtureRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            string candidate = Path.Combine(dir.FullName, "Fixtures", "sprites", "manifest.json");
            if (File.Exists(candidate))
            {
                return Path.Combine(dir.FullName, "Fixtures");
            }

            dir = dir.Parent!;
        }

        throw new InvalidOperationException("Fixtures directory not found; build the test project.");
    }
}
