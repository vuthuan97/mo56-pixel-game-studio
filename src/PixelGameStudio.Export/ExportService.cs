using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;
using PixelGameStudio.Assets.Composition;
using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Animation;
using PixelGameStudio.Domain.Assets;
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

/// <summary>Results for a batch, one isolated package per character/animation.</summary>
public sealed record BatchExportResult(IReadOnlyList<ExportResult> Packages, IReadOnlyList<string> Skipped)
{
    public int FrameCount => Packages.Sum(package => package.FrameCount);
    public int SheetCount => Packages.Sum(package => package.SheetCount);
}

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

    public BatchExportResult ExportBatch(
        Project project,
        string projectRoot,
        IReadOnlyList<CharacterEntity> characters,
        IReadOnlyList<string?> animationIds,
        CharacterExportOptions options,
        string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(characters);
        ArgumentNullException.ThrowIfNull(animationIds);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        if (characters.Count == 0 || animationIds.Count == 0)
        {
            throw new InvalidOperationException("Cần chọn ít nhất một nhân vật và một hoạt ảnh để xuất.");
        }

        if (characters.Select(character => character.Id).Distinct(StringComparer.Ordinal).Count() != characters.Count ||
            animationIds.Distinct(StringComparer.Ordinal).Count() != animationIds.Count)
        {
            throw new InvalidOperationException("Danh sách nhân vật hoặc hoạt ảnh xuất có mục trùng.");
        }

        var plan = new List<(CharacterEntity Character, string? AnimationId)>();
        var skipped = new List<string>();
        // Only pair a generated action with its owner; shared animations are
        // exported for every selected character.
        foreach (CharacterEntity character in characters)
        {
            if (!AssetRules.IsValidId(character.Id) ||
                !project.Characters.Any(item => ReferenceEquals(item, character)))
            {
                throw new InvalidOperationException($"Nhân vật '{character.Id}' không thuộc project hiện tại.");
            }

            foreach (string? animationId in animationIds)
            {
                if (animationId is not null &&
                    (!AssetRules.IsValidId(animationId) ||
                     !project.Animations.Any(animation => animation.Id == animationId)))
                {
                    throw new InvalidOperationException($"Animation '{animationId}' không hợp lệ hoặc không tồn tại.");
                }

                if (animationId?.StartsWith("action.", StringComparison.Ordinal) == true &&
                    !IsOwnedActionAnimation(character, animationId))
                {
                    skipped.Add($"{character.Id}: bỏ qua '{animationId}' (thuộc nhân vật khác).");
                    continue;
                }

                plan.Add((character, animationId));
            }
        }

        if (plan.Count == 0)
        {
            throw new InvalidOperationException("Không có cặp nhân vật/hoạt ảnh tương thích để xuất.");
        }

        var packages = new List<ExportResult>();
        foreach ((CharacterEntity character, string? animationId) in plan)
        {
            string packageDirectory = Path.Combine(outputDirectory, "characters", character.Id,
                animationId is null ? "default-pose" : $"animation-{animationId}");
            var packageOptions = new CharacterExportOptions
            {
                Views = options.Views,
                AnimationId = animationId,
                IncludeFrames = options.IncludeFrames,
                IncludeSpritesheet = options.IncludeSpritesheet,
                IncludeLayers = options.IncludeLayers,
                IncludeGodot = options.IncludeGodot,
                IncludeManifest = options.IncludeManifest,
            };
            packages.Add(ExportCharacter(project, projectRoot, character, packageOptions, packageDirectory));
        }

        return new BatchExportResult(packages, skipped);
    }

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

        foreach (string view in views)
        {
            if (!project.View.Directions.Contains(view, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Direction '{view}' không thuộc ViewProfile của project.");
            }
        }

        AnimationDefinition? animation = options.AnimationId is null
            ? null
            : project.Animations.FirstOrDefault(a => a.Id == options.AnimationId)
              ?? throw new InvalidOperationException($"Animation '{options.AnimationId}' không tồn tại.");

        if (options.IncludeGodot && animation is not null && !options.IncludeFrames)
        {
            throw new InvalidOperationException("Gói Godot cần xuất từng frame PNG để tham chiếu sprite.");
        }

        if (animation is not null && animation.Id.StartsWith("action.", StringComparison.Ordinal) &&
            !IsOwnedActionAnimation(character, animation.Id))
        {
            throw new InvalidOperationException($"Animation '{animation.Id}' không thuộc nhân vật '{character.Id}'.");
        }

        // Validate the entire sequence before creating output files. A missing pose
        // must never silently become the character's default pose in the export.
        List<PoseDefinition>? animationPoses = null;
        if (animation is not null)
        {
            if (animation.Fps is < 1 or > 60 || animation.Frames.Count == 0)
            {
                throw new InvalidOperationException($"Animation '{animation.Id}' có FPS hoặc danh sách frame không hợp lệ.");
            }

            animationPoses = new List<PoseDefinition>(animation.Frames.Count);
            for (int i = 0; i < animation.Frames.Count; i++)
            {
                AnimationFrame frame = animation.Frames[i];
                if (frame.DurationTicks < 1)
                {
                    throw new InvalidOperationException($"Animation '{animation.Id}' frame {i} có DurationTicks không hợp lệ.");
                }

                PoseDefinition pose = project.Poses.FirstOrDefault(p => p.Id == frame.PoseId)
                    ?? throw new InvalidOperationException($"Animation '{animation.Id}' frame {i} thiếu pose '{frame.PoseId}'.");
                animationPoses.Add(pose);
            }
        }

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
                    PixelBuffer composed = _composer.Compose(project, projectRoot, character,
                        new CompositionOptions { View = view, Pose = animationPoses![i] });
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
                        Pose = animation is null ? null : animationPoses![i],
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
            GeneratedActionBinding? binding = character.GeneratedActionBindings
                .FirstOrDefault(item => item.TemplateId == actionId);
            string generatedId = CharacterActionIds.Animation(character.Id, actionId);
            AnimationDefinition? animation = project.Animations.FirstOrDefault(item => item.Id == binding?.AnimationId)
                ?? project.Animations.FirstOrDefault(item => item.Id == generatedId)
                ?? project.Animations.FirstOrDefault(item =>
                    item.Id == CharacterActionIds.LegacyAnimation(actionId));
            BehaviorDefinition? behavior = project.Behaviors.FirstOrDefault(item => item.Id == $"beh.{actionId}");
            return (object)new
            {
                id = actionId,
                animation = animation?.Id ?? behavior?.AnimationId,
                sourceFingerprint = binding?.SourceFingerprint ?? Fingerprint(new
                {
                    character.Build,
                    character.Appearance,
                    character.Equipment,
                    animation,
                }),
            };
        }).ToList();
    }

    private static bool IsOwnedActionAnimation(CharacterEntity character, string animationId)
    {
        return character.GeneratedActionBindings.Any(item => item.AnimationId == animationId) ||
            character.ActionIds.Any(actionId =>
            {
                string scoped = CharacterActionIds.Animation(character.Id, actionId);
                return animationId == scoped ||
                       animationId.StartsWith(scoped + ".v", StringComparison.Ordinal) ||
                       animationId == CharacterActionIds.LegacyAnimation(actionId);
            });
    }

    private static string Fingerprint(object value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, ManifestJsonOptions));
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }
}
