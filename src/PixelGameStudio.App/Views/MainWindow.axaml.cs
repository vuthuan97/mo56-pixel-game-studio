using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PixelGameStudio.App.ViewModels;

namespace PixelGameStudio.App.Views;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _playTimer;
    private readonly DispatcherTimer _autosaveTimer;

    public MainWindow()
    {
        InitializeComponent();
        // Aseprite-style transparency checker behind the sprite preview canvas.
        if (this.FindControl<Border>("PreviewCanvasCard") is { } canvasCard)
        {
            canvasCard.Background = CreateCheckerBrush();
        }
        // Polling playback clock: PlayTick() applies the animation's own FPS cadence.
        _playTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _playTimer.Tick += (_, _) => (DataContext as MainViewModel)?.PlayTick();
        _playTimer.Start();
        // Phase 11 autosave: keep a recovery copy while the session is dirty.
        _autosaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _autosaveTimer.Tick += (_, _) => (DataContext as MainViewModel)?.AutosaveNow();
        _autosaveTimer.Start();
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    /// <summary>16px tile (8px cells) tiled infinitely — marks the transparent area of the preview canvas.</summary>
    private static ImageBrush CreateCheckerBrush()
    {
        const int size = 16, cell = 8;
        var bitmap = new WriteableBitmap(new global::Avalonia.PixelSize(size, size), new global::Avalonia.Vector(96, 96),
            global::Avalonia.Platform.PixelFormat.Rgba8888);
        byte[] pixels = new byte[size * size * 4];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int i = (y * size + x) * 4;
                bool lightCell = ((x / cell + y / cell) & 1) == 0;
                // #E9EDF2 light cell, #FFFFFF dark cell, fully opaque
                pixels[i] = lightCell ? (byte)0xE9 : (byte)0xFF;
                pixels[i + 1] = lightCell ? (byte)0xED : (byte)0xFF;
                pixels[i + 2] = lightCell ? (byte)0xF2 : (byte)0xFF;
                pixels[i + 3] = 0xFF;
            }
        }

        using (var frame = bitmap.Lock())
        {
            Marshal.Copy(pixels, 0, frame.Address, pixels.Length);
        }

        return new ImageBrush(bitmap)
        {
            TileMode = TileMode.Tile,
            DestinationRect = new global::Avalonia.RelativeRect(
                new global::Avalonia.Rect(0, 0, size, size), global::Avalonia.RelativeUnit.Absolute),
            Stretch = Stretch.None,
        };
    }

    private void OnNewProjectClick(object? sender, RoutedEventArgs e)
    {
        ViewModel?.NewProject();
    }

    private async void OnOpenProjectClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        IReadOnlyList<IStorageFolder> folders = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Chọn thư mục project",
                AllowMultiple = false,
            });
        if (folders.Count > 0 && ViewModel.OpenProject(folders[0].Path.LocalPath))
        {
            ViewModel.EnterEditor();
        }
        else if (folders.Count > 0)
        {
            ViewModel.LauncherMessage = ViewModel.Status;
        }
    }

    /// <summary>Launcher: create a new project — identity form first, then pick the save folder.</summary>
    private async void OnCreateNewProjectClick(object? sender, RoutedEventArgs e)
    {
        await CreateNewProjectFlowAsync();
    }

    /// <summary>Shared create flow: identity dialog → save folder → provision environment.</summary>
    private async Task CreateNewProjectFlowAsync()
    {
        if (ViewModel is null)
        {
            return;
        }

        var dialog = new NewProjectDialog();
        bool? confirmed = await dialog.ShowDialog<bool?>(this);
        if (confirmed != true)
        {
            return;
        }

        IReadOnlyList<IStorageFolder> folders = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Chọn thư mục lưu project",
                AllowMultiple = false,
            });
        if (folders.Count == 0)
        {
            return; // user canceled — nothing is created
        }

        string root = folders[0].Path.LocalPath;
        if (File.Exists(Path.Combine(root, "project.pgsproj")))
        {
            ViewModel.LauncherMessage = $"Thư mục '{root}' đã chứa project — chọn thư mục khác để tránh ghi đè.";
            ViewModel.Status = ViewModel.LauncherMessage;
            return;
        }

        ViewModel.CreateNewProject(dialog.ProjectName, dialog.SelectedGenre, dialog.SelectedPerspective,
            dialog.CanvasWidth, dialog.CanvasHeight);
        ViewModel.SaveProjectAs(root);
        if (ViewModel.HasSaveRoot)
        {
            // Project is on disk now — generate the base-body placeholder art so
            // the initial character is visible immediately.
            ViewModel.EnsureBaseBodyArt();
            ViewModel.EnterEditor();
        }
        else
        {
            ViewModel.LauncherMessage = ViewModel.Status;
        }
    }

    private void OnShowLauncherClick(object? sender, RoutedEventArgs e)
    {
        ViewModel?.ShowLauncher();
    }

    private void OnRecentListDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        if (sender is ListBox { SelectedItem: RecentProjectItemViewModel recent })
        {
            ViewModel.OpenRecentProject(recent.Path);
        }
    }

    private async void OnSaveProjectAsClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is null || !ViewModel.HasProject)
        {
            return;
        }

        IReadOnlyList<IStorageFolder> folders = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Chọn thư mục lưu project",
                AllowMultiple = false,
            });
        if (folders.Count > 0)
        {
            ViewModel.SaveProjectAs(folders[0].Path.LocalPath);
        }
    }

    private async void OnRecoverAutosaveClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        IReadOnlyList<IStorageFolder> folders = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Chọn thư mục project chứa autosave",
                AllowMultiple = false,
            });
        if (folders.Count > 0)
        {
            ViewModel.TryRecoverProject(folders[0].Path.LocalPath);
        }
    }

    private void OnSaveProjectClick(object? sender, RoutedEventArgs e)
    {
        ViewModel?.SaveCurrentProject();
    }

    private void OnRunValidationClick(object? sender, RoutedEventArgs e)
    {
        ViewModel?.ValidationInspector.Run();
    }

    private async void OnExportCharacterClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        IReadOnlyList<IStorageFolder> folders = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Chọn thư mục export",
                AllowMultiple = false,
            });
        if (folders.Count > 0)
        {
            ViewModel.ExportInspector.Run(folders[0].Path.LocalPath);
        }
    }

    private async void OnImportAssetClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Chọn PNG để import",
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("PNG") { Patterns = ["*.png"] }],
            });
        if (files.Count > 0)
        {
            ViewModel.ImportAsset(files[0].Path.LocalPath);
        }
    }

    private void OnDeleteAssetClick(object? sender, RoutedEventArgs e)
    {
        ViewModel?.DeleteSelectedAsset();
    }

    private async void OnExportBehaviorClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is null || !ViewModel.HasSelectedBehavior)
        {
            return;
        }

        IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Lưu behavior template",
            SuggestedFileName = "behavior_template.json",
            DefaultExtension = "json",
        });
        if (file is not null)
        {
            ViewModel.ExportSelectedBehavior(file.Path.LocalPath);
        }
    }

    private async void OnImportBehaviorClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Chọn behavior template (.json)",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("JSON") { Patterns = ["*.json"] }],
        });
        if (files.Count > 0)
        {
            ViewModel.ImportBehaviorTemplate(files[0].Path.LocalPath);
        }
    }
}
