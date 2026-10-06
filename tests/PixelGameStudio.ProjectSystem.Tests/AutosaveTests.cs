using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Templates;
using PixelGameStudio.ProjectSystem;
using Xunit;

namespace PixelGameStudio.ProjectSystem.Tests;

public class AutosaveTests : IDisposable
{
    private readonly string _root;

    public AutosaveTests()
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
    public void Autosave_WritesRecoveryCopyOnly()
    {
        var store = new ProjectStore();
        Project project = ProjectTemplates.ClassicTopDown46();

        store.Autosave(project, _root);

        Assert.True(File.Exists(ProjectPaths.AutosaveFile(_root)));
        Assert.False(File.Exists(ProjectPaths.ProjectFile(_root)));
    }

    [Fact]
    public void TryRecover_WhenMainMissing_ReturnsAutosave()
    {
        var store = new ProjectStore();
        Project project = ProjectTemplates.SideScroller48x32();
        project.Name = "Bản đang sửa dở";
        store.Autosave(project, _root);

        bool recovered = store.TryRecover(_root, out Project? recoveredProject);

        Assert.True(recovered);
        Assert.NotNull(recoveredProject);
        Assert.Equal("Bản đang sửa dở", recoveredProject!.Name);
    }

    [Fact]
    public void TryRecover_WhenMainIsNewer_ReturnsFalse()
    {
        var store = new ProjectStore();
        Project project = ProjectTemplates.ClassicTopDown46();

        store.Save(project, _root);
        store.Autosave(project, _root);
        // Make the autosave look older than the main save.
        File.SetLastWriteTimeUtc(ProjectPaths.AutosaveFile(_root), DateTime.UtcNow - TimeSpan.FromMinutes(5));

        Assert.False(store.TryRecover(_root, out Project? recovered));
        Assert.Null(recovered);
    }

    [Fact]
    public void TryRecover_WhenAutosaveIsNewer_ReturnsAutosave()
    {
        var store = new ProjectStore();
        Project project = ProjectTemplates.ClassicTopDown46();
        store.Save(project, _root);

        Project crashed = ProjectTemplates.ClassicTopDown46();
        crashed.Name = "Sau lần sửa chưa lưu";
        Thread.Sleep(20);
        store.Autosave(crashed, _root);

        bool recovered = store.TryRecover(_root, out Project? recoveredProject);

        Assert.True(recovered);
        Assert.Equal(crashed.Name, recoveredProject!.Name);
    }

    [Fact]
    public void TryRecover_WithoutAutosave_ReturnsFalse()
    {
        var store = new ProjectStore();
        Directory.CreateDirectory(_root);

        Assert.False(store.TryRecover(_root, out Project? recovered));
        Assert.Null(recovered);
    }

    [Fact]
    public void TryRecover_WithCorruptAutosave_ReturnsFalse()
    {
        var store = new ProjectStore();
        Directory.CreateDirectory(ProjectPaths.AutosaveDirectory(_root));
        File.WriteAllText(ProjectPaths.AutosaveFile(_root), "không phải json");

        Assert.False(store.TryRecover(_root, out Project? recovered));
        Assert.Null(recovered);
    }
}
