using PixelGameStudio.Core.Primitives;
using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Assets;
using PixelGameStudio.Domain.Templates;
using PixelGameStudio.ProjectSystem;
using PixelGameStudio.Assets;
using PixelGameStudio.Rendering;
using Xunit;

namespace PixelGameStudio.ProjectSystem.Tests;

public class AssetLibraryTests : IDisposable
{
    private readonly string _root;
    private readonly string _sourceDir;
    private readonly ProjectStore _store = new();
    private readonly AssetLibraryService _library = new();
    private readonly Project _project;

    public AssetLibraryTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "pgs-tests", Guid.NewGuid().ToString("N"));
        _sourceDir = Path.Combine(_root, "sources");
        Directory.CreateDirectory(_sourceDir);
        _project = ProjectTemplates.ClassicTopDown46();
        _store.Save(_project, _root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private string WritePng(string name, int width, int height, byte fill = 200)
    {
        var buffer = new PixelBuffer(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                buffer[x, y] = new Rgba32(fill, (byte)(255 - fill), 90, 255);
            }
        }

        string path = Path.Combine(_sourceDir, name);
        using (Stream output = File.Create(path))
        {
            PngCodec.Encode(buffer, output);
        }

        return path;
    }

    [Fact]
    public void Import_CopiesNormalizedPng_AndRegistersDefinition()
    {
        string source = WritePng("iron_sword.png", 32, 46);

        AssetImportResult result = _library.Import(_project, _root, source, new AssetImportOptions
        {
            DisplayName = "Kiếm sắt",
            Type = AssetTypes.Equipment,
            Tags = ["weapon", "metal"],
            Views = ["Down", "Left"],
            Anchor = new AssetAnchor(4, 29),
            ZIndex = 3,
        });

        Assert.Equal("iron_sword", result.Definition.Id);
        Assert.Equal(32, result.Definition.CanvasWidth);
        Assert.Equal(46, result.Definition.CanvasHeight);
        Assert.False(result.Resized);
        string stored = _library.ResolvePath(_root, result.Definition);
        Assert.True(File.Exists(stored));
        PixelBuffer pixels = _library.LoadPixels(_root, result.Definition);
        Assert.Equal(32, pixels.Width);
        Assert.Equal(46, pixels.Height);
        Assert.Single(_project.Assets);
        Assert.Empty(_project.Validate());
    }

    [Fact]
    public void Import_SlugifiesFileName()
    {
        string source = WritePng("Iron Sword #1.png", 32, 46);

        AssetImportResult result = _library.Import(_project, _root, source, new AssetImportOptions());

        Assert.Equal("Iron-Sword-1", result.Definition.Id);
    }

    [Fact]
    public void Import_WrongCanvas_Rejected_UnlessAllowResize()
    {
        string source = WritePng("small.png", 16, 16);

        var ex = Assert.Throws<AssetImportException>(
            () => _library.Import(_project, _root, source, new AssetImportOptions { AssetId = "small" }));
        Assert.Contains("khác canvas project", ex.Message);

        AssetImportResult result = _library.Import(_project, _root, source, new AssetImportOptions
        {
            AssetId = "small",
            AllowResize = true,
        });

        Assert.True(result.Resized);
        Assert.Equal(32, result.Definition.CanvasWidth);
        Assert.Equal(46, result.Definition.CanvasHeight);
        Assert.Single(result.Warnings);
        // audit §10: rescale must be nearest-neighbor, never silent
        Assert.Contains("resize NEAREST", result.Warnings[0]);
    }

    [Fact]
    public void Import_DuplicateId_Rejected_UnlessOverwrite()
    {
        string source = WritePng("axe.png", 32, 46);
        _library.Import(_project, _root, source, new AssetImportOptions { AssetId = "axe" });

        Assert.Throws<AssetImportException>(
            () => _library.Import(_project, _root, source, new AssetImportOptions { AssetId = "axe" }));

        _library.Import(_project, _root, source, new AssetImportOptions { AssetId = "axe", OverwriteExisting = true });
        Assert.Single(_project.Assets);
    }

    [Fact]
    public void Import_InvalidPng_ThrowsFriendly()
    {
        string path = Path.Combine(_sourceDir, "broken.png");
        File.WriteAllText(path, "this is not a png");

        var ex = Assert.Throws<AssetImportException>(
            () => _library.Import(_project, _root, path, new AssetImportOptions()));

        Assert.Contains("PNG hợp lệ", ex.Message);
    }

    [Fact]
    public void Delete_RemovesDefinitionFileAndThumbnails()
    {
        string source = WritePng("axe.png", 32, 46);
        AssetImportResult result = _library.Import(_project, _root, source, new AssetImportOptions { AssetId = "axe" });
        var cache = new ThumbnailCache(_library);
        string thumb = cache.GetOrCreate(_project, _root, result.Definition);
        Assert.True(File.Exists(thumb));

        _library.Delete(_project, _root, "axe");

        Assert.Empty(_project.Assets);
        Assert.False(File.Exists(_library.ResolvePath(_root, result.Definition)));
        Assert.False(File.Exists(thumb));
        Assert.Empty(Directory.GetFiles(Path.Combine(AssetLibraryService.AssetsDirectory(_root), "thumbs")));
    }

    [Fact]
    public void Search_FiltersByQueryTagsAndType()
    {
        _library.Import(_project, _root, WritePng("iron_sword.png", 32, 46), new AssetImportOptions
        {
            AssetId = "iron_sword",
            DisplayName = "Kiếm sắt",
            Type = AssetTypes.Equipment,
            Tags = ["weapon", "metal"],
        });
        _library.Import(_project, _root, WritePng("wood_bow.png", 32, 46), new AssetImportOptions
        {
            AssetId = "wood_bow",
            DisplayName = "Cung gỗ",
            Type = AssetTypes.Equipment,
            Tags = ["weapon", "wood"],
        });
        _library.Import(_project, _root, WritePng("dust.png", 32, 46), new AssetImportOptions
        {
            AssetId = "dust",
            DisplayName = "Bụi hiệu ứng",
            Type = AssetTypes.Placeholder,
        });

        Assert.Equal(3, _library.Search(_project).Count);
        Assert.Equal(["iron_sword"], _library.Search(_project, "kiếm").Select(a => a.Id)); // displayName match
        Assert.Equal(["iron_sword"], _library.Search(_project, tags: ["metal"]).Select(a => a.Id));
        Assert.Equal(["iron_sword"], _library.Search(_project, tags: ["weapon", "metal"]).Select(a => a.Id));
        Assert.Empty(_library.Search(_project, tags: ["metal", "wood"])); // AND semantics
        Assert.Equal(2, _library.Search(_project, type: AssetTypes.Equipment).Count);
    }

    [Fact]
    public void Thumbnail_GetOrCreate_ReusesCache_AndInvalidatesOnContentChange()
    {
        string source = WritePng("axe.png", 32, 46);
        AssetImportResult result = _library.Import(_project, _root, source, new AssetImportOptions { AssetId = "axe" });
        var cache = new ThumbnailCache(_library);

        string first = cache.GetOrCreate(_project, _root, result.Definition, 96);
        string reused = cache.GetOrCreate(_project, _root, result.Definition, 96);
        Assert.Equal(first, reused);

        // 32x46 into 96 box → integer 2x up-scale (64x92), nearest only
        PixelBuffer thumb = PngCodec.Decode(File.OpenRead(first));
        Assert.Equal(64, thumb.Width);
        Assert.Equal(92, thumb.Height);

        // Content change of the STORED asset → new hash → new cache file, stale one pruned
        var newPixels = new PixelBuffer(32, 46);
        newPixels.Fill(new Rgba32(20, 235, 90, 255));
        using (Stream output = File.Create(_library.ResolvePath(_root, result.Definition)))
        {
            PngCodec.Encode(newPixels, output);
        }

        string second = cache.GetOrCreate(_project, _root, result.Definition, 96);
        Assert.NotEqual(first, second);
        Assert.False(File.Exists(first));
    }

    [Fact]
    public void Thumbnail_Downscale_UsesNearest_WhenNativeExceedsBox()
    {
        // A large stored asset (constructed directly; import would require AllowResize
        // since it exceeds the project canvas). Thumbnail must still render via nearest down-scale.
        var big = new PixelBuffer(256, 256);
        big.Fill(new Rgba32(10, 10, 10, 255));
        Directory.CreateDirectory(AssetLibraryService.AssetsDirectory(_root));
        string bigPath = Path.Combine(AssetLibraryService.AssetsDirectory(_root), "big.png");
        using (Stream output = File.Create(bigPath))
        {
            PngCodec.Encode(big, output);
        }

        var definition = new AssetDefinition { Id = "big", File = "big.png", CanvasWidth = 256, CanvasHeight = 256 };
        var cache = new ThumbnailCache(_library);

        string thumbPath = cache.GetOrCreate(_project, _root, definition, 96);

        PixelBuffer thumb = PngCodec.Decode(File.OpenRead(thumbPath));
        Assert.Equal(96, thumb.Width);
        Assert.Equal(96, thumb.Height);
    }

    [Fact]
    public void ProjectRoundTrip_PreservesAssetDefinitions()
    {
        _library.Import(_project, _root, WritePng("iron_sword.png", 32, 46), new AssetImportOptions
        {
            AssetId = "iron_sword",
            Type = AssetTypes.Equipment,
            Tags = ["weapon"],
            Views = ["Down"],
            Anchor = new AssetAnchor(4, 29),
            ZIndex = 2,
        });

        _store.Save(_project, _root);
        Project loaded = _store.Open(_root);

        AssetDefinition asset = Assert.Single(loaded.Assets);
        Assert.Equal("iron_sword", asset.Id);
        Assert.Equal(AssetTypes.Equipment, asset.Type);
        Assert.Equal(["weapon"], asset.Tags);
        Assert.Equal(["Down"], asset.Views);
        Assert.Equal(new AssetAnchor(4, 29), asset.Anchor);
        Assert.Equal(2, asset.ZIndex);
    }

    [Fact]
    public void ValidateOnDisk_DetectsMissingFileAndCanvasMismatch()
    {
        _library.Import(_project, _root, WritePng("axe.png", 32, 46), new AssetImportOptions { AssetId = "axe" });
        AssetDefinition axe = _project.Assets.Single(a => a.Id == "axe");

        // missing file
        File.Delete(_library.ResolvePath(_root, axe));
        IReadOnlyList<string> missingIssues = _library.ValidateOnDisk(_project, _root);
        Assert.Contains(missingIssues, issue => issue.Contains("mất file trên đĩa"));

        // canvas mismatch (rewrite the STORED file at 16x16)
        var wrong = new PixelBuffer(16, 16);
        wrong.Fill(new Rgba32(255, 0, 0, 255));
        using (Stream output = File.Create(_library.ResolvePath(_root, axe)))
        {
            PngCodec.Encode(wrong, output);
        }

        IReadOnlyList<string> mismatchIssues = _library.ValidateOnDisk(_project, _root);
        Assert.Contains(mismatchIssues, issue => issue.Contains("nhưng file là 16x16"));

        // healthy again
        var right = new PixelBuffer(32, 46);
        right.Fill(new Rgba32(200, 55, 90, 255));
        using (Stream output = File.Create(_library.ResolvePath(_root, axe)))
        {
            PngCodec.Encode(right, output);
        }

        Assert.Empty(_library.ValidateOnDisk(_project, _root));
    }

    [Fact]
    public void ResolvePath_RejectsTraversal()
    {
        var evil = new AssetDefinition { Id = "evil", File = "../evil.png", CanvasWidth = 32, CanvasHeight = 46 };

        Assert.Throws<AssetImportException>(() => _library.ResolvePath(_root, evil));
    }
}
