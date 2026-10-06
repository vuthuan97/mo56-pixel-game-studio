using System.Globalization;
using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Assets;
using PixelGameStudio.Domain.Character;
using PixelGameStudio.Domain.Templates;
using PixelGameStudio.Rendering;
using PixelGameStudio.Rendering.Procedural;

namespace PixelGameStudio.Assets.Composition;

/// <summary>Summary of what the factory generated.</summary>
public sealed record StarterContentResult(int AssetCount, string CharacterId, string RigId);

/// <summary>
/// Builds starter content inside a project: the legacy procedural renderer is
/// used ONLY as a template/placeholder art source (rule 05_assets_equipment) —
/// each rig part, variant and equipment item is exported to a real PNG asset
/// per view, then wired through RigDefinition + Appearance + Equipment as pure
/// data. From there the asset-driven composer renders.
/// </summary>
public static class StarterContentFactory
{
    private static readonly string[] Views = ["Down", "Up", "Left", "Right"];

    private static readonly string[] HandStates = ["down", "up", "forward", "back"];
    private static readonly string[] LegStates = ["neutral", "forward", "back"];

    private static readonly Dictionary<string, string> PartLayerNames = new()
    {
        ["torso"] = "body",
        ["head"] = "head",
        ["face"] = "face",
        ["hair_front"] = "hair_front",
        ["hair_back"] = "hair_back",
        ["arm_left"] = "left_arm",
        ["arm_right"] = "right_arm",
        ["leg_left"] = "left_leg",
        ["leg_right"] = "right_leg",
        ["weapon"] = "weapon_back",
    };

    private static readonly Dictionary<string, string> SlotAssetIds = new()
    {
        ["main_hand"] = "Kiếm",
        ["off_hand"] = "Không",
        ["head"] = "Không",
        ["inner"] = "Không",
        ["outer"] = "Kiếm tu",
        ["chest_armor"] = "Không",
        ["shoulder"] = "Không",
        ["gloves"] = "Không",
        ["pants"] = "Quần tối",
        ["boots"] = "Giày da",
        ["belt"] = "Đai ngọc",
        ["cape"] = "Không",
        ["accessory"] = "Ngọc bội",
    };

    /// <summary>
    /// Generates the placeholder art library (base body parts, pose-state and
    /// appearance variants, equipment items) and wires ONE target character
    /// with the bare base look: no clothes, no equipment — everything else is
    /// added by the user afterwards. When <paramref name="character"/> is null a
    /// default "char.hero" is created. When <paramref name="generateFiles"/> is
    /// false the existing asset library is reused and only the character wiring
    /// runs. Safe to call repeatedly.
    /// </summary>
    public static StarterContentResult CreateBaseBody(
        Project project, string projectRoot, AssetLibraryService library,
        CharacterEntity? character = null, bool generateFiles = true)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentNullException.ThrowIfNull(library);

        RigDefinition rig = project.Rigs.FirstOrDefault(r => r.Id == RigTemplates.HumanoidTopDownId)
            ?? RigTemplates.HumanoidTopDown4();
        if (!project.Rigs.Contains(rig))
        {
            project.Rigs.Add(rig);
        }

        int assetCount = 0;
        ICharacterLayerRenderer renderer = project.Style.CharacterRenderer.Equals("ReferenceGrid", StringComparison.OrdinalIgnoreCase)
            ? new ReferenceGridSpriteRenderer()
            : new LegacySpriteRenderer();
        if (project.Style.CharacterRenderer.Equals("ReferenceGrid", StringComparison.OrdinalIgnoreCase))
        {
            project.Style.CharacterRendererVersion = 2;
        }

        if (generateFiles)
        {
            // Base body parts per view (procedural render → placeholder asset).
            foreach (string view in Views)
            {
                LegacySpriteSpec spec = NeutralSpec();
                spec.Direction = view;
                List<(string Name, PixelBuffer Image)> layers = renderer.RenderLayers(spec, LegacyPosePresets.Poses["idle_0"]);
                foreach (KeyValuePair<string, string> kv in PartLayerNames)
                {
                    PixelBuffer layer = layers.First(l => l.Name == kv.Value).Image;
                    if (!HasOpaquePixels(layer))
                    {
                        continue;
                    }

                    SaveAsset(project, projectRoot, library, $"part.{kv.Key}.{Slug(view)}",
                        $"Part {kv.Key} ({view})", AssetTypes.Sprite, layer, [view], [$"part:{kv.Key}"]);
                    assetCount++;
                }
            }

            // Arm/leg pose-state variant art (down/up/forward/back…) so animation
            // presets can move limbs.
            foreach ((string partId, string stateKey, string[] values) in new[]
                     {
                         (ValueTuple.Create("arm_left", "hand", HandStates)),
                         ("arm_right", "hand", HandStates),
                         ("leg_left", "leg", LegStates),
                         ("leg_right", "leg", LegStates),
                     })
            {
                foreach (string view in Views)
                {
                    foreach (string value in values)
                    {
                        LegacySpriteSpec spec = NeutralSpec();
                        spec.Direction = view;
                        var pose = new global::PixelGameStudio.Rendering.Procedural.LegacyPose("state");
                        if (stateKey == "hand")
                        {
                            pose = partId.Contains("left")
                                ? pose with { LeftArm = value }
                                : pose with { RightArm = value };
                        }
                        else
                        {
                            pose = partId.Contains("left")
                                ? pose with { LeftLeg = value }
                                : pose with { RightLeg = value };
                        }

                        List<(string Name, PixelBuffer Image)> layers = renderer.RenderLayers(spec, pose);
                        PixelBuffer layer = layers.First(l => l.Name == (partId.Contains("arm")
                            ? (partId == "arm_left" ? "left_arm" : "right_arm")
                            : (partId == "leg_left" ? "left_leg" : "right_leg"))).Image;
                        if (!HasOpaquePixels(layer))
                        {
                            continue;
                        }

                        SaveAsset(project, projectRoot, library, $"part.{partId}.{value}.{Slug(view)}",
                            $"Part {partId} {value} ({view})", AssetTypes.Sprite, layer, [view],
                            [$"part:{partId}", "state"]);
                        assetCount++;
                    }
                }
            }

            // Appearance variants: body build, eye/mouth, hair styles.
            void GenerateAppearanceVariants(
                string partId, string legacyLayer, IReadOnlyList<string> variantNames,
                Action<LegacySpriteSpec, string> applyVariant)
            {
                foreach (string variantName in variantNames)
                {
                    foreach (string view in Views)
                    {
                        LegacySpriteSpec spec = NeutralSpec();
                        spec.Direction = view;
                        applyVariant(spec, variantName);
                        List<(string Name, PixelBuffer Image)> layers = renderer.RenderLayers(spec, LegacyPosePresets.Poses["idle_0"]);
                        PixelBuffer layer = layers.First(l => l.Name == legacyLayer).Image;
                        bool isNoHair = partId is "hair_front" or "hair_back" && variantName == "Không tóc";
                        if (!HasOpaquePixels(layer) && !isNoHair)
                        {
                            continue;
                        }

                        SaveAsset(project, projectRoot, library, $"part.{partId}.{Slug(variantName)}.{Slug(view)}",
                            $"Part {partId} {variantName} ({view})", AssetTypes.Sprite, layer, [view],
                            [$"part:{partId}", "variant"]);
                        assetCount++;
                    }
                }
            }

            void GenerateFaceCombinations()
            {
                foreach (string eye in AppearanceCatalog.EyeStyles)
                {
                    foreach (string mouth in AppearanceCatalog.MouthStyles)
                    {
                        foreach (string view in Views)
                        {
                            LegacySpriteSpec spec = NeutralSpec();
                            spec.Direction = view;
                            spec.EyeStyle = eye;
                            spec.MouthStyle = mouth;
                            List<(string Name, PixelBuffer Image)> layers =
                                renderer.RenderLayers(spec, LegacyPosePresets.Poses["idle_0"]);
                            PixelBuffer layer = layers.First(l => l.Name == "face").Image;
                            SaveAsset(project, projectRoot, library,
                                $"part.face.combo.{Slug(eye)}.{Slug(mouth)}.{Slug(view)}",
                                $"Part face {eye} + {mouth} ({view})", AssetTypes.Sprite, layer,
                                [view], ["part:face", "variant", "composite"]);
                            assetCount++;
                        }
                    }
                }
            }

            GenerateAppearanceVariants("torso", "body", AppearanceCatalog.BodyVariants,
                (spec, name) => spec.BodyType = name);
            GenerateAppearanceVariants("face", "face", AppearanceCatalog.EyeStyles,
                (spec, name) => spec.EyeStyle = name);
            GenerateAppearanceVariants("face", "face", AppearanceCatalog.MouthStyles,
                (spec, name) => spec.MouthStyle = name);
            GenerateFaceCombinations();
            GenerateAppearanceVariants("hair_front", "hair_front", AppearanceCatalog.HairStyles,
                (spec, name) => spec.HairStyle = name);
            GenerateAppearanceVariants("hair_back", "hair_back", AppearanceCatalog.HairStyles,
                (spec, name) => spec.HairStyle = name);

            // Equipment items per view (rendered alone on the default body) —
            // assets only; characters stay bare until the user equips them.
            foreach (EquipmentSlotDef slot in rig.EquipmentSlots)
            {
                foreach (string item in LegacyCatalog.EquipmentLibrary.TryGetValue(slot.Id, out string[]? items)
                    ? items
                    : [])
                {
                    if (item == "Không")
                    {
                        continue;
                    }

                    string legacyLayer = slot.Id switch
                    {
                        "head" => "head_equipment",
                        "main_hand" => "weapon_back",
                        _ => slot.Id,
                    };
                    foreach (string view in Views)
                    {
                        LegacySpriteSpec spec = NeutralSpec();
                        spec.Direction = view;
                        spec.Equipment[slot.Id] = item;
                        List<(string Name, PixelBuffer Image)> layers = renderer.RenderLayers(spec, LegacyPosePresets.Poses["idle_0"]);
                        PixelBuffer layer = layers.First(l => l.Name == legacyLayer).Image;
                        if (!HasOpaquePixels(layer))
                        {
                            continue;
                        }

                        SaveAsset(project, projectRoot, library, $"eq.{slot.Id}.{Slug(item)}.{Slug(view)}",
                            $"{slot.DisplayName}: {item} ({view})", AssetTypes.Equipment, layer, [view],
                            ["starter", $"slot:{slot.Id}"]);
                        assetCount++;
                    }
                }
            }

            // Weapon front states (raise/slash) for the main_hand slot.
            foreach (string weaponState in new[] { "raise", "slash" })
            {
                foreach (string view in Views)
                {
                    LegacySpriteSpec spec = NeutralSpec();
                    spec.Direction = view;
                    spec.Equipment["main_hand"] = "Kiếm"; // raise/slash art is weapon-independent in the legacy renderer
                    var pose = new global::PixelGameStudio.Rendering.Procedural.LegacyPose("weapon", WeaponState: weaponState);
                    List<(string Name, PixelBuffer Image)> layers = renderer.RenderLayers(spec, pose);
                    PixelBuffer layer = layers.First(l => l.Name == "weapon_front").Image;
                    if (!HasOpaquePixels(layer))
                    {
                        continue;
                    }

                    SaveAsset(project, projectRoot, library, $"eq.main_hand.{weaponState}.{Slug(view)}",
                        $"Vũ khí {weaponState} ({view})", AssetTypes.Equipment, layer, [view],
                        ["starter", "slot:main_hand", "state"]);
                    assetCount++;
                }
            }
        }

        // Target character: the caller's character, or a fresh default one.
        CharacterEntity target;
        bool createdDefault = character is null;
        if (character is null)
        {
            target = new CharacterEntity
            {
                Id = "char.hero",
                Name = "Nhân vật mới",
                RigId = rig.Id,
            };
            project.Characters.RemoveAll(c => c.Id == target.Id);
            project.Characters.Add(target);
        }
        else
        {
            target = character;
            target.RigId = rig.Id;
        }

        // Wire the bare base look onto the target character: base art per part,
        // registered variant assets and default state values, no equipment.
        foreach (PartNode part in rig.Parts)
        {
            if (part.Id == "weapon")
            {
                continue; // the weapon part is dressed by the main_hand equipment slot, not by appearance
            }

            PartAppearance appearance = AppearanceService.EnsureAppearance(target, part.Id);
            if (appearance.AssetRef is null && appearance.ViewAssets.Count == 0)
            {
                foreach (string view in project.View.Directions)
                {
                    string assetId = $"part.{part.Id}.{Slug(view)}";
                    if (project.Assets.Any(a => a.Id.Equals(assetId, StringComparison.Ordinal)))
                    {
                        appearance.ViewAssets[view] = new AssetReference { AssetId = assetId };
                    }
                }
            }
        }

        foreach (string partId in new[] { "arm_left", "arm_right" })
        {
            PartAppearance? appearance = target.AppearanceOf(partId);
            if (appearance is null)
            {
                continue;
            }

            RegisterVariantAssets(project, appearance, partId, "hand", HandStates);
            if (!appearance.States.ContainsKey("hand"))
            {
                appearance.States["hand"] = "down";
            }
        }

        foreach (string partId in new[] { "leg_left", "leg_right" })
        {
            PartAppearance? appearance = target.AppearanceOf(partId);
            if (appearance is null)
            {
                continue;
            }

            RegisterVariantAssets(project, appearance, partId, "leg", LegStates);
            if (!appearance.States.ContainsKey("leg"))
            {
                appearance.States["leg"] = "neutral";
            }
        }

        PartAppearance? torso = target.AppearanceOf("torso");
        if (torso is not null)
        {
            RegisterVariantAssets(project, torso, "torso", "body", AppearanceCatalog.BodyVariants.Select(Slug));
            if (torso.StateAssets.ContainsKey("body") && !torso.States.ContainsKey("body"))
            {
                torso.States["body"] = "tieu-chuan";
            }
        }

        PartAppearance? face = target.AppearanceOf("face");
        if (face is not null)
        {
            RegisterVariantAssets(project, face, "face", "eye", AppearanceCatalog.EyeStyles.Select(Slug));
            RegisterVariantAssets(project, face, "face", "mouth", AppearanceCatalog.MouthStyles.Select(Slug));
            RegisterFaceCombinationAssets(project, face);
            if (face.StateAssets.ContainsKey("eye") && !face.States.ContainsKey("eye"))
            {
                face.States["eye"] = "binh-tinh";
            }

            if (face.StateAssets.ContainsKey("mouth") && !face.States.ContainsKey("mouth"))
            {
                face.States["mouth"] = "trung-tinh";
            }
        }

        foreach (string hairPart in new[] { "hair_front", "hair_back" })
        {
            PartAppearance? hair = target.AppearanceOf(hairPart);
            if (hair is null)
            {
                continue;
            }

            RegisterVariantAssets(project, hair, hairPart, "hair", AppearanceCatalog.HairStyles.Select(Slug));
            if (hair.StateAssets.ContainsKey("hair") && !hair.States.ContainsKey("hair"))
            {
                hair.States["hair"] = "bui-cao";
            }
        }

        return new StarterContentResult(assetCount, target.Id, rig.Id);
    }

    /// <summary>Registers the project's variant assets (by id pattern) under one state key of a part.</summary>
    private static void RegisterVariantAssets(
        Project project, PartAppearance appearance, string partId, string stateKey, IEnumerable<string> values)
    {
        foreach (string value in values)
        {
            var variant = new StateAssetVariant($"part.{partId}.{value}.{Slug(project.View.DefaultDirection)}");
            foreach (string view in project.View.Directions)
            {
                string assetId = $"part.{partId}.{value}.{Slug(view)}";
                if (project.Assets.Any(a => a.Id.Equals(assetId, StringComparison.Ordinal)))
                {
                    variant.ViewAssets[view] = assetId;
                }
            }

            if (variant.ViewAssets.Count == 0)
            {
                continue;
            }

            if (!appearance.StateAssets.TryGetValue(stateKey, out Dictionary<string, StateAssetVariant>? byValue))
            {
                byValue = new Dictionary<string, StateAssetVariant>(StringComparer.OrdinalIgnoreCase);
                appearance.StateAssets[stateKey] = byValue;
            }

            byValue[value] = variant;
        }
    }

    private static void RegisterFaceCombinationAssets(Project project, PartAppearance appearance)
    {
        var values = new Dictionary<string, StateAssetVariant>(StringComparer.OrdinalIgnoreCase);
        foreach (string eye in AppearanceCatalog.EyeStyles.Select(Slug))
        {
            foreach (string mouth in AppearanceCatalog.MouthStyles.Select(Slug))
            {
                string key = FaceCombinationKey(eye, mouth);
                var variant = new StateAssetVariant(
                    $"part.face.combo.{eye}.{mouth}.{Slug(project.View.DefaultDirection)}");
                foreach (string view in project.View.Directions)
                {
                    string assetId = $"part.face.combo.{eye}.{mouth}.{Slug(view)}";
                    if (project.Assets.Any(a => a.Id.Equals(assetId, StringComparison.Ordinal)))
                    {
                        variant.ViewAssets[view] = assetId;
                    }
                }

                if (variant.ViewAssets.Count > 0)
                {
                    values[key] = variant;
                }
            }
        }

        if (values.Count > 0)
        {
            appearance.StateAssets["face"] = values;
        }
    }

    internal static string FaceCombinationKey(string eye, string mouth) => $"{eye}|{mouth}";

    /// <summary>Generates the full art library plus a default character wearing the demo loadout.</summary>
    public static StarterContentResult CreateHumanoidDemo(Project project, string projectRoot, AssetLibraryService library)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentNullException.ThrowIfNull(library);

        StarterContentResult baseBody = CreateBaseBody(project, projectRoot, library);
        CharacterEntity character = project.Characters.First(c => c.Id == baseBody.CharacterId);
        RigDefinition rig = project.Rigs.First(r => r.Id == baseBody.RigId);

        // Starter loadout — the demo character wears one item per non-empty slot.
        foreach (KeyValuePair<string, string> kv in SlotAssetIds)
        {
            if (kv.Value == "Không")
            {
                continue;
            }

            var viewAssets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string view in Views)
            {
                string assetId = $"eq.{kv.Key}.{Slug(kv.Value)}.{Slug(view)}";
                if (project.Assets.Any(a => a.Id.Equals(assetId, StringComparison.Ordinal)))
                {
                    viewAssets[view] = assetId;
                }
            }

            string fallback = $"eq.{kv.Key}.{Slug(kv.Value)}.{Slug("Down")}";
            if (viewAssets.Count == 0 && !project.Assets.Any(a => a.Id.Equals(fallback, StringComparison.Ordinal)))
            {
                continue;
            }

            var equipped = new EquippedItem(kv.Key, fallback)
            {
                ViewAssets = viewAssets,
            };
            if (kv.Key == "main_hand")
            {
                var weaponVariants = new Dictionary<string, StateAssetVariant>(StringComparer.OrdinalIgnoreCase);
                foreach (string weaponState in new[] { "raise", "slash" })
                {
                    var variant = new StateAssetVariant($"eq.main_hand.{weaponState}.{Slug("Down")}");
                    foreach (string view in Views)
                    {
                        string assetId = $"eq.main_hand.{weaponState}.{Slug(view)}";
                        if (project.Assets.Any(a => a.Id.Equals(assetId, StringComparison.Ordinal)))
                        {
                            variant.ViewAssets[view] = assetId;
                        }
                    }

                    if (variant.ViewAssets.Count > 0)
                    {
                        weaponVariants[weaponState] = variant;
                    }
                }

                if (weaponVariants.Count > 0)
                {
                    equipped = equipped with { StateAssets = weaponVariants };
                }
            }

            character.Equipment.RemoveAll(e => e.SlotId == kv.Key);
            character.Equipment.Add(equipped);
        }

        return baseBody;
    }

    internal static LegacySpriteSpec NeutralSpec()
    {
        var spec = new LegacySpriteSpec();
        foreach (string key in spec.Equipment.Keys.ToList())
        {
            spec.Equipment[key] = "Không";
        }

        return spec;
    }

    private static void SaveAsset(
        Project project, string projectRoot, AssetLibraryService library,
        string id, string displayName, string type, PixelBuffer pixels,
        IReadOnlyList<string> views, IReadOnlyList<string> tags)
    {
        // Write directly (pixels already native-size) — bypasses Import's canvas
        // check because these ARE project-canvas assets generated in-process.
        string path = Path.Combine(AssetLibraryService.AssetsDirectory(projectRoot), $"{id}.png");
        Directory.CreateDirectory(AssetLibraryService.AssetsDirectory(projectRoot));
        using (Stream output = File.Create(path))
        {
            PngCodec.Encode(pixels, output);
        }

        project.Assets.RemoveAll(a => a.Id == id);
        project.Assets.Add(new AssetDefinition
        {
            Id = id,
            DisplayName = displayName,
            Type = type,
            File = $"{id}.png",
            CanvasWidth = pixels.Width,
            CanvasHeight = pixels.Height,
            Tags = [.. tags],
            Views = [.. views],
        });
    }

    private static bool HasOpaquePixels(PixelBuffer buffer)
    {
        for (int y = 0; y < buffer.Height; y++)
        {
            for (int x = 0; x < buffer.Width; x++)
            {
                if (buffer[x, y].A > 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>ASCII slug: strips Vietnamese diacritics, lowercases, non-alphanumerics become '-'.</summary>
    internal static string Slug(string input) => AppearanceCatalog.Slug(input);
}
