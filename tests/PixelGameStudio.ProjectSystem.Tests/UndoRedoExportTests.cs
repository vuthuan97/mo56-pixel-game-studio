using PixelGameStudio.Assets;
using PixelGameStudio.Assets.Composition;
using PixelGameStudio.Behaviors;
using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Templates;
using PixelGameStudio.Export;
using PixelGameStudio.ProjectSystem;
using PixelGameStudio.Rendering;
using Xunit;

namespace PixelGameStudio.ProjectSystem.Tests;

public class UndoRedoServiceTests
{
    private readonly ProjectStore _store = new();

    [Fact]
    public void Checkpoint_Undo_Redo_RestoresMutations()
    {
        var service = new UndoRedoService(_store);
        var project = ProjectTemplates.ClassicTopDown46();
        project.Name = "v0";
        service.Attach(project);

        service.Checkpoint(project); // snapshot v0, then mutate
        project.Name = "v1";

        service.Checkpoint(project); // snapshot v1, then mutate
        project.Pixels.CanvasWidth = 64;

        Assert.True(service.CanUndo);
        Project? undone = service.Undo(project);
        Assert.NotNull(undone);
        Assert.Equal("v1", undone!.Name);          // undo canvas mutation → v1
        Assert.Equal(32, undone.Pixels.CanvasWidth);

        Project? undoneAgain = service.Undo(undone);
        Assert.NotNull(undoneAgain);
        Assert.Equal("v0", undoneAgain!.Name);     // undo rename → v0

        Assert.True(service.CanRedo);
        Project? redone = service.Redo(undoneAgain);
        Assert.NotNull(redone);
        Assert.Equal("v1", redone!.Name);
        Assert.Equal(32, redone.Pixels.CanvasWidth);

        _ = service.Redo(redone);
        Assert.False(service.CanRedo); // redo consumed
    }

    [Fact]
    public void Checkpoint_ClearsRedoStack()
    {
        var service = new UndoRedoService(_store);
        var project = ProjectTemplates.ClassicTopDown46();
        service.Attach(project);

        service.Checkpoint(project);
        project.Name = "v1";
        _ = service.Undo(project);
        Assert.True(service.CanRedo);

        service.Checkpoint(project); // new branch → redo must clear
        Assert.False(service.CanRedo);
    }

    [Fact]
    public void Undo_EmptyHistory_ReturnsNull()
    {
        var service = new UndoRedoService(_store);
        var project = ProjectTemplates.ClassicTopDown46();
        service.Attach(project);

        Assert.False(service.CanUndo);
        Assert.Null(service.Undo(project));
    }

    [Fact]
    public void History_IsCapped()
    {
        var service = new UndoRedoService(_store);
        var project = ProjectTemplates.ClassicTopDown46();
        service.Attach(project);

        for (int i = 0; i < 60; i++)
        {
            service.Checkpoint(project);
            project.Pixels.CanvasWidth = 32 + i;
        }

        Assert.True(service.UndoDepth <= 50);
    }
}

public class ExportServiceTests : IDisposable
{
    private readonly string _root;
    private readonly ProjectStore _store = new();
    private readonly AssetLibraryService _library = new();
    private readonly RigSpriteComposer _composer = new(new AssetLibraryService());

    public ExportServiceTests()
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
    public void ExportCharacter_ProducesFramesSpritesheetAndManifest()
    {
        var project = ProjectTemplates.ClassicTopDown46();
        _store.Save(project, _root);
        StarterContentFactory.CreateHumanoidDemo(project, _root, _library);
        new BehaviorLibraryService().InstallPresets(project); // walk animation for export
        var hero = project.Characters.Single(c => c.Id == "char.hero");

        var service = new ExportService(_composer);
        string outDir = Path.Combine(_root, "export");
        ExportResult result = service.ExportCharacter(project, _root, hero, new CharacterExportOptions
        {
            Views = ["Down"],
            AnimationId = "walk",
            IncludeFrames = true,
            IncludeSpritesheet = true,
            IncludeManifest = true,
        }, outDir);

        Assert.Equal(4, result.FrameCount);
        Assert.Equal(1, result.SheetCount);

        // frames exist and decode
        for (int i = 0; i < 4; i++)
        {
            string framePath = Path.Combine(outDir, "frames", "down", $"down_{i:00}.png");
            Assert.True(File.Exists(framePath), framePath);
            PixelBuffer frame = PngCodec.Decode(File.OpenRead(framePath));
            Assert.Equal(32, frame.Width);
            Assert.Equal(46, frame.Height);
        }

        // spritesheet: one row of 4 columns of 32x46 cells (default packing)
        string sheetPath = Path.Combine(outDir, "spritesheet_down.png");
        Assert.True(File.Exists(sheetPath));
        PixelBuffer sheet = PngCodec.Decode(File.OpenRead(sheetPath));
        Assert.Equal(32 * 4, sheet.Width);
        Assert.Equal(46, sheet.Height);

        // manifest carries the frame map (audit §9 fix)
        string manifestPath = Path.Combine(outDir, "manifest.json");
        Assert.True(File.Exists(manifestPath));
        string manifest = File.ReadAllText(manifestPath);
        Assert.Contains("\"key\": \"down_00\"", manifest);
        Assert.Contains("\"column\": 3", manifest);
    }

    [Fact]
    public void ExportCharacter_UnknownAnimation_Throws()
    {
        var project = ProjectTemplates.ClassicTopDown46();
        _store.Save(project, _root);
        StarterContentFactory.CreateHumanoidDemo(project, _root, _library);
        var hero = project.Characters.Single(c => c.Id == "char.hero");

        var service = new ExportService(_composer);
        Assert.Throws<InvalidOperationException>(
            () => service.ExportCharacter(project, _root, hero, new CharacterExportOptions
            {
                Views = ["Down"],
                AnimationId = "anim.missing",
            }, Path.Combine(_root, "out2")));
    }

    [Theory]
    [InlineData("missing-pose")]
    [InlineData("empty-frames")]
    [InlineData("invalid-duration")]
    [InlineData("invalid-fps")]
    public void ExportCharacter_InvalidAnimation_DoesNotCreateOutput(string failure)
    {
        var project = ProjectTemplates.ClassicTopDown46();
        _store.Save(project, _root);
        StarterContentFactory.CreateHumanoidDemo(project, _root, _library);
        new BehaviorLibraryService().InstallPresets(project);
        var hero = project.Characters.Single(c => c.Id == "char.hero");
        var walk = project.Animations.Single(a => a.Id == "walk");
        switch (failure)
        {
            case "missing-pose": walk.Frames[0].PoseId = "pose.missing"; break;
            case "empty-frames": walk.Frames.Clear(); break;
            case "invalid-duration": walk.Frames[0].DurationTicks = 0; break;
            case "invalid-fps": walk.Fps = 0; break;
        }

        string output = Path.Combine(_root, $"invalid-{failure}");
        Assert.Throws<InvalidOperationException>(() => new ExportService(_composer).ExportCharacter(
            project, _root, hero,
            new CharacterExportOptions { AnimationId = walk.Id, Views = ["Down"] }, output));
        Assert.False(Directory.Exists(output));
    }

    [Fact]
    public void ExportCharacter_UnsupportedDirection_DoesNotCreateOutput()
    {
        var project = ProjectTemplates.ClassicTopDown46();
        _store.Save(project, _root);
        StarterContentFactory.CreateHumanoidDemo(project, _root, _library);
        var hero = project.Characters.Single(c => c.Id == "char.hero");
        string output = Path.Combine(_root, "invalid-direction");

        Assert.Throws<InvalidOperationException>(() => new ExportService(_composer).ExportCharacter(
            project, _root, hero,
            new CharacterExportOptions { Views = ["Diagonal"] }, output));
        Assert.False(Directory.Exists(output));
    }

    [Fact]
    public void ExportCharacter_GodotWithoutFrames_RejectsBrokenPackage()
    {
        var project = ProjectTemplates.ClassicTopDown46();
        _store.Save(project, _root);
        StarterContentFactory.CreateHumanoidDemo(project, _root, _library);
        new BehaviorLibraryService().InstallPresets(project);
        var hero = project.Characters.Single(c => c.Id == "char.hero");
        string output = Path.Combine(_root, "invalid-godot");

        Assert.Throws<InvalidOperationException>(() => new ExportService(_composer).ExportCharacter(
            project, _root, hero,
            new CharacterExportOptions
            {
                AnimationId = "walk",
                Views = ["Down"],
                IncludeGodot = true,
                IncludeFrames = false,
            }, output));
        Assert.False(Directory.Exists(output));
    }
}
