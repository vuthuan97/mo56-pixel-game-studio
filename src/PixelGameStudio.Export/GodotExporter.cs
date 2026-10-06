using System.Text;

namespace PixelGameStudio.Export;

/// <summary>
/// Godot integration writer — ported from the legacy prototype's godot/exporter
/// (same .tres format 3, animation naming "&lt;animation&gt;_&lt;direction&gt;",
/// loop off for one-shot clips) but driven by the export folder layout of the
/// new pipeline: frames/{view}/{key}.png with the frame map from the manifest.
/// </summary>
public static class GodotExporter
{
    public static void WriteIntegration(
        string godotDirectory,
        string animationId,
        IReadOnlyList<string> views,
        int framesPerView,
        int fps,
        bool loop)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(godotDirectory);
        ArgumentNullException.ThrowIfNull(views);
        if (views.Count == 0 || framesPerView <= 0)
        {
            throw new ArgumentException("Need at least one view and one frame.");
        }

        Directory.CreateDirectory(godotDirectory);

        var ext = new StringBuilder();
        var entries = new StringBuilder();
        int resourceCount = 0;

        foreach (string view in views)
        {
            var frameRefs = new List<string>();
            for (int i = 0; i < framesPerView; i++)
            {
                resourceCount++;
                string key = $"{view.ToLowerInvariant()}_{i:00}";
                string id = $"{resourceCount}_{view}_{i}";
                ext.AppendLine($"[ext_resource type=\"Texture2D\" path=\"../frames/{view.ToLowerInvariant()}/{key}.png\" id=\"{id}\"]");
                frameRefs.Add($"ExtResource(\"{id}\")");
            }

            string frameList = string.Join(",\n", frameRefs.Select(r =>
                "{\n\"duration\": 1.0,\n\"texture\": " + r + "\n}"));
            entries.AppendLine("{\n\"frames\": [" + frameList + "],\n" +
                               $"\"loop\": {(loop ? "true" : "false")},\n" +
                               $"\"name\": &\"{animationId}_{view.ToLowerInvariant()}\",\n" +
                               $"\"speed\": {(double)fps}\n}}");
        }

        var tres = new StringBuilder();
        tres.AppendLine($"[gd_resource type=\"SpriteFrames\" load_steps={resourceCount + 1} format=3]");
        tres.AppendLine();
        tres.Append(ext.ToString());
        tres.AppendLine("[resource]");
        tres.Append("animations = [");
        tres.AppendLine(string.Join(",\n", entries.ToString().TrimEnd().Split("\n")));
        tres.AppendLine("]");

        File.WriteAllText(Path.Combine(godotDirectory, "sprite_frames.tres"), tres.ToString());

        var gdscript = new StringBuilder();
        gdscript.AppendLine("extends AnimatedSprite2D");
        gdscript.AppendLine();
        gdscript.AppendLine("func play_animation(animation: String, direction: String) -> void:");
        gdscript.AppendLine("    play(animation + \"_\" + direction)");
        gdscript.AppendLine();
        gdscript.AppendLine("func play_move(direction: String) -> void:");
        gdscript.AppendLine("    play(\"walk_\" + direction)");
        gdscript.AppendLine();
        gdscript.AppendLine("func play_idle(direction: String) -> void:");
        gdscript.AppendLine("    play(\"idle_\" + direction)");
        File.WriteAllText(Path.Combine(godotDirectory, "character_sprite_example.gd"), gdscript.ToString());
        File.WriteAllText(Path.Combine(godotDirectory, "README_GODOT.md"),
            "# Godot Export\n\n" +
            "Assign `sprite_frames.tres` to an AnimatedSprite2D. " +
            $"Animation names use `<animation>_<direction>` (here: `{animationId}_<direction>`). " +
            "Textures point at `../frames/<direction>/` relative to this folder.\n");
    }
}
