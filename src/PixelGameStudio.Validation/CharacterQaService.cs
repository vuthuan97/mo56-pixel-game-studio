using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Character;
using PixelGameStudio.Assets;
using PixelGameStudio.Assets.Composition;
using PixelGameStudio.Rendering;

namespace PixelGameStudio.Validation;

/// <summary>
/// Phase 10 Art QA extensions beyond the legacy analyzer: pixel noise and
/// per-character coverage checks (missing views, broken assets, decode).
/// </summary>
public static class CharacterQaService
{
    /// <summary>
    /// Pixel noise = share of opaque pixels whose 4-neighborhood contains no
    /// pixel of the same RGBA color. Solid shapes score ~0; scattered single
    /// pixels approach 1. Legacy-style readability heuristic for native 1x.
    /// </summary>
    public static double MeasurePixelNoise(PixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        int opaque = 0;
        int isolated = 0;
        for (int y = 0; y < buffer.Height; y++)
        {
            for (int x = 0; x < buffer.Width; x++)
            {
                var p = buffer[x, y];
                if (p.A == 0)
                {
                    continue;
                }

                opaque++;
                bool hasSameNeighbor =
                    (x > 0 && buffer[x - 1, y].SameColor(p)) ||
                    (x + 1 < buffer.Width && buffer[x + 1, y].SameColor(p)) ||
                    (y > 0 && buffer[x, y - 1].SameColor(p)) ||
                    (y + 1 < buffer.Height && buffer[x, y + 1].SameColor(p));
                if (!hasSameNeighbor)
                {
                    isolated++;
                }
            }
        }

        return opaque == 0 ? 0 : (double)isolated / opaque;
    }

    /// <summary>Full QA of one character: project rules, on-disk assets, direction coverage, composed-frame noise/contrast.</summary>
    public static IReadOnlyList<string> CheckCharacter(
        Project project,
        string projectRoot,
        CharacterEntity character,
        AssetLibraryService library,
        RigSpriteComposer composer)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(library);
        ArgumentNullException.ThrowIfNull(composer);

        var issues = new List<string>();

        // 1) direction coverage per part: a part bound to view assets must cover
        //    every direction of the ViewProfile (parts with a view-independent
        //    AssetRef are exempt).
        foreach (PartAppearance appearance in character.Appearance)
        {
            if (appearance.ViewAssets.Count == 0 || appearance.AssetRef is not null)
            {
                continue;
            }

            foreach (string direction in project.View.Directions)
            {
                if (!appearance.ViewAssets.ContainsKey(direction))
                {
                    issues.Add($"Part '{appearance.PartId}' thiếu art cho view '{direction}'.");
                }
            }
        }

        // 2) composed frames: decode already guaranteed in memory; run per-frame
        //    readability checks (contrast + noise) at native 1x.
        if (project.Rigs.All(r => r.Id != character.RigId))
        {
            issues.Add($"Character '{character.Id}' trỏ tới rig không tồn tại '{character.RigId}'.");
            return issues;
        }

        try
        {
            PixelBuffer composed = composer.Compose(project, projectRoot, character,
                new CompositionOptions { View = project.View.DefaultDirection, ApplyOutline = false });
            SpriteAnalysis analysis = SpriteAnalyzer.Analyze(composed);
            double noise = MeasurePixelNoise(composed);
            if (analysis.ValueRange < project.Style.MinValueRange)
            {
                issues.Add($"Value range {analysis.ValueRange} < ngưỡng {project.Style.MinValueRange} của StyleProfile (khó đọc ở native 1x).");
            }

            if (noise > 0.25)
            {
                issues.Add($"Pixel noise cao ({noise:0.00}) — nhiều pixel đơn lẻ, kiểm tra native 1x.");
            }

            if (!project.Style.TransparentBackground && composed[0, 0].A == 0)
            {
                issues.Add("StyleProfile yêu cầu nền đặc nhưng sprite đang trong suốt.");
            }
        }
        catch (Exception ex)
        {
            issues.Add($"Không compose được character: {ex.Message}");
        }

        return issues;
    }
}
