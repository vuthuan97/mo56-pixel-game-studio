using PixelGameStudio.Core.Primitives;
using PixelGameStudio.Rendering;
using PixelGameStudio.Rendering.Procedural;

string outputRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "artifacts", "renderer-v3"));
Directory.CreateDirectory(outputRoot);

string[] directions = ["Down", "Up", "Left", "Right"];
var reference = new ReferenceGridSpriteRenderer();
var legacy = new LegacySpriteRenderer();

WriteSheet(Path.Combine(outputRoot, "reference-grid-v3-after-4x.png"), directions,
    direction => Compose(reference.RenderLayers(new LegacySpriteSpec { Direction = direction }, LegacyPosePresets.Poses["idle_0"])), 4);
WriteSheet(Path.Combine(outputRoot, "reference-grid-v3-bare-1x.png"), directions,
    direction => Compose(reference.RenderLayers(BareSpec(direction), LegacyPosePresets.Poses["idle_0"])), 1);
WriteSheet(Path.Combine(outputRoot, "reference-grid-v3-bare-4x.png"), directions,
    direction => Compose(reference.RenderLayers(BareSpec(direction), LegacyPosePresets.Poses["idle_0"])), 4);
WriteSheet(Path.Combine(outputRoot, "legacy-before-4x.png"), directions,
    direction => legacy.Render(new LegacySpriteSpec { Direction = direction }, LegacyPosePresets.Poses["idle_0"]), 4);
WriteSheet(Path.Combine(outputRoot, "reference-grid-v3-walk-and-arm-up-4x.png"), directions,
    direction => Compose(reference.RenderLayers(new LegacySpriteSpec { Direction = direction }, LegacyPosePresets.Poses["attack_1"])), 4);
WriteModes(Path.Combine(outputRoot, "reference-grid-v3-modes-4x.png"), reference);
WriteBodyVariants(Path.Combine(outputRoot, "reference-grid-v3-body-variants-4x.png"), reference);

static LegacySpriteSpec BareSpec(string direction)
{
    var spec = new LegacySpriteSpec { Direction = direction };
    foreach (string key in spec.Equipment.Keys.ToList())
    {
        spec.Equipment[key] = "Không";
    }

    return spec;
}

static PixelBuffer Compose(List<(string Name, PixelBuffer Image)> layers)
{
    var result = new PixelBuffer(layers[0].Image.Width, layers[0].Image.Height);
    foreach ((string _, PixelBuffer image) in layers)
    {
        PixelOps.Composite(result, image);
    }

    return PixelOps.SelectiveOutline(result, new Rgba32(25, 17, 38, 255));
}

static void WriteSheet(string path, string[] directions, Func<string, PixelBuffer> render, int scale)
{
    const int gap = 4;
    int cellWidth = directions.Length > 0 ? render(directions[0]).Width * scale : 1;
    int cellHeight = directions.Length > 0 ? render(directions[0]).Height * scale : 1;
    var sheet = new PixelBuffer(cellWidth * directions.Length + gap * (directions.Length - 1), cellHeight);
    for (int index = 0; index < directions.Length; index++)
    {
        PixelBuffer scaled = PixelOps.NearestScale(render(directions[index]), scale);
        PixelOps.Composite(sheet, scaled, index * (cellWidth + gap), 0);
    }

    Save(path, sheet);
}

static void WriteModes(string path, ReferenceGridSpriteRenderer renderer)
{
    var spec = new LegacySpriteSpec { Direction = "Down" };
    PixelBuffer normal = Compose(renderer.RenderLayers(spec, LegacyPosePresets.Poses["idle_0"]));
    PixelBuffer grayscale = PixelOps.ToGrayscale(normal);
    PixelBuffer silhouette = normal.Clone();
    PixelOps.ToSilhouette(silhouette);
    var sheet = new PixelBuffer(32 * 4 * 3 + 8, 46 * 4);
    PixelOps.Composite(sheet, PixelOps.NearestScale(normal, 4), 0, 0);
    PixelOps.Composite(sheet, PixelOps.NearestScale(grayscale, 4), 32 * 4 + 4, 0);
    PixelOps.Composite(sheet, PixelOps.NearestScale(silhouette, 4), 32 * 8 + 8, 0);
    Save(path, sheet);
}

static void WriteBodyVariants(string path, ReferenceGridSpriteRenderer renderer)
{
    string[] variants = ["Tiêu chuẩn", "Mảnh", "Đậm"];
    var sheet = new PixelBuffer(32 * 4 * variants.Length + 8, 46 * 4);
    for (int index = 0; index < variants.Length; index++)
    {
        LegacySpriteSpec spec = BareSpec("Down");
        spec.BodyType = variants[index];
        PixelBuffer image = Compose(renderer.RenderLayers(spec, LegacyPosePresets.Poses["idle_0"]));
        PixelOps.Composite(sheet, PixelOps.NearestScale(image, 4), index * (32 * 4 + 4), 0);
    }

    Save(path, sheet);
}

static void Save(string path, PixelBuffer buffer)
{
    using Stream output = File.Create(path);
    PngCodec.Encode(buffer, output);
}
