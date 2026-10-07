using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;
using PixelGameStudio.Assets.Composition;
using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Animation;
using PixelGameStudio.Domain.Behaviors;
using PixelGameStudio.Domain.Character;
using PixelGameStudio.Rendering;

namespace PixelGameStudio.Export;

/// <summary>Export options for one character.</summary>
public sealed class CharacterExportOptions
{
    /// <summary>Views (direction keys) to export; empty = all project directions.</summary>
    public IReadOnlyList<string> Views { get; set; } = [];

    /// <summary>Animation whose frames become the exported spritesheet; null = single default pose.</summary>
    public string? AnimationId { get; set; }

    public bool IncludeFrames { get; set; } = true;

    public bool IncludeSpritesheet { get; set; } = true;

    /// <summary>Per-frame layer PNGs (asset-driven equivalent of the legacy layer export).</summary>
    public bool IncludeLayers { get; set; }

    /// <summary>Godot integration: sprite_frames.tres + example gdscript + readme.</summary>
    public bool IncludeGodot { get; set; }

    /// <summary>Manifest carries the frame map (the legacy exporter's missing piece, audit §9).</summary>
    public bool IncludeManifest { get; set; } = true;
}

/// <summary>Summary of one export run.</summary>
public sealed record ExportResult(string OutputDirectory, int FrameCount, int SheetCount);

/// <summary>
/// Exports a composed character to game-ready files: per-frame PNGs, packed
/// spritesheets (via PixelPrimitives.SpritesheetPack, with an explicit frame
/// map in the manifest) and a manifest.json describing everything.
/// </summary>
public sealed class ExportService
{
    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly RigSpriteComposer _composer;

    public ExportService(RigSpriteComposer composer) => _composer = composer;

    public ExportResult ExportCharacter(
        Project project,
        string projectRoot,
        CharacterEntity character,
        CharacterExportOptions options,
        string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        string[] views = options.Views.Count > 0
            ? [.. options.Views]
            : [.. project.View.Directions];

        if (views.Length == 0)
        {
            throw new InvalidOperationException("ViewProfile của project không có direction nào để export.");
        }

        AnimationDefinition? animation = options.AnimationId is null
            ? null
            : project.Animations.FirstOrDefault(a => a.Id == options.AnimationId)
              ?? throw new InvalidOperationException($"Animation '{options.AnimationId}' không tồn tại.");

        Directory.CreateDirectory(outputDirectory);
        int frameCount = 0;
        int sheetCount = 0;
        var manifest = new Dictionary<string, object?>
        {
            ["character"] = new { character.Id, character.Name, rig = character.RigId },
            ["actions"] = BuildActionManifest(project, character),
            ["view"] = project.View.Perspective,
            ["directions"] = views,
            ["animation"] = animation?.Id,
            ["fps"] = animation?.Fps,
            ["loop"] = animation?.Loop,
            ["sheets"] = new List<object>(),
        };

        foreach (string view in views)
        {
            var frames = new List<(string Key, PixelBuffer Frame)>();
            if (animation is null)
            {
                PixelBuffer composed = _composer.Compose(project, projectRoot, character,
                    new CompositionOptions { View = view });
                frames.Add(($"{view.ToLowerInvariant()}_0", composed));
            }
            else
            {
                for (int i = 0; i < animation.Frames.Count; i++)
                {
                    string poseId = animation.Frames[i].PoseId;
                    PoseDefinition? pose = project.Poses.FirstOrDefault(p => p.Id == poseId);
                    PixelBuffer composed = _composer.Compose(project, projectRoot, character,
                        new CompositionOptions { View = view, Pose = pose });
                    frames.Add(($"{view.ToLowerInvariant()}_{i:00}", composed));
                }
            }

            if (options.IncludeFrames)
            {
                string framesDir = Path.Combine(outputDirectory, "frames", view.ToLowerInvariant());
                Directory.CreateDirectory(framesDir);
                foreach ((string key, PixelBuffer frame) in frames)
                {
                    using (Stream output = File.Create(Path.Combine(framesDir, $"{key}.png")))
                    {
                        PngCodec.Encode(frame, output);
                    }

                    frameCount++;
                }
            }

            if (options.IncludeSpritesheet)
            {
                SpritesheetPackResult pack = PixelPrimitives.SpritesheetPack(frames);
                string sheetPath = Path.Combine(outputDirectory, $"spritesheet_{view.ToLowerInvariant()}.png");
                using (Stream output = File.Create(sheetPath))
                {
                    PngCodec.Encode(pack.Sheet, output);
                }

                sheetCount++;
                ((List<object>)manifest["sheets"]!).Add(new
                {
                    file = Path.GetFileName(sheetPath),
                    view,
                    cellWidth = pack.CellWidth,
                    cellHeight = pack.CellHeight,
                    frames = pack.Cells.Select(c => new { key = c.Key, column = c.Column, row = c.Row }).ToList(),
                });
            }

            if (options.IncludeLayers)
            {
                for (int i = 0; i < frames.Count; i++)
                {
                    CompositionOptions layerOptions = new()
                    {
                        View = view,
                        Pose = animation is null ? null : project.Poses.FirstOrDefault(p => p.Id == animation.Frames[i].PoseId),
                        ApplyOutline = false,
                    };
                    IReadOnlyList<(string Name, PixelBuffer Image)> layers =
                        _composer.ComposeLayers(project, projectRoot, character, layerOptions);
                    string layerDir = Path.Combine(outputDirectory, "layers", view.ToLowerInvariant(), frames[i].Key);
                    Directory.CreateDirectory(layerDir);
                    foreach ((string name, PixelBuffer image) in layers)
                    {
                        using (Stream output = File.Create(Path.Combine(layerDir, $"{name}.png")))
                        {
                            PngCodec.Encode(image, output);
                        }
                    }
                }
            }
        }

        WritePackageJson(project, character, animation, views, options, outputDirectory);

        if (options.IncludeGodot && animation is not null)
        {
            GodotExporter.WriteIntegration(
                Path.Combine(outputDirectory, "godot"),
                animationId: animation.Id,
                views: views,
                framesPerView: animation.Frames.Count,
                fps: animation.Fps,
                loop: animation.Loop);
        }

        if (options.IncludeManifest)
        {
            string manifestPath = Path.Combine(outputDirectory, "manifest.json");
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, ManifestJsonOptions));
        }

        return new ExportResult(outputDirectory, frameCount, sheetCount);
    }

    /// <summary>
    /// Character package metadata: the JSON counterpart of everything in the
    /// export folder (supersedes the legacy package.json which mixed UI state).
    /// </summary>
    private static void WritePackageJson(
        Project project,
        CharacterEntity character,
        AnimationDefinition? animation,
        IReadOnlyList<string> views,
        CharacterExportOptions options,
        string outputDirectory)
    {
        var package = new Dictionary<string, object?>
        {
            ["schemaVersion"] = 2,
            ["character"] = new
            {
                character.Id,
                character.Name,
                rig = character.RigId,
                equipment = character.Equipment.Select(e => new { slot = e.SlotId, asset = e.AssetId }).ToList(),
                build = character.Build,
                actions = character.ActionIds,
            },
            ["actions"] = BuildActionManifest(project, character),
            ["view"] = new { perspective = project.View.Perspective, directions = views },
            ["animation"] = animation is null
                ? null
                : new
                {
                    id = animation.Id,
                    fps = animation.Fps,
                    loop = animation.Loop,
                    frames = animation.Frames.Select(f => new
                    {
                        pose = f.PoseId,
                        durationTicks = f.DurationTicks,
                        markers = f.Markers.Select(m => new { type = m.Type, value = m.Value }).ToList(),
                    }).ToList(),
                },
            ["style"] = new
            {
                project.Style.OutlineStyle,
                project.Style.OutlineColorHex,
                project.Style.LightDirection,
                project.Style.NearestNeighborOnly,
            },
            ["included"] = new { options.IncludeFrames, options.IncludeSpritesheet, options.IncludeLayers, options.IncludeGodot },
        };

        File.WriteAllText(
            Path.Combine(outputDirectory, "package.json"),
            JsonSerializer.Serialize(package, ManifestJsonOptions));
    }

    private static IReadOnlyList<object> BuildActionManifest(Project project, CharacterEntity character)
    {
        return character.ActionIds.Select(actionId =>
        {
            string generatedId = $"action.{actionId}";
            AnimationDefinition? animation = project.Animations.FirstOrDefault(item => item.Id == generatedId);
            BehaviorDefinition? behavior = project.Behaviors.FirstOrDefault(item => item.Id == $"beh.{actionId}");
            return (object)new
            {
                id = actionId,
                animation = animation?.Id ?? behavior?.AnimationId,
                sourceFingerprint = Fingerprint(new
                {
                    character.Build,
                    character.Appearance,
                    character.Equipment,
                    animation,
                }),
            };
        }).ToList();
    }

    private static string Fingerprint(object value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, ManifestJsonOptions));
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }
}
