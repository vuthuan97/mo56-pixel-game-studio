using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Profiles;
using PixelGameStudio.Domain.Templates;
using PixelGameStudio.ProjectSystem;
using Xunit;

namespace PixelGameStudio.ProjectSystem.Tests;

public class ProjectStoreTests : IDisposable
{
    private readonly string _root;

    public ProjectStoreTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "pgs-tests", Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void Save_ThenOpen_RoundTripsAllProfiles()
    {
        var store = new ProjectStore();
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Name = "Vũ Trụ Mộng Cảnh";
        project.Game = new GameProfile { Genre = GameGenres.Survival, Notes = "Ghi chú tiếng Việt" };
        project.View = ViewProfile.TopDown8();
        project.Pixels = new PixelProfile { CanvasWidth = 48, CanvasHeight = 56, PixelSize = 1 };
        project.Palette = new PaletteProfile
        {
            Name = "Tùy chỉnh",
            MaxColors = 16,
            Colors =
            [
                new PaletteEntry { Name = "Đen", Hex = "#101014FF" },
                new PaletteEntry { Name = "Trắng", Hex = "#FFFFFFEE" },
            ],
        };
        project.Style = new ProjectStyleProfile
        {
            OutlineStyle = "External1px",
            OutlineThickness = 1,
            OutlineColorHex = "#101014FF",
            LightDirection = "TopRight",
            ShadowLevels = 2,
            HighlightLevels = 2,
            DetailDensity = "High",
            TransparentBackground = true,
            AlphaThreshold = 4,
            NearestNeighborOnly = true,
            TileSizePx = 16,
            MinValueRange = 80,
            Character = new CharacterProportions { HeadHeightPx = 12, TorsoHeightPx = 20, LegHeightPx = 18 },
        };

        store.Save(project, _root);
        Project loaded = store.Open(_root);

        Assert.Equal(project.Name, loaded.Name);
        Assert.Equal(project.ProjectId, loaded.ProjectId);
        Assert.Equal(project.Game.Genre, loaded.Game.Genre);
        Assert.Equal(project.Game.Notes, loaded.Game.Notes);
        Assert.Equal(project.View.Perspective, loaded.View.Perspective);
        Assert.Equal(project.View.Directions, loaded.View.Directions);
        Assert.Equal(project.View.DefaultDirection, loaded.View.DefaultDirection);
        Assert.Equal(project.Pixels.CanvasWidth, loaded.Pixels.CanvasWidth);
        Assert.Equal(project.Pixels.CanvasHeight, loaded.Pixels.CanvasHeight);
        Assert.Equal(project.Pixels.PixelSize, loaded.Pixels.PixelSize);
        Assert.Equal(project.Palette.Name, loaded.Palette.Name);
        Assert.Equal(project.Palette.MaxColors, loaded.Palette.MaxColors);
        Assert.Equal(project.Palette.Colors.Count, loaded.Palette.Colors.Count);
        Assert.Equal(project.Palette.Colors[1].Hex, loaded.Palette.Colors[1].Hex);
        Assert.Equal(project.Style.LightDirection, loaded.Style.LightDirection);
        Assert.Equal(project.Style.ShadowLevels, loaded.Style.ShadowLevels);
        Assert.Equal(project.Style.AlphaThreshold, loaded.Style.AlphaThreshold);
        Assert.Equal(project.Style.TileSizePx, loaded.Style.TileSizePx);
        Assert.Equal(project.Style.MinValueRange, loaded.Style.MinValueRange);
        Assert.Equal(project.Style.Character.HeadHeightPx, loaded.Style.Character.HeadHeightPx);
        Assert.True(loaded.Validate().Count == 0);
    }

    [Fact]
    public void Save_CreatesStandardFolderLayout()
    {
        var store = new ProjectStore();

        store.Save(ProjectTemplates.ClassicTopDown46(), _root);

        Assert.True(File.Exists(ProjectPaths.ProjectFile(_root)));
        Assert.True(Directory.Exists(ProjectPaths.AssetsDirectory(_root)));
        Assert.True(Directory.Exists(ProjectPaths.AutosaveDirectory(_root)));
    }

    [Fact]
    public void Save_JsonDoesNotContainBinaryPayloads()
    {
        var store = new ProjectStore();
        store.Save(ProjectTemplates.ClassicTopDown46(), _root);

        string json = File.ReadAllText(ProjectPaths.ProjectFile(_root));
        Assert.DoesNotContain("base64", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Save_UpdatesModifiedAtUtc_AndClearsAutosave()
    {
        var store = new ProjectStore();
        Project project = ProjectTemplates.ClassicTopDown46();
        DateTime before = project.ModifiedAtUtc;
        Thread.Sleep(20);
        store.Autosave(project, _root);
        Assert.True(File.Exists(ProjectPaths.AutosaveFile(_root)));

        store.Save(project, _root);

        Assert.True(project.ModifiedAtUtc > before);
        Assert.False(File.Exists(ProjectPaths.AutosaveFile(_root)));
    }

    [Fact]
    public void Open_MissingProjectFile_Throws()
    {
        var store = new ProjectStore();

        Assert.Throws<ProjectStoreException>(() => store.Open(_root));
    }

    [Fact]
    public void Open_CorruptJson_ThrowsWithFriendlyMessage()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(ProjectPaths.ProjectFile(_root), "{ not json !!!");

        var store = new ProjectStore();
        var ex = Assert.Throws<ProjectStoreException>(() => store.Open(_root));
        Assert.Contains("JSON", ex.Message);
    }

    [Fact]
    public void Open_FutureSchemaVersion_IsRejected()
    {
        var store = new ProjectStore();
        Project project = ProjectTemplates.ClassicTopDown46();
        project.SchemaVersion = Project.CurrentSchemaVersion + 1;
        Directory.CreateDirectory(_root);
        File.WriteAllText(ProjectPaths.ProjectFile(_root), store.Serialize(project));

        Assert.Throws<ProjectStoreException>(() => store.Open(_root));
    }

    [Fact]
    public void Deserialize_UnknownFields_AreIgnoredForwardCompat()
    {
        var store = new ProjectStore();
        Project project = ProjectTemplates.ClassicTopDown46();
        string json = store.Serialize(project);
        json = json.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 1, \"futureField\": { \"x\": 1 }");

        Project loaded = store.Deserialize(json);

        Assert.Equal(project.Name, loaded.Name);
    }
}
