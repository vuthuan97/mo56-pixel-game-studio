using PixelGameStudio.Core.Primitives;
using PixelGameStudio.Rendering;
using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Animation;
using PixelGameStudio.Domain.Assets;
using PixelGameStudio.Domain.Character;

namespace PixelGameStudio.Assets.Composition;

/// <summary>Options for one composition run.</summary>
public sealed class CompositionOptions
{
    /// <summary>View/direction key to render (from the project ViewProfile).</summary>
    public string View { get; set; } = "Down";

    /// <summary>Optional pose; parts not covered keep their rig transform.</summary>
    public PoseDefinition? Pose { get; set; }

    /// <summary>Apply the project StyleProfile external 1px outline after compositing.</summary>
    public bool ApplyOutline { get; set; } = true;

}

/// <summary>
/// The asset-driven production composer (rule 05_assets_equipment): renders a
/// CharacterEntity by resolving each part's asset per view/state, applying
/// palette overrides and integer offsets, layering by explicit z-order, then
/// equipment on its slot anchors, and finally the StyleProfile outline.
/// Procedural renderers are reduced to asset/template sources — composition
/// itself is pure pixel operations.
/// </summary>
public sealed class RigSpriteComposer
{
    private readonly AssetLibraryService _library;

    public RigSpriteComposer(AssetLibraryService library) => _library = library;

    private sealed record DrawOp(int Z, int Order, string Name, PixelBuffer Pixels, int X, int Y);

    public PixelBuffer Compose(Project project, string projectRoot, CharacterEntity character, CompositionOptions options)
    {
        var issues = new List<string>();
        List<DrawOp> ops = BuildDrawOps(project, projectRoot, character, options, issues);
        if (issues.Count > 0)
        {
            throw new CompositionException(issues);
        }

        var canvas = new PixelBuffer(project.Pixels.CanvasWidth, project.Pixels.CanvasHeight);
        foreach (DrawOp op in ops.OrderBy(op => op.Z).ThenBy(op => op.Order))
        {
            PixelOps.Composite(canvas, op.Pixels, op.X, op.Y);
        }

        if (options.ApplyOutline &&
            Core.Primitives.Rgba32.TryParseHex(project.Style.OutlineColorHex, out Rgba32 outlineColor) &&
            project.Style.OutlineStyle is "External1px" or "Selective1px")
        {
            return project.Style.OutlineStyle == "Selective1px"
                ? PixelOps.SelectiveOutline(canvas, outlineColor)
                : PixelOps.Outline(canvas, outlineColor);
        }

        return canvas;
    }

    /// <summary>
    /// Renders every draw op as its own full-canvas layer (named by part id or
    /// "slot.{slotId}" for equipment), ordered back-to-front — the asset-driven
    /// equivalent of the legacy render_layers export.
    /// </summary>
    public IReadOnlyList<(string Name, PixelBuffer Image)> ComposeLayers(
        Project project, string projectRoot, CharacterEntity character, CompositionOptions options)
    {
        var issues = new List<string>();
        List<DrawOp> ops = BuildDrawOps(project, projectRoot, character, options, issues);
        if (issues.Count > 0)
        {
            throw new CompositionException(issues);
        }

        var layers = new List<(string Name, PixelBuffer Image)>();
        foreach (DrawOp op in ops.OrderBy(op => op.Z).ThenBy(op => op.Order))
        {
            var layer = new PixelBuffer(project.Pixels.CanvasWidth, project.Pixels.CanvasHeight);
            PixelOps.Composite(layer, op.Pixels, op.X, op.Y);
            layers.Add((op.Name, layer));
        }

        return layers;
    }

    private List<DrawOp> BuildDrawOps(
        Project project,
        string projectRoot,
        CharacterEntity character,
        CompositionOptions options,
        List<string> issues)
    {
        RigDefinition rig = project.Rigs.FirstOrDefault(r => r.Id.Equals(character.RigId, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Character '{character.RigId}' trỏ tới rig không tồn tại '{character.RigId}'.");

        string view = options.View;
        var cache = new Dictionary<string, PixelBuffer>(StringComparer.OrdinalIgnoreCase);
        var ops = new List<DrawOp>();
        int order = 0;

        foreach (PartNode part in rig.Parts)
        {
            PartAppearance? appearance = character.AppearanceOf(part.Id);
            PartPose pose = options.Pose?.Parts.TryGetValue(part.Id, out PartPose? p) == true ? p : new PartPose();
            if (pose.Hidden)
            {
                continue;
            }

            var states = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> kv in appearance?.States ?? new Dictionary<string, string>())
            {
                states[kv.Key] = kv.Value;
            }

            foreach (KeyValuePair<string, string> kv in pose.States)
            {
                states[kv.Key] = kv.Value;
            }

            string? assetId = ResolvePartAsset(appearance, part, view, states);
            if (assetId is null)
            {
                continue;
            }

            AssetDefinition? asset = project.Assets.FirstOrDefault(a => a.Id.Equals(assetId, StringComparison.Ordinal));
            if (asset is null)
            {
                issues.Add($"Asset '{assetId}' của part '{part.Id}' không có trong thư viện.");
                continue;
            }

            PixelBuffer pixels = LoadAndPrepare(project, projectRoot, cache, asset, appearance, view);
            (int absX, int absY) = AbsoluteOffset(rig, part.Id);
            (int buildX, int buildY) = character.Build?.PartOffset(part.Id) ?? (0, 0);
            ops.Add(new DrawOp(part.ZIndex, order++, part.Id, pixels,
                absX + buildX + pose.OffsetX, absY + buildY + pose.OffsetY));
        }

        foreach (EquippedItem item in character.Equipment)
        {
            EquipmentSlotDef? slot = rig.FindSlot(item.SlotId);
            if (slot is null)
            {
                issues.Add($"Slot '{item.SlotId}' không tồn tại trong rig — bỏ qua khi render.");
                continue;
            }

            SlotPose slotPose = options.Pose?.Slots.TryGetValue(item.SlotId, out SlotPose? sp) == true ? sp : new SlotPose();
            if (slotPose.Hidden)
            {
                continue;
            }

            var states = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> kv in slotPose.States)
            {
                states[kv.Key] = kv.Value;
            }

            string? equippedAssetId = ResolveEquippedAsset(item, view, states);
            AssetDefinition? asset = equippedAssetId is null
                ? null
                : project.Assets.FirstOrDefault(a => a.Id.Equals(equippedAssetId, StringComparison.Ordinal));
            if (asset is null)
            {
                issues.Add($"Trang bị '{equippedAssetId ?? item.AssetId}' trên slot '{item.SlotId}' không hợp lệ — bỏ qua khi render.");
                continue;
            }

            PixelBuffer pixels = LoadAndPrepare(project, projectRoot, cache, asset, null, view);
            AnchorPoint? anchor = rig.FindAnchor(slot.AnchorId);
            (int absX, int absY) = anchor is null ? (0, 0) : AbsoluteOffset(rig, anchor.PartId);
            (int buildX, int buildY) = anchor is null || character.Build is null
                ? (0, 0)
                : character.Build.PartOffset(anchor.PartId);
            int z = slotPose.ZIndexOverride ?? slot.ZIndex;
            ops.Add(new DrawOp(z, order++, $"slot.{item.SlotId}", pixels,
                absX + buildX + (anchor?.OffsetX ?? 0) + slotPose.OffsetX,
                absY + buildY + (anchor?.OffsetY ?? 0) + slotPose.OffsetY));
        }

        return ops;
    }

    /// <summary>
    /// Asset resolution order for a part: state variant art (pose state wins),
    /// then the per-view asset, then the default asset. Null = nothing to draw.
    /// </summary>
    private static string? ResolvePartAsset(
        PartAppearance? appearance,
        PartNode part,
        string view,
        IReadOnlyDictionary<string, string> states)
    {
        if (appearance is not null)
        {
            // The face is one rig part, but eye and mouth are independent
            // controls. Prefer the generated composite asset so changing the
            // mouth does not get masked by the eye variant (or vice versa).
            if (part.Id.Equals("face", StringComparison.OrdinalIgnoreCase) &&
                states.TryGetValue("eye", out string? eye) &&
                states.TryGetValue("mouth", out string? mouth) &&
                appearance.StateAssets.TryGetValue("face", out Dictionary<string, StateAssetVariant>? combinations) &&
                combinations.TryGetValue($"{eye}|{mouth}", out StateAssetVariant? combination) &&
                combination.ViewAssets.TryGetValue(view, out string? combinedAssetId))
            {
                return combinedAssetId;
            }

            foreach (KeyValuePair<string, string> kv in states)
            {
                if (appearance.StateAssets.TryGetValue(kv.Key, out Dictionary<string, StateAssetVariant>? byValue) &&
                    byValue.TryGetValue(kv.Value, out StateAssetVariant? variant) &&
                    variant.ViewAssets.TryGetValue(view, out string? stateViewAssetId))
                {
                    return stateViewAssetId;
                }

                // A state variant without art for THIS view falls through to the
                // view's default asset (e.g. the face has no art on the Up view).
            }

            if (appearance.ViewAssets.TryGetValue(view, out AssetReference? viewAsset))
            {
                return viewAsset.AssetId;
            }

            return appearance.AssetRef?.AssetId ?? part.AssetRef?.AssetId;
        }

        return part.AssetRef?.AssetId;
    }

    private static string? ResolveEquippedAsset(
        EquippedItem item,
        string view,
        IReadOnlyDictionary<string, string> states)
    {
        foreach (KeyValuePair<string, string> kv in states)
        {
            if (item.StateAssets.TryGetValue(kv.Value, out StateAssetVariant? variant))
            {
                return variant.ViewAssets.TryGetValue(view, out string? viewAssetId) ? viewAssetId : variant.AssetId;
            }
        }

        return item.ViewAssets.TryGetValue(view, out string? assetId) ? assetId : item.AssetId;
    }

    private PixelBuffer LoadAndPrepare(
        Project project,
        string root,
        Dictionary<string, PixelBuffer> cache,
        AssetDefinition asset,
        PartAppearance? appearance,
        string view)
    {
        if (!cache.TryGetValue(asset.Id, out PixelBuffer? pixels))
        {
            pixels = _library.LoadPixels(root, asset);
            cache[asset.Id] = pixels;
        }

        PixelBuffer prepared = pixels.Clone();

        if (appearance is not null && appearance.PaletteOverride.Count > 0)
        {
            var map = new Dictionary<Rgba32, Rgba32>();
            foreach (PaletteMapping mapping in appearance.PaletteOverride)
            {
                if (Rgba32.TryParseHex(mapping.FromHex, out Rgba32 from) &&
                    Rgba32.TryParseHex(mapping.ToHex, out Rgba32 to))
                {
                    map[from] = to;
                }
            }

            PixelPrimitives.PaletteSwap(prepared, map);
        }

        if (appearance?.ViewMirrors.Contains(view, StringComparer.OrdinalIgnoreCase) == true)
        {
            prepared = PixelOps.MirrorX(prepared);
        }

        return prepared;
    }

    private static (int X, int Y) AbsoluteOffset(RigDefinition rig, string partId)
    {
        int x = 0;
        int y = 0;
        foreach (PartNode part in rig.PathToRoot(partId))
        {
            x += part.Transform.OffsetX;
            y += part.Transform.OffsetY;
        }

        return (x, y);
    }
}

/// <summary>Raised when composition encounters broken references; carries all issues.</summary>
public sealed class CompositionException : Exception
{
    public CompositionException(IReadOnlyList<string> issues)
        : base("Lỗi compose: " + string.Join("; ", issues))
    {
        Issues = issues;
    }

    public IReadOnlyList<string> Issues { get; }
}
