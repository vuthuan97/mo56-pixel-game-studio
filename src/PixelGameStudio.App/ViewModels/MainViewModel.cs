using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixelGameStudio.App.Rendering;
using PixelGameStudio.App.Services;
using PixelGameStudio.Core.Primitives;
using PixelGameStudio.Assets;
using PixelGameStudio.Assets.Composition;
using PixelGameStudio.Behaviors;
using PixelGameStudio.Export;
using PixelGameStudio.App.ViewModels.Inspectors;
using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Animation;
using PixelGameStudio.Domain.Character;
using PixelGameStudio.Domain.Behaviors;
using PixelGameStudio.Domain.Assets;
using PixelGameStudio.Domain.Profiles;
using PixelGameStudio.Domain.Templates;
using PixelGameStudio.ProjectSystem;
using PixelGameStudio.Validation;
using PixelGameStudio.Rendering;
using PixelGameStudio.Rendering.Procedural;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace PixelGameStudio.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly LegacySpriteRenderer _renderer = new();
    private readonly ProjectStore _store = new();
    private readonly AssetLibraryService _assetLibrary = new();
    private readonly ThumbnailCache _thumbnailCache;
    private readonly RigSpriteComposer _composer;
    private readonly BehaviorLibraryService _behaviorLibrary = new();
    private readonly ExportService _exportService;
    private readonly UndoRedoService _undoRedo;
    private readonly RecentProjectsStore _recentStore = new();
    private Project? _project;
    private string? _projectRoot;

    /// <summary>Sprite directions follow the project's ViewProfile (4/8 hướng, side-view…).</summary>
    public IReadOnlyList<string> Directions => _project is not null && _project.View.Directions.Count > 0
        ? _project.View.Directions
        : (IReadOnlyList<string>)[ "Down", "Up", "Left", "Right" ];
    public IReadOnlyList<string> Poses { get; } = LegacyPosePresets.Poses.Keys.ToList();
    public IReadOnlyList<string> Modes { get; } = ["Normal", "Grayscale", "Silhouette", "Part Debug", "Anchor Debug"];
    public IReadOnlyList<int> Scales { get; } = [1, 2, 4, 10];

    [ObservableProperty]
    private string _selectedDirection = "Down";

    partial void OnSelectedDirectionChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            SelectedDirection = _project?.View.Directions.FirstOrDefault()
                ?? _project?.View.DefaultDirection
                ?? "Down";
            return;
        }

        RenderPreview();
    }

    [ObservableProperty]
    private string _selectedPose = "idle_0";

    [ObservableProperty]
    private string _selectedMode = "Normal";

    [ObservableProperty]
    private int _selectedScale = 4;

    [ObservableProperty]
    private WriteableBitmap? _preview;

    [ObservableProperty]
    private string _status = "Pixel Game Studio — renderer online";

    [ObservableProperty]
    private string _projectName = "New Project";

    [ObservableProperty]
    private string _projectSummary = "Chưa có project";

    [ObservableProperty]
    private string? _assetQuery;

    [ObservableProperty]
    private string? _assetTagFilter;

    public ObservableCollection<AssetItemViewModel> AssetItems { get; } = [];

    public ObservableCollection<ActionTemplateItemViewModel> ActionTemplateItems { get; } = [];

    public ObservableCollection<ActionTemplateItemViewModel> FilteredActionTemplateItems { get; } = [];

    public ObservableCollection<string> ActionGenerationResults { get; } = [];

    public IReadOnlyList<string> ActionGroupOptions { get; } =
        ["Tất cả", .. ActionTemplateCatalog.All.Select(template => template.Group).Distinct(StringComparer.Ordinal)];

    [ObservableProperty]
    private string _actionQuery = string.Empty;

    [ObservableProperty]
    private string _selectedActionGroup = "Tất cả";

    partial void OnActionQueryChanged(string value) => RefreshFilteredActionTemplates();

    partial void OnSelectedActionGroupChanged(string value) => RefreshFilteredActionTemplates();

    public int SelectedActionCount => CurrentCharacter?.SelectedActionIds.Count ?? 0;

    public string SelectedActionCountText => $"Đã chọn {SelectedActionCount} hành động";

    [ObservableProperty]
    private ActionTemplateItemViewModel? _selectedActionTemplate;

    [ObservableProperty]
    private bool _isActionGenerationRunning;

    [ObservableProperty]
    private double _actionGenerationProgress;

    [ObservableProperty]
    private string _actionGenerationStatus = string.Empty;

    private CancellationTokenSource? _actionGenerationCancellation;

    public MainViewModel()
    {
        _thumbnailCache = new ThumbnailCache(_assetLibrary);
        _composer = new RigSpriteComposer(_assetLibrary);
        _exportService = new ExportService(_composer);
        _undoRedo = new UndoRedoService(_store);
        _renderer.SetAnchorStore(new LegacyAnchorStore());
        _projectInspector = new ProjectInspectorViewModel(this);
        _rigInspector = new RigInspectorViewModel(this);
        _animationInspector = new AnimationInspectorViewModel(this);
        _behaviorInspector = new BehaviorInspectorViewModel(this);
        _validationInspector = new ValidationInspectorViewModel(this);
        ExportInspector = new ExportInspectorViewModel(this);
        _exportInspector = ExportInspector;
        ValidationInspector = new ValidationInspectorViewModel(this);
        _validationInspector = ValidationInspector;
        CharacterInspector = new CharacterInspectorViewModel(this);
        EquipmentInspector = new EquipmentInspectorViewModel(this);
        _workspaceInspectorRouter = new WorkspaceInspectorRouter(
            _projectInspector,
            CharacterInspector,
            EquipmentInspector,
            _animationInspector,
            _behaviorInspector,
            _exportInspector,
            LibraryInspector,
            BackgroundInspector);
        PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(SelectedDirection):
                case nameof(SelectedPose):
                case nameof(SelectedMode):
                case nameof(SelectedScale):
                    RenderPreview();
                    break;
                case nameof(ProjectName):
                    if (_project is not null && _project.Name != ProjectName)
                    {
                        _project.Name = ProjectName;
                        UpdateSummary();
                    }

                    break;
                case nameof(AssetQuery):
                case nameof(AssetTagFilter):
                    RefreshAssets();
                    break;
            }
        };
        NewProject();
        SelectedWorkspace = "Character";
        LoadRecentProjects();
        Status = "Màn hình chính: chọn project gần đây hoặc tạo project mới.";
        RenderPreview();
    }

    [RelayCommand]
    public void NewProject()
    {
        _project = ProjectTemplates.ClassicTopDown46();
        ApplyReferenceCharacterStyle(_project);
        _projectRoot = null;
        ProjectName = _project.Name;
        _undoRedo.Attach(_project);
        IsDirty = false;
        UpdateSummary();
        RefreshAssets();
        AnimationOptions.Clear();
        TimelineFrames.Clear();
        BehaviorItems.Clear();
        foreach (ActionTemplateItemViewModel item in ActionTemplateItems)
        {
            item.Thumbnail?.Dispose();
        }
        ActionTemplateItems.Clear();
        SelectedAnimationId = null;
        IsPlaying = false;
        ProvisionProjectContent();
        ExportInspector.RefreshChoices();
        Status = _projectRoot is null
            ? "Đã tạo project mới với nhân vật nền — lưu project để sinh asset thân mặc định, hoặc dùng 'Tạo nội dung mẫu'."
            : "Đã tạo project mới với nhân vật nền.";
    }

    /// <summary>
    /// Creates a project from the new-project dialog: identity first (name,
    /// genre, perspective, canvas), then the environment is provisioned
    /// (rig, presets, base character) and body art once saved.
    /// </summary>
    public void CreateNewProject(string name, string genre, string perspective, int canvasWidth, int canvasHeight)
    {
        _project = ProjectTemplates.ClassicTopDown46();
        ApplyReferenceCharacterStyle(_project);
        _project.Name = string.IsNullOrWhiteSpace(name) ? "New Top-Down Project" : name.Trim();
        _project.Game.Genre = genre;
        _project.View = perspective switch
        {
            KnownPerspectives.TopDown8 => ViewProfile.TopDown8(),
            KnownPerspectives.Diagonal4 => ViewProfile.Diagonal4(),
            KnownPerspectives.SideView2 => ViewProfile.SideView2(),
            KnownPerspectives.Frontal => ViewProfile.Frontal(),
            _ => ViewProfile.TopDown4(),
        };
        _project.Pixels.CanvasWidth = Math.Clamp(canvasWidth, 8, 1024);
        _project.Pixels.CanvasHeight = Math.Clamp(canvasHeight, 8, 1024);
        ProjectName = _project.Name;
        _undoRedo.Attach(_project);
        IsDirty = false;
        UpdateSummary();
        RefreshAssets();
        AnimationOptions.Clear();
        TimelineFrames.Clear();
        BehaviorItems.Clear();
        SelectedAnimationId = null;
        IsPlaying = false;
        ProvisionProjectContent();
    }

    private static void ApplyReferenceCharacterStyle(Project project)
    {
        project.Style.CharacterRenderer = "ReferenceGrid";
        // The factory writes the version only after files are generated
        // successfully. New unsaved projects therefore do not claim to have a
        // current asset library yet.
        project.Style.CharacterRendererVersion = 0;
        project.Style.OutlineStyle = "Selective1px";
        project.Style.OutlineColorHex = "#191126FF";
        project.Style.LightDirection = "TopLeft";
        project.Style.ShadowLevels = 1;
        project.Style.HighlightLevels = 1;
    }

    /// <summary>
    /// Makes any project immediately usable: humanoid rig, pose/animation/behavior
    /// presets and at least one base character. When the project
    /// has a save root, the base character also gets procedural body art so it
    /// renders instead of an empty canvas. Idempotent — skips what already exists.
    /// </summary>
    private void ProvisionProjectContent()
    {
        if (_project is null)
        {
            return;
        }

        RigDefinition rig = RigTemplates.HumanoidTopDown4();
        bool changed = false;
        if (!_project.Rigs.Any(r => r.Id == rig.Id))
        {
            _project.Rigs.Add(rig);
            changed = true;
        }

        if (_project.Poses.Count == 0 || _project.Animations.Count == 0 || _project.Behaviors.Count == 0)
        {
            _behaviorLibrary.InstallPresets(_project);
            changed = true;
        }

        if (!_project.Characters.Any())
        {
            var character = new CharacterEntity
            {
                Id = "char.hero",
                Name = "Nhân vật mới",
                RigId = rig.Id,
            };
            foreach (PartNode part in rig.Parts)
            {
                if (part.Id != "weapon")
                {
                    character.Appearance.Add(new PartAppearance { PartId = part.Id });
                }
            }

            _project.Characters.Add(character);
            changed = true;
        }

        if (changed)
        {
            if (_projectRoot is not null)
            {
                try
                {
                    _store.Save(_project, _projectRoot);
                    IsDirty = false;
                }
                catch
                {
                    // provisioning is best-effort; the regular save flow reports errors
                    MarkDirty();
                }
            }
            else
            {
                MarkDirty();
            }
        }

        // Bare/empty characters get placeholder body art when the project has a
        // save root, so the character is visible right after opening.
        EnsureBaseBodyArt();
        EnsureAppearanceVariants();

        SelectedCharacterId ??= _project.Characters.FirstOrDefault()?.Id;
        SelectedAnimationId ??= _project.Animations.FirstOrDefault()?.Id;
        if (!_project.View.Directions.Contains(SelectedDirection, StringComparer.Ordinal))
        {
            SelectedDirection = _project.View.DefaultDirection;
        }

        OnPropertyChanged(nameof(Directions));
        RefreshCharacterOptions();
        RefreshAssets();
        RefreshAnimationOptions();
        RefreshBehaviorItems();
        RefreshActionTemplates();
        RebuildEquipmentSlots();
        RebuildTimeline();
        OnPropertyChanged(nameof(Inspector));
        RenderPreview();
    }

    /// <summary>
    /// Gives the current character a visible base body: when it has no part art
    /// at all, wires the placeholder art library onto it (generating the library
    /// first if the project does not have one yet). Requires a save root.
    /// </summary>
    public bool EnsureBaseBodyArt()
    {
        if (_project is null || _projectRoot is null || CurrentCharacter is null)
        {
            return false;
        }

        bool hasAnyPartArt = CurrentCharacter.Appearance.Any(a => a.AssetRef is not null || a.ViewAssets.Count > 0);
        if (hasAnyPartArt)
        {
            return false;
        }

        try
        {
            Checkpoint();
            bool libraryExists = _project.Assets.Any(a =>
                a.Tags.Contains("source:starter-template", StringComparer.OrdinalIgnoreCase));
            StarterContentResult result = StarterContentFactory.CreateBaseBody(
                _project, _projectRoot, _assetLibrary, CurrentCharacter, generateFiles: !libraryExists);
            _store.Save(_project, _projectRoot);
            IsDirty = false;
            RefreshAssets();
            RebuildEquipmentSlots();
            RenderPreview();
            Status = libraryExists
                ? $"Đã gắn thân nền cho '{CurrentCharacter.Name}'."
                : $"Đã sinh thư viện art nền ({result.AssetCount} asset) và gắn cho '{CurrentCharacter.Name}'.";
            return true;
        }
        catch (Exception ex)
        {
            Status = $"Lỗi tạo thân mặc định: {ex.Message}";
            return false;
        }
    }

    public void SaveProjectAs(string rootDirectory)
    {
        if (_project is null)
        {
            return;
        }

        _project.Name = ProjectName;
        IReadOnlyList<string> issues = _project.Validate();
        try
        {
            _store.Save(_project, rootDirectory);
            _projectRoot = rootDirectory;
            _undoRedo.Attach(_project);
            IsDirty = false;
            UpdateSummary();
            RefreshAssets();
            RefreshCharacterOptions();
            SelectedCharacterId ??= _project.Characters.FirstOrDefault()?.Id;
            Status = issues.Count == 0
                ? $"Đã lưu project vào '{rootDirectory}'."
                : $"Đã lưu project (có {issues.Count} cảnh báo validation: {issues[0]})";
            _recentStore.Upsert(rootDirectory, _project.Name);
            LoadRecentProjects();
        }
        catch (Exception ex)
        {
            Status = $"Lỗi lưu project: {ex.Message}";
        }
    }

    public void SaveProject(string rootDirectory)
    {
        if (_project is null)
        {
            return;
        }

        _project.Name = ProjectName;
        try
        {
            _store.Save(_project, rootDirectory);
            _projectRoot = rootDirectory;
            UpdateSummary();
            Status = $"Đã lưu project vào '{rootDirectory}'.";
            _recentStore.Upsert(rootDirectory, _project.Name);
            LoadRecentProjects();
        }
        catch (Exception ex)
        {
            Status = $"Lỗi lưu project: {ex.Message}";
        }
    }

    public bool HasProject => _project is not null;

    /// <summary>True when the project has a save location on disk.</summary>
    public bool HasSaveRoot => _projectRoot is not null;

    // ---- Launcher (màn hình chính — danh sách project gần đây) ----

    [ObservableProperty]
    private bool _launcherVisible = true;

    [ObservableProperty]
    private string _launcherMessage = "";

    public bool EditorVisible => !LauncherVisible;

    partial void OnLauncherVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(EditorVisible));
        OnPropertyChanged(nameof(WindowTitle));
        if (value)
        {
            LoadRecentProjects();
        }
    }

    public ObservableCollection<RecentProjectItemViewModel> RecentProjects { get; } = [];

    public bool HasRecentProjects => RecentProjects.Count > 0;

    public string HasRecentProjectsText => RecentProjects.Count == 0
        ? "Chưa có project gần đây — tạo project mới hoặc mở từ thư mục để bắt đầu."
        : "Bấm 'Mở' (hoặc nhấp đúp) để vào trình chỉnh sửa.";

    private void LoadRecentProjects()
    {
        RecentProjects.Clear();
        foreach (RecentProjectEntry entry in _recentStore.Load())
        {
            bool exists = Directory.Exists(entry.Path);
            RecentProjects.Add(new RecentProjectItemViewModel(
                entry.Path,
                entry.Name,
                exists ? $"Mở lần cuối: {entry.LastOpenedUtc.ToLocalTime():dd/MM/yyyy HH:mm}" : "⚠ Thư mục không còn tồn tại",
                exists,
                new RelayCommand(() => _ = OpenRecentProject(entry.Path)),
                new RelayCommand(() => RemoveRecentByPath(entry.Path))));
        }

        OnPropertyChanged(nameof(HasRecentProjects));
        OnPropertyChanged(nameof(HasRecentProjectsText));
    }

    public bool OpenRecentProject(string path)
    {
        LauncherMessage = "";
        if (!Directory.Exists(path))
        {
            LauncherMessage = $"Thư mục project không tồn tại: {path}";
            return false;
        }

        if (OpenProject(path))
        {
            LauncherVisible = false;
            return true;
        }

        LauncherMessage = Status;
        return false;
    }

    public void RemoveRecentByPath(string path)
    {
        _recentStore.Remove(path);
        LoadRecentProjects();
    }

    public void EnterEditor() => LauncherVisible = false;

    public void ShowLauncher() => LauncherVisible = true;

    public void SaveCurrentProject()
    {
        if (_project is null || _projectRoot is null)
        {
            Status = "Project chưa có vị trí lưu — dùng Save Project As…";
            return;
        }

        SaveProject(_projectRoot);
    }

    public bool OpenProject(string rootDirectory)
    {
        try
        {
            _project = _store.Open(rootDirectory);
            _projectRoot = rootDirectory;
            ProjectName = _project.Name;
            _undoRedo.Attach(_project);
            IsDirty = false;
            UpdateSummary();
            RefreshAssets();
            ProvisionProjectContent();
            RefreshCharacterOptions();
            SelectedCharacterId = _project.Characters.FirstOrDefault()?.Id;
            ExportInspector.RefreshChoices();
            IReadOnlyList<string> issues = _project.Validate();
            Status = issues.Count == 0
                ? $"Đã mở project '{_project.Name}' từ '{rootDirectory}'."
                : $"Đã mở project '{_project.Name}' với {issues.Count} cảnh báo validation.";
            _recentStore.Upsert(rootDirectory, _project.Name);
            LoadRecentProjects();
            return true;
        }
        catch (Exception ex)
        {
            Status = $"Lỗi mở project: {ex.Message}";
            return false;
        }
    }

    public void TryRecoverProject(string rootDirectory)
    {
        try
        {
            if (_store.TryRecover(rootDirectory, out Project? recovered) && recovered is not null)
            {
                _project = recovered;
                _projectRoot = rootDirectory;
                ProjectName = recovered.Name;
                UpdateSummary();
                RefreshAssets();
                ExportInspector.RefreshChoices();
                Status = $"Đã khôi phục bản autosave của '{recovered.Name}'.";
            }
        }
        catch (Exception ex)
        {
            Status = $"Lỗi khôi phục autosave: {ex.Message}";
        }
    }

    public void ImportAsset(string sourcePngPath)
    {
        if (_project is null || _projectRoot is null)
        {
            Status = "Cần lưu project (Save Project As…) trước khi import asset.";
            return;
        }

        try
        {
            Checkpoint();
            AssetImportResult result = _assetLibrary.Import(_project, _projectRoot, sourcePngPath, new AssetImportOptions());
            MarkDirty();
            RefreshAssets();
            Status = result.Warnings.Count == 0
                ? $"Đã import asset '{result.Definition.Id}'."
                : $"Đã import asset '{result.Definition.Id}': {string.Join(" ", result.Warnings)}";
        }
        catch (Exception ex)
        {
            Status = $"Lỗi import: {ex.Message}";
        }
    }

    public void DeleteSelectedAsset()
    {
        if (_project is null || _projectRoot is null || SelectedAsset is null)
        {
            return;
        }

        string id = SelectedAsset.Id;
        Checkpoint();
        _assetLibrary.Delete(_project, _projectRoot, id);
        MarkDirty();
        RefreshAssets();
        Status = $"Đã xóa asset '{id}'.";
    }

    public void RefreshAssets()
    {
        AssetItems.Clear();
        if (_project is null || _projectRoot is null)
        {
            return;
        }

        IEnumerable<string>? tags = string.IsNullOrWhiteSpace(AssetTagFilter)
            ? null
            : AssetTagFilter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        IReadOnlyList<AssetDefinition> assets = _assetLibrary.Search(_project, AssetQuery, tags);
        foreach (AssetDefinition asset in assets)
        {
            string meta = $"{asset.Type} • {asset.CanvasWidth}x{asset.CanvasHeight} • z{asset.ZIndex}";
            string? thumbPath = null;
            try
            {
                thumbPath = _thumbnailCache.GetOrCreate(_project, _projectRoot, asset);
            }
            catch
            {
                // thumbnail failure is non-fatal; the row still lists text-only
            }

            AssetItems.Add(new AssetItemViewModel(asset.Id, asset.DisplayName, meta, thumbPath ?? string.Empty));
        }

        OnPropertyChanged(nameof(AssetCountText));
    }

    public string AssetCountText => _projectRoot is null ? "0 asset" : $"{AssetItems.Count} asset";

    [ObservableProperty]
    private AssetItemViewModel? _selectedAsset;

    [ObservableProperty]
    private string? _selectedCharacterId;

    public ObservableCollection<string> CharacterOptions { get; } = [];

    /// <summary>Combo-friendly roster (id + display name) mirroring CharacterOptions.</summary>
    public ObservableCollection<CharacterOptionItem> CharacterDisplayItems { get; } = [];

    private void RefreshCharacterOptions()
    {
        string? current = SelectedCharacterId;
        CharacterOptions.Clear();
        CharacterDisplayItems.Clear();
        foreach (CharacterEntity character in _project?.Characters ?? Enumerable.Empty<CharacterEntity>())
        {
            CharacterOptions.Add(character.Id);
            CharacterDisplayItems.Add(new CharacterOptionItem(
                character.Id,
                string.IsNullOrWhiteSpace(character.Name) ? character.Id : character.Name));
        }

        // Re-assert the selection (null round-trip) so the ComboBox SelectedValue
        // rebinds after the roster swap instead of showing a blank picker.
        if (current is not null)
        {
            SelectedCharacterId = null;
            SelectedCharacterId = current;
        }
    }

    public ObservableCollection<EquipmentSlotViewModel> EquipmentSlots { get; } = [];

    [RelayCommand]
    public void CreateStarterContent()
    {
        if (_project is null || _projectRoot is null)
        {
            Status = "Cần lưu project (Save Project As…) trước khi tạo nội dung mẫu.";
            return;
        }

        Checkpoint();
        StarterContentResult result = StarterContentFactory.CreateHumanoidDemo(_project, _projectRoot, _assetLibrary);
        _store.Save(_project, _projectRoot);
        MarkDirty();
        SelectedRigId = result.RigId;
        OnPropertyChanged(nameof(Inspector));
        CharacterOptions.Clear();
        foreach (CharacterEntity character in _project.Characters)
        {
            CharacterOptions.Add(character.Id);
        }

        SelectedCharacterId = result.CharacterId;
        Status = $"Đã tạo nội dung mẫu: {result.AssetCount} asset, rig '{result.RigId}'.";
    }

    public CharacterEntity? CurrentCharacter =>
        _project?.Characters.FirstOrDefault(c => c.Id == SelectedCharacterId);

    /// <summary>
    /// Backfills appearance assets introduced after an older project was
    /// created. This keeps new hair and face controls usable without requiring
    /// the user to recreate the whole project manually.
    /// </summary>
    public void EnsureAppearanceVariants()
    {
        if (_project is null || _projectRoot is null || CurrentCharacter is null)
        {
            return;
        }

        bool rendererNeedsRefresh = _project.Style.CharacterRenderer.Equals("ReferenceGrid", StringComparison.OrdinalIgnoreCase) &&
                                    _project.Style.CharacterRendererVersion < StarterContentFactory.ReferenceGridRendererVersion;
        bool hasGeneratedLibrary = _project.Assets.Any(a =>
            a.Tags.Contains("source:starter-template", StringComparer.OrdinalIgnoreCase));
        if (!hasGeneratedLibrary || rendererNeedsRefresh)
        {
            StarterContentFactory.CreateBaseBody(_project, _projectRoot, _assetLibrary, CurrentCharacter);
            RefreshAssets();
        }
    }

    partial void OnSelectedCharacterIdChanged(string? value)
    {
        CharacterInspector.RefreshFromCharacter();
        RebuildEquipmentSlots();
        RefreshActionTemplates();
        if (value is not null)
        {
            RenderPreview();
        }
    }

    /// <summary>v1 multi-character: adds another base character to the project.</summary>
    [RelayCommand]
    public void AddCharacter()
    {
        if (_project is null)
        {
            return;
        }

        RigDefinition? rig = CurrentCharacter is not null
            ? _project.Rigs.FirstOrDefault(r => r.Id == CurrentCharacter.RigId)
            : _project.Rigs.FirstOrDefault();
        if (rig is null)
        {
            Status = "Project chưa có rig — không thể thêm nhân vật.";
            return;
        }

        Checkpoint();
        int n = _project.Characters.Count + 1;
        string id = $"char.hero{n}";
        while (_project.Characters.Any(c => c.Id.Equals(id, StringComparison.Ordinal)))
        {
            n++;
            id = $"char.hero{n}";
        }

        var character = new CharacterEntity
        {
            Id = id,
            Name = $"Nhân vật {n}",
            RigId = rig.Id,
        };
        foreach (PartNode part in rig.Parts)
        {
            if (part.Id != "weapon")
            {
                character.Appearance.Add(new PartAppearance { PartId = part.Id });
            }
        }

        _project.Characters.Add(character);
        RefreshCharacterOptions();
        SelectedCharacterId = character.Id;
        MarkDirty();
        EnsureBaseBodyArt();
        Status = $"Đã thêm '{character.Name}' — combo Nhân vật để chuyển giữa các nhân vật.";
    }

    [RelayCommand]
    public void DuplicateCharacter()
    {
        if (_project is null || CurrentCharacter is null)
        {
            return;
        }

        Checkpoint();
        CharacterEntity copy = JsonSerializer.Deserialize<CharacterEntity>(
            JsonSerializer.Serialize(CurrentCharacter)) ?? throw new InvalidOperationException("Cannot duplicate character.");
        int n = _project.Characters.Count + 1;
        string id = $"char.hero{n}";
        while (_project.Characters.Any(c => c.Id.Equals(id, StringComparison.Ordinal)))
        {
            n++;
            id = $"char.hero{n}";
        }

        copy.Id = id;
        // Output animations belong to the source character; the duplicate keeps
        // template choices but must generate its own poses/frames.
        copy.ActionIds.Clear();
        copy.Name = $"{CurrentCharacter.Name} (copy)";
        _project.Characters.Add(copy);
        RefreshCharacterOptions();
        SelectedCharacterId = copy.Id;
        MarkDirty();
        RebuildEquipmentSlots();
        RenderPreview();
        Status = $"Duplicated '{copy.Name}'.";
    }

    public void RefreshActionTemplates()
    {
        string? selectedId = SelectedActionTemplate?.Id;
        foreach (ActionTemplateItemViewModel old in ActionTemplateItems)
        {
            old.Thumbnail?.Dispose();
        }
        ActionTemplateItems.Clear();
        if (_project is null || CurrentCharacter is null)
        {
            SelectedActionTemplate = null;
            FilteredActionTemplateItems.Clear();
            OnPropertyChanged(nameof(SelectedActionCount));
            OnPropertyChanged(nameof(SelectedActionCountText));
            return;
        }

        foreach (ActionAvailability item in ActionTemplateCatalog.Evaluate(_project, CurrentCharacter))
        {
            var choice = new ActionTemplateItemViewModel(
                item.Template.Id,
                item.Template.DisplayName,
                item.Template.Group,
                item.IsAvailable,
                item.Reason,
                CurrentCharacter.SelectedActionIds.Contains(item.Template.Id, StringComparer.Ordinal));
            if (_projectRoot is not null)
            {
                try
                {
                    PixelBuffer pixels = ComposePreviewCharacter(_project, CurrentCharacter,
                        ActionTemplateCatalog.CreatePreviewPose(item.Template.Id, _project));
                    choice.Thumbnail = PixelOps.NearestScale(pixels, 2).ToWriteableBitmap();
                }
                catch
                {
                    // A missing art asset must not hide the template or its availability reason.
                }
            }
            choice.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ActionTemplateItemViewModel.IsSelected))
                {
                    SetActionSelected(choice.Id, choice.IsSelected);
                }
            };
            ActionTemplateItems.Add(choice);
        }

        SelectedActionTemplate = ActionTemplateItems.FirstOrDefault(item => item.Id == selectedId)
            ?? ActionTemplateItems.FirstOrDefault();
        RefreshFilteredActionTemplates();
        OnPropertyChanged(nameof(SelectedActionCount));
        OnPropertyChanged(nameof(SelectedActionCountText));
    }

    private void RefreshFilteredActionTemplates()
    {
        FilteredActionTemplateItems.Clear();
        foreach (ActionTemplateItemViewModel item in ActionTemplateItems)
        {
            if (SelectedActionGroup != "Tất cả" && item.Group != SelectedActionGroup)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(ActionQuery) &&
                !item.DisplayName.Contains(ActionQuery, StringComparison.OrdinalIgnoreCase) &&
                !item.Id.Contains(ActionQuery, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            FilteredActionTemplateItems.Add(item);
        }
    }

    private void SetActionSelected(string templateId, bool selected)
    {
        CharacterEntity? character = CurrentCharacter;
        if (character is null || character.SelectedActionIds.Contains(templateId, StringComparer.Ordinal) == selected)
        {
            return;
        }

        Checkpoint();
        if (selected)
        {
            character.SelectedActionIds.Add(templateId);
        }
        else
        {
            character.SelectedActionIds.Remove(templateId);
        }

        MarkDirty();
        OnPropertyChanged(nameof(SelectedActionCount));
        OnPropertyChanged(nameof(SelectedActionCountText));
    }

    [RelayCommand]
    public void PreviewActionTemplate()
    {
        if (_project is null || _projectRoot is null || CurrentCharacter is null ||
            SelectedActionTemplate is null)
        {
            Status = "Cần lưu project và chọn nhân vật để xem thử hành động.";
            return;
        }

        try
        {
            RenderComposedCharacter(_project, CurrentCharacter,
                ActionTemplateCatalog.CreatePreviewPose(SelectedActionTemplate.Id, _project));
            Status = $"Xem thử {SelectedActionTemplate.DisplayName} (không đổi lựa chọn).";
        }
        catch (Exception ex)
        {
            Status = $"Không xem thử được: {ex.Message}";
        }
    }

    [RelayCommand]
    public void SelectAllCompatibleActions()
    {
        foreach (ActionTemplateItemViewModel item in FilteredActionTemplateItems.Where(item => item.IsAvailable))
        {
            item.IsSelected = true;
        }
    }

    [RelayCommand]
    public async Task GenerateSelectedActionsAsync()
    {
        if (_project is null || CurrentCharacter is null || IsActionGenerationRunning)
        {
            return;
        }

        Project project = _project;
        CharacterEntity character = CurrentCharacter;
        string[] selectedIds = character.SelectedActionIds.Distinct(StringComparer.Ordinal).ToArray();
        if (selectedIds.Length == 0)
        {
            ActionGenerationStatus = "Chưa chọn hành động nào.";
            return;
        }

        _actionGenerationCancellation?.Dispose();
        _actionGenerationCancellation = new CancellationTokenSource();
        CancellationToken cancellationToken = _actionGenerationCancellation.Token;
        ActionGenerationResults.Clear();
        IsActionGenerationRunning = true;
        ActionGenerationProgress = 0;
        try
        {
            for (int i = 0; i < selectedIds.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string templateId = selectedIds[i];
                ActionAvailability? availability = ActionTemplateCatalog.Evaluate(project, character)
                    .FirstOrDefault(item => item.Template.Id == templateId);
                if (availability is null || !availability.IsAvailable)
                {
                    ActionGenerationResults.Add($"{templateId}: {availability?.Reason ?? "chưa có mẫu"}");
                    continue;
                }

                Checkpoint();
                int actionIndex = i;
                var progress = new Progress<double>(value =>
                    ActionGenerationProgress = (actionIndex + value) / selectedIds.Length);
                try
                {
                    int priorAnimationCount = project.Animations.Count;
                    AnimationDefinition animation = await Task.Run(() =>
                        ActionTemplateCatalog.Generate(project, character, templateId, cancellationToken, progress),
                        cancellationToken);
                    if (project.Animations.Count > priorAnimationCount)
                    {
                        MarkDirty();
                        ActionGenerationResults.Add($"{templateId}: tạo {animation.Frames.Count} frame → {animation.Id}");
                    }
                    else
                    {
                        ActionGenerationResults.Add($"{templateId}: giữ bản đã có {animation.Id} (không ghi đè).");
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    ActionGenerationResults.Add($"{templateId}: lỗi {ex.Message}");
                }
            }

            ActionGenerationProgress = 1;
            ActionGenerationStatus = $"Đã xử lý {ActionGenerationResults.Count}/{selectedIds.Length} hành động.";
        }
        catch (OperationCanceledException)
        {
            ActionGenerationStatus = "Đã hủy; hành động đang xử lý không ghi dữ liệu dang dở.";
        }
        finally
        {
            IsActionGenerationRunning = false;
            _actionGenerationCancellation?.Dispose();
            _actionGenerationCancellation = null;
            RefreshAnimationOptions();
            RefreshActionTemplates();
            RebuildTimeline();
        }
    }

    public void GenerateActionTemplate()
    {
        if (_project is null || CurrentCharacter is null || SelectedActionTemplate is null)
        {
            Status = "Select an action template first.";
            return;
        }

        try
        {
            Checkpoint();
            AnimationDefinition animation = ActionTemplateCatalog.Generate(
                _project, CurrentCharacter, SelectedActionTemplate.Id);
            MarkDirty();
            RefreshAnimationOptions();
            SelectedAnimationId = animation.Id;
            RefreshBehaviorItems();
            RefreshActionTemplates();
            RebuildTimeline();
            Status = $"Generated action '{animation.DisplayName}' with {animation.Frames.Count} real frames.";
        }
        catch (Exception ex)
        {
            Status = $"Action generation failed: {ex.Message}";
        }
    }

    [RelayCommand]
    public Task GenerateActionTemplateAsync() => GenerateActionWithModeAsync(ActionRegenerationMode.Preserve);

    [RelayCommand]
    public Task CreateNewActionVersionAsync() => GenerateActionWithModeAsync(ActionRegenerationMode.CreateNewVersion);

    [RelayCommand]
    public Task ReplaceGeneratedActionAsync() => GenerateActionWithModeAsync(ActionRegenerationMode.Replace);

    private async Task GenerateActionWithModeAsync(ActionRegenerationMode mode)
    {
        if (_project is null || CurrentCharacter is null || SelectedActionTemplate is null)
        {
            ActionGenerationStatus = "Select an available action template first.";
            return;
        }

        if (IsActionGenerationRunning)
        {
            return;
        }

        _actionGenerationCancellation?.Dispose();
        _actionGenerationCancellation = new CancellationTokenSource();
        CancellationToken cancellationToken = _actionGenerationCancellation.Token;
        string templateId = SelectedActionTemplate.Id;
        Project project = _project;
        CharacterEntity character = CurrentCharacter;
        IsActionGenerationRunning = true;
        ActionGenerationProgress = 0;
        Checkpoint();
        ActionGenerationStatus = "Generating poses and frames…";

        try
        {
            var progress = new Progress<double>(value =>
            {
                ActionGenerationProgress = value;
                ActionGenerationStatus = $"Generating… {value:P0}";
            });
            AnimationDefinition animation = await Task.Run(
                () => ActionTemplateCatalog.Generate(project, character, templateId, cancellationToken, progress, mode),
                cancellationToken);
            MarkDirty();
            RefreshAnimationOptions();
            SelectedAnimationId = animation.Id;
            RefreshBehaviorItems();
            RefreshActionTemplates();
            RebuildTimeline();
            ActionGenerationProgress = 1;
            ActionGenerationStatus = mode == ActionRegenerationMode.Preserve
                ? $"Giữ bản đã có hoặc tạo {animation.DisplayName} ({animation.Frames.Count} frame), không ghi đè bản chỉnh tay."
                : $"{(mode == ActionRegenerationMode.Replace ? "Đã thay thế" : "Đã tạo bản mới")} {animation.Id}.";
        }
        catch (OperationCanceledException)
        {
            ActionGenerationStatus = "Generation cancelled; no partial action was committed.";
        }
        catch (Exception ex)
        {
            ActionGenerationStatus = $"Generation failed: {ex.Message}";
        }
        finally
        {
            IsActionGenerationRunning = false;
            _actionGenerationCancellation?.Dispose();
            _actionGenerationCancellation = null;
        }
    }

    [RelayCommand]
    public void CancelActionGeneration() => _actionGenerationCancellation?.Cancel();

    [RelayCommand]
    public void DeleteCharacter()
    {
        if (_project is null || CurrentCharacter is null)
        {
            return;
        }

        if (_project.Characters.Count <= 1)
        {
            Status = "Project must keep at least one character.";
            return;
        }

        string removedName = CurrentCharacter.Name;
        Checkpoint();
        _project.Characters.Remove(CurrentCharacter);
        RefreshCharacterOptions();
        SelectedCharacterId = _project.Characters.FirstOrDefault()?.Id;
        MarkDirty();
        RebuildEquipmentSlots();
        RenderPreview();
        Status = $"Deleted '{removedName}'.";
    }

    private void RebuildEquipmentSlots()
    {
        EquipmentSlots.Clear();
        CharacterEntity? character = CurrentCharacter;
        RigDefinition? rig = _project?.Rigs.FirstOrDefault(r => r.Id == character?.RigId);
        if (_project is null || character is null || rig is null)
        {
            return;
        }

        foreach (EquipmentSlotDef slot in rig.EquipmentSlots)
        {
            var options = new List<string> { EquipmentSlotViewModel.NoneOption };
            IReadOnlyList<AssetDefinition> slotAssets = _assetLibrary
                .Search(_project, tags: [$"slot:{slot.Id}"]);
            options.AddRange(slotAssets.Select(a => a.Id));

            EquippedItem? equipped = character.Equipment.FirstOrDefault(e => e.SlotId == slot.Id);
            string? current = equipped is not null &&
                              equipped.ViewAssets.TryGetValue(SelectedDirection, out string? viewAsset) &&
                              viewAsset is not null
                ? viewAsset
                : equipped?.AssetId;

            var vm = new EquipmentSlotViewModel(slot.Id, slot.DisplayName, options)
            {
                SelectedOption = current ?? EquipmentSlotViewModel.NoneOption,
            };
            vm.OptionItems.Clear();
            vm.OptionItems.Add(new EquipmentOptionViewModel(
                EquipmentSlotViewModel.NoneOption,
                EquipmentSlotViewModel.NoneOption,
                null,
                "empty"));
            foreach (AssetDefinition asset in slotAssets)
            {
                string? thumbPath = null;
                try
                {
                    thumbPath = _thumbnailCache.GetOrCreate(_project, _projectRoot ?? string.Empty, asset, 48);
                }
                catch
                {
                    // Missing thumbnails must not prevent equipment selection.
                }

                WriteableBitmap? thumbnail = null;
                if (!string.IsNullOrWhiteSpace(thumbPath))
                {
                    try
                    {
                        thumbnail = PngCodec.Decode(File.OpenRead(thumbPath)).ToWriteableBitmap();
                    }
                    catch
                    {
                        thumbnail = null;
                    }
                }

                vm.OptionItems.Add(new EquipmentOptionViewModel(
                    asset.Id,
                    asset.DisplayName,
                    thumbnail,
                    string.Join(", ", asset.Tags)));
            }
            vm.OnChanged = UpdateEquipment;
            vm.OnAssignSelected = AssignSelectedAssetToSlot;
            EquipmentSlots.Add(vm);
        }
    }

    private void UpdateEquipment(string slotId, string? assetId)
    {
        CharacterEntity? character = CurrentCharacter;
        if (character is null)
        {
            return;
        }

        Checkpoint();
        character.Equipment.RemoveAll(e => e.SlotId == slotId);
        if (!string.IsNullOrEmpty(assetId))
        {
            character.Equipment.Add(new EquippedItem(slotId, assetId));
        }

        MarkDirty();
        RenderPreview();
    }

    private void AssignSelectedAssetToSlot(string slotId)
    {
        if (_project is null || CurrentCharacter is null || SelectedAsset is null)
        {
            Status = "Select an asset in the contextual browser first.";
            return;
        }

        EquipmentSlotDef? slot = _project.Rigs
            .FirstOrDefault(rig => rig.Id == CurrentCharacter.RigId)?.FindSlot(slotId);
        if (slot is null)
        {
            return;
        }

        AssetDefinition? selectedDefinition = _project.Assets.FirstOrDefault(asset => asset.Id == SelectedAsset.Id);
        if (selectedDefinition is null)
        {
            Status = $"Asset '{SelectedAsset.Id}' is no longer in the project library.";
            return;
        }

        if (slot.AllowedTags.Any(tag => !selectedDefinition.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
        {
            Status = $"Asset '{SelectedAsset.Id}' does not match slot '{slot.DisplayName}'.";
            return;
        }

        UpdateEquipment(slotId, SelectedAsset.Id);
        RebuildEquipmentSlots();
        Status = $"Assigned '{selectedDefinition.DisplayName}' to '{slot.DisplayName}'.";
    }

    [RelayCommand]
    private void RenderPreview()
    {
        CharacterEntity? character = CurrentCharacter;
        if (character is not null)
        {
            if (_project is not null && _projectRoot is not null)
            {
                RenderComposedCharacter(_project, character, GetCurrentPose());
                return;
            }

            // A new project has no asset files yet, but its procedural starter
            // source can still render the character and preview modes.
            RenderUnsavedCharacter(_project!);
            return;
        }

        if (!LegacyPosePresets.Poses.TryGetValue(SelectedPose, out var pose))
        {
            return;
        }

        var spec = new LegacySpriteSpec { Direction = SelectedDirection };
        var mode = SelectedMode switch
        {
            "Grayscale" => LegacyRenderMode.Grayscale,
            "Silhouette" => LegacyRenderMode.Silhouette,
            "Part Debug" => LegacyRenderMode.PartDebug,
            "Anchor Debug" => LegacyRenderMode.AnchorDebug,
            _ => LegacyRenderMode.Normal,
        };

        PixelBuffer rendered = _renderer.Render(spec, pose, mode);
        PixelBuffer scaled = SelectedScale == 1
            ? rendered
            : PixelOps.NearestScale(rendered, SelectedScale);
        PixelBuffer output = GamePreview ? BuildGameScaleViewport(scaled) : scaled;

        WriteableBitmap? old = Preview;
        Preview = (GamePreview ? output : PreviewBackdrop.Apply(output, SelectedPreviewBackground)).ToWriteableBitmap();
        OnPropertyChanged(nameof(Preview));
        old?.Dispose();

        string previewTag = GamePreview ? " • game-scale 360x640" : $" • {SelectedScale}x";
        Status = $"{ProjectName} • {SelectedDirection} • {SelectedPose} • {SelectedMode}{previewTag} (nearest-neighbor)";
    }

    private void RenderComposedCharacter(Project project, CharacterEntity character, PoseDefinition? pose)
    {
        try
        {
            PixelBuffer composed = ComposePreviewCharacter(project, character, pose);

            if (ShowOnionSkin && pose is not null && SelectedAnimationId is not null)
            {
                PoseDefinition? previousPose = GetPoseAt(CurrentFrameIndex - 1);
                if (previousPose is not null)
                {
                    PixelBuffer previous = ComposePreviewCharacter(project, character, previousPose);
                    PixelBuffer faded = PixelOps.Fade(previous, 56);
                    PixelOps.Composite(faded, composed);
                    composed = faded;
                }
            }

            PixelBuffer output;
            if (GamePreview)
            {
                output = BuildGameScaleViewport(composed);
            }
            else
            {
                output = SelectedScale == 1
                    ? composed
                    : PixelOps.NearestScale(composed, SelectedScale);
            }

            WriteableBitmap? old = Preview;
            Preview = (GamePreview ? output : PreviewBackdrop.Apply(output, SelectedPreviewBackground)).ToWriteableBitmap();
            OnPropertyChanged(nameof(Preview));
            old?.Dispose();
            string animationTag = SelectedAnimationId is null ? "" : $" • {SelectedAnimationId}[{CurrentFrameIndex}]";
            string previewTag = GamePreview ? " • game-scale 360x640" : $" • {SelectedScale}x";
            Status = $"{ProjectName} • {character.Name} • {SelectedDirection}{animationTag}{previewTag} (asset-driven compose)";
        }
        catch (Exception ex)
        {
            Status = $"Lỗi compose: {ex.Message}";
        }
    }

    private PixelBuffer ComposePreviewCharacter(Project project, CharacterEntity character, PoseDefinition? pose)
    {
        var options = new CompositionOptions
        {
            View = SelectedDirection,
            Pose = pose,
            ApplyOutline = SelectedMode != "Part Debug",
        };

        if (SelectedMode == "Part Debug")
        {
            var result = new PixelBuffer(project.Pixels.CanvasWidth, project.Pixels.CanvasHeight);
            foreach ((string name, PixelBuffer image) in _composer.ComposeLayers(project, _projectRoot!, character, options))
            {
                PixelBuffer debugLayer = image.Clone();
                PixelOps.Recolor(debugLayer, DebugColorForPart(name));
                PixelOps.Composite(result, debugLayer);
            }

            return result;
        }

        PixelBuffer composed = _composer.Compose(project, _projectRoot!, character, options);
        return SelectedMode switch
        {
            "Grayscale" => PixelOps.ToGrayscale(composed),
            "Silhouette" => ToSilhouetteCopy(composed),
            "Anchor Debug" => AddAnchorDebug(project, composed, SelectedDirection),
            _ => composed,
        };
    }

    private static PixelBuffer ToSilhouetteCopy(PixelBuffer source)
    {
        PixelBuffer result = source.Clone();
        PixelOps.ToSilhouette(result);
        return result;
    }

    private static Rgba32 DebugColorForPart(string name) => name switch
    {
        "hair_back" or "hair_front" => new Rgba32(255, 80, 80, 255),
        "body" or "torso" => new Rgba32(70, 130, 255, 255),
        "head" => new Rgba32(255, 220, 80, 255),
        "face" => new Rgba32(255, 240, 140, 255),
        "left_arm" or "right_arm" => new Rgba32(80, 220, 120, 255),
        "left_leg" or "right_leg" => new Rgba32(180, 90, 240, 255),
        "weapon_back" or "weapon_front" or "slot.main_hand" => new Rgba32(255, 255, 255, 255),
        _ => new Rgba32(80, 220, 220, 255),
    };

    private static PixelBuffer AddAnchorDebug(Project project, PixelBuffer source, string direction)
    {
        PixelBuffer result = source.Clone();
        var overlay = new PixelBuffer(result.Width, result.Height);
        var colors = new Dictionary<string, Rgba32>
        {
            ["head"] = new(255, 80, 80, 255),
            ["chest"] = new(80, 160, 255, 255),
            ["left_hand"] = new(80, 220, 120, 255),
            ["right_hand"] = new(80, 220, 120, 255),
            ["left_foot"] = new(180, 90, 240, 255),
            ["right_foot"] = new(180, 90, 240, 255),
            ["weapon"] = new(255, 255, 255, 255),
            ["accessory"] = new(255, 110, 210, 255),
        };

        if (LegacyAnchorStore.Defaults.TryGetValue(direction, out Dictionary<string, (int X, int Y)>? defaults))
        {
            foreach ((string name, (int x, int y)) in defaults)
            {
                DrawAnchor(overlay, x, y, colors.GetValueOrDefault(name, new Rgba32(80, 220, 220, 255)));
            }
        }
        else
        {
            RigDefinition? rig = project.Rigs.FirstOrDefault();
            if (rig is not null)
            {
                foreach (AnchorPoint anchor in rig.Anchors)
                {
                    DrawAnchor(overlay, anchor.OffsetX, anchor.OffsetY,
                        colors.GetValueOrDefault(anchor.Id.Replace("anchor.", "", StringComparison.Ordinal),
                            new Rgba32(80, 220, 220, 255)));
                }
            }
        }

        PixelOps.Composite(result, overlay);
        return result;
    }

    private static void DrawAnchor(PixelBuffer target, int x, int y, Rgba32 color)
    {
        PixelDraw.FillRect(target, x - 1, y, x + 1, y, color);
        PixelDraw.FillRect(target, x, y - 1, x, y + 1, color);
    }

    /// <summary>Game-scale preview: 360x640 viewport, background, 32px grid, sprite at 4x, NPC silhouettes (legacy game-preview flow).</summary>
    private PixelBuffer BuildGameScaleViewport(PixelBuffer composed)
    {
        Rgba32 bg = GameBackgrounds.GetValueOrDefault(GameBackground, GameBackgrounds["Cỏ"]);
        var viewport = new PixelBuffer(360, 640);
        viewport.Fill(bg);

        var gridLines = new PixelBuffer(360, 640);
        var gridColor = new Rgba32((byte)(bg.R / 2), (byte)(bg.G / 2), (byte)(bg.B / 2), 70);
        for (int y = 0; y < 640; y += 32)
        {
            PixelDraw.HLine(gridLines, 0, y, 359, gridColor);
        }

        for (int x = 0; x < 360; x += 32)
        {
            for (int y = 0; y < 640; y++)
            {
                gridLines[x, y] = gridColor;
            }
        }

        PixelOps.Composite(viewport, gridLines);

        PixelBuffer sprite4x = PixelOps.NearestScale(composed, 4);
        PixelOps.Composite(viewport, sprite4x, (360 - sprite4x.Width) / 2, 320 - (sprite4x.Height / 2));

        if (CurrentCharacter is not null && _project is not null && _projectRoot is not null)
        {
            PixelBuffer silhouette = _composer.Compose(_project, _projectRoot, CurrentCharacter,
                new CompositionOptions { View = SelectedDirection, ApplyOutline = false });
            PixelOps.ToSilhouette(silhouette);
            PixelBuffer npc = PixelOps.NearestScale(silhouette, 2);
            foreach ((int x, int y) in new[] { (70, 220), (250, 250), (90, 420), (250, 430) })
            {
                PixelOps.Composite(viewport, npc, x, y);
            }
        }

        return viewport;
    }

    /// <summary>Blank canvas honoring scale / game-preview, shown before the project has a save root.</summary>
    private void RenderBlankCanvas()
    {
        var blank = new PixelBuffer(_project?.Pixels.CanvasWidth ?? 32, _project?.Pixels.CanvasHeight ?? 46);
        PixelBuffer output = GamePreview
            ? BuildGameScaleViewport(blank)
            : SelectedScale == 1 ? blank : PixelOps.NearestScale(blank, SelectedScale);

        WriteableBitmap? old = Preview;
        Preview = (GamePreview ? output : PreviewBackdrop.Apply(output, SelectedPreviewBackground)).ToWriteableBitmap();
        OnPropertyChanged(nameof(Preview));
        old?.Dispose();

        string previewTag = GamePreview ? " • game-scale 360x640" : $" • {SelectedScale}x";
        Status = $"{ProjectName} • nhân vật thân trống{previewTag} — lưu project rồi import PNG/asset để lên hình.";
    }

    private void RenderUnsavedCharacter(Project project)
    {
        LegacyPose pose = LegacyPosePresets.Poses.GetValueOrDefault(SelectedPose) ?? LegacyPosePresets.Poses["idle_0"];
        var spec = new LegacySpriteSpec
        {
            Direction = SelectedDirection,
            CanvasWidth = project.Pixels.CanvasWidth,
            CanvasHeight = project.Pixels.CanvasHeight,
        };
        foreach (string key in spec.Equipment.Keys.ToList())
        {
            spec.Equipment[key] = "Không";
        }

        ICharacterLayerRenderer renderer = project.Style.CharacterRenderer.Equals("ReferenceGrid", StringComparison.OrdinalIgnoreCase)
            ? new ReferenceGridSpriteRenderer()
            : _renderer;
        List<(string Name, PixelBuffer Image)> layers = renderer.RenderLayers(spec, pose);
        PixelBuffer composed = new(project.Pixels.CanvasWidth, project.Pixels.CanvasHeight);
        CharacterBuildProfile? build = CurrentCharacter?.Build;
        foreach ((string name, PixelBuffer sourceImage) in layers)
        {
            PixelBuffer image = sourceImage;
            string? partId = name switch
            {
                "body" => "torso",
                "left_arm" or "right_arm" or "left_leg" or "right_leg" or
                "head" or "face" or "hair_front" or "hair_back" => name,
                _ => null,
            };
            if (build is not null && partId is not null)
            {
                image = RigSpriteComposer.ApplyBuildShape(image, build, partId);
                (int offsetX, int offsetY) = build.PartOffset(partId);
                if (offsetX != 0 || offsetY != 0)
                {
                    var shifted = new PixelBuffer(image.Width, image.Height);
                    PixelOps.Composite(shifted, image, offsetX, offsetY);
                    image = shifted;
                }
            }

            PixelOps.Composite(composed, image);
        }

        PixelBuffer output = SelectedMode switch
        {
            "Grayscale" => PixelOps.ToGrayscale(composed),
            "Silhouette" => ToSilhouetteCopy(composed),
            "Part Debug" => ComposeDebugLayers(project.Pixels.CanvasWidth, project.Pixels.CanvasHeight, layers),
            "Anchor Debug" => AddAnchorDebug(project, composed, SelectedDirection),
            _ => composed,
        };
        if (GamePreview)
        {
            output = BuildGameScaleViewport(output);
        }
        else if (SelectedScale != 1)
        {
            output = PixelOps.NearestScale(output, SelectedScale);
        }

        WriteableBitmap? old = Preview;
        Preview = (GamePreview ? output : PreviewBackdrop.Apply(output, SelectedPreviewBackground)).ToWriteableBitmap();
        OnPropertyChanged(nameof(Preview));
        old?.Dispose();
        string previewTag = GamePreview ? " • game-scale 360x640" : $" • {SelectedScale}x";
        Status = $"{ProjectName} • {SelectedDirection} • {SelectedPose} • {SelectedMode}{previewTag} (procedural preview, nearest-neighbor)";
    }

    private static PixelBuffer ComposeDebugLayers(int width, int height,
        IReadOnlyList<(string Name, PixelBuffer Image)> layers)
    {
        var result = new PixelBuffer(width, height);
        foreach ((string name, PixelBuffer image) in layers)
        {
            PixelBuffer debug = image.Clone();
            PixelOps.Recolor(debug, DebugColorForPart(name));
            PixelOps.Composite(result, debug);
        }

        return result;
    }

    private PoseDefinition? GetCurrentPose()
    {        if (_project is null || SelectedAnimationId is null)
        {
            return null;
        }

        AnimationDefinition? animation = _project.Animations.FirstOrDefault(a => a.Id == SelectedAnimationId);
        if (animation is null || animation.Frames.Count == 0)
        {
            return null;
        }

        int index = Math.Clamp(CurrentFrameIndex, 0, animation.Frames.Count - 1);
        string poseId = animation.Frames[index].PoseId;
        return _project.Poses.FirstOrDefault(p => p.Id == poseId);
    }

    private PoseDefinition? GetPoseAt(int frameIndex)
    {
        if (_project is null || SelectedAnimationId is null)
        {
            return null;
        }

        AnimationDefinition? animation = _project.Animations.FirstOrDefault(a => a.Id == SelectedAnimationId);
        if (animation is null || animation.Frames.Count == 0)
        {
            return null;
        }

        int index = ((frameIndex % animation.Frames.Count) + animation.Frames.Count) % animation.Frames.Count;
        string poseId = animation.Frames[index].PoseId;
        return _project.Poses.FirstOrDefault(p => p.Id == poseId);
    }

    [ObservableProperty]
    private string? _selectedAnimationId;

    [ObservableProperty]
    private int _currentFrameIndex;

    partial void OnCurrentFrameIndexChanged(int value)
    {
        RebuildTimelineSelection();
        RenderPreview();
        OnPropertyChanged(nameof(CurrentFrameDurationTicks));
        NotifyAnimationInterval();
    }

    partial void OnSelectedAnimationIdChanged(string? value)
    {
        CurrentFrameIndex = 0;
        RebuildTimeline();
        RenderPreview();
        OnPropertyChanged(nameof(CurrentFrameDurationTicks));
    }

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _showOnionSkin;

    [ObservableProperty]
    private string? _selectedBehaviorGroup;

    [ObservableProperty]
    private bool _gamePreview;

    public IReadOnlyList<string> PreviewBackgroundOptions { get; } = ["Ô caro", "Sáng", "Tối"];

    [ObservableProperty]
    private string _selectedPreviewBackground = "Ô caro";

    partial void OnSelectedPreviewBackgroundChanged(string value) => RenderPreview();

    [ObservableProperty]
    private string _gameBackground = "Cỏ";

    public IReadOnlyList<string> GameBackgroundOptions { get; } = ["Cỏ", "Đá", "Đất", "Tối"];

    private static readonly Dictionary<string, Rgba32> GameBackgrounds = new()
    {
        ["Cỏ"] = new(72, 120, 76, 255),
        ["Đá"] = new(110, 112, 118, 255),
        ["Đất"] = new(120, 88, 64, 255),
        ["Tối"] = new(38, 42, 58, 255),
    };

    partial void OnGamePreviewChanged(bool value) => RenderPreview();

    partial void OnGameBackgroundChanged(string value) => RenderPreview();

    public ObservableCollection<string> AnimationOptions { get; } = [];

    public ObservableCollection<string> BehaviorGroupOptions { get; } = [];

    public ObservableCollection<FrameItemViewModel> TimelineFrames { get; } = [];

    public ObservableCollection<BehaviorItemViewModel> BehaviorItems { get; } = [];

    public double AnimationIntervalMs
    {
        get
        {
            AnimationDefinition? animation = _project?.Animations.FirstOrDefault(a => a.Id == SelectedAnimationId);
            return animation is { Fps: > 0, Frames.Count: > 0 } &&
                   CurrentFrameIndex >= 0 && CurrentFrameIndex < animation.Frames.Count
                ? AnimationPlayback.FrameDurationMilliseconds(animation, CurrentFrameIndex)
                : 160;
        }
    }

    public bool HasCurrentFrameMarkers =>
        TimelineFrames.FirstOrDefault(f => f.Index == CurrentFrameIndex)?.HasMarkers == true;

    [RelayCommand]
    public void CreateAnimation()
    {
        if (_project is null || CurrentCharacter is null) return;
        try
        {
            Checkpoint();
            AnimationDefinition animation = CharacterAnimationEditor.Create(_project, CurrentCharacter, GetCurrentPose());
            MarkDirty();
            RefreshAnimationOptions();
            SelectedAnimationId = animation.Id;
            RebuildTimeline();
            Status = $"Đã tạo hoạt ảnh {animation.Id}.";
        }
        catch (Exception ex) { Status = $"Không tạo được hoạt ảnh: {ex.Message}"; }
    }

    [RelayCommand]
    public void DuplicateAnimation()
    {
        if (_project is null || CurrentCharacter is null) return;
        AnimationDefinition? source = _project.Animations.FirstOrDefault(animation => animation.Id == SelectedAnimationId);
        if (source is null) return;
        try
        {
            Checkpoint();
            AnimationDefinition animation = CharacterAnimationEditor.Duplicate(_project, CurrentCharacter, source);
            MarkDirty();
            RefreshAnimationOptions();
            SelectedAnimationId = animation.Id;
            RebuildTimeline();
            Status = $"Đã nhân bản hoạt ảnh {animation.Id}; pose tách riêng khỏi bản gốc.";
        }
        catch (Exception ex) { Status = $"Không nhân bản được hoạt ảnh: {ex.Message}"; }
    }

    [RelayCommand]
    public void SaveAnimationAsBehaviorTemplate()
    {
        if (_project is null) return;
        AnimationDefinition? animation = _project.Animations.FirstOrDefault(item => item.Id == SelectedAnimationId);
        if (animation is null) return;
        try
        {
            Checkpoint();
            BehaviorDefinition template = CharacterAnimationEditor.SaveAsBehaviorTemplate(_project, animation);
            MarkDirty();
            RefreshBehaviorItems();
            RefreshActionTemplates();
            Status = $"Đã lưu mẫu behavior {template.Id} cho hoạt ảnh {animation.Id}.";
        }
        catch (Exception ex) { Status = $"Không lưu được mẫu behavior: {ex.Message}"; }
    }

    [RelayCommand]
    public void InstallPresets()
    {
        if (_project is null || _projectRoot is null)
        {
            Status = "Cần lưu project trước khi cài preset.";
            return;
        }

        Checkpoint();
        int added = _behaviorLibrary.InstallPresets(_project);
        MarkDirty();
        RefreshAnimationOptions();
        RefreshBehaviorItems();
        Status = added == 0 ? "Preset đã có đầy đủ." : $"Đã cài {added} preset (pose/animation/behavior).";
    }

    [RelayCommand]
    public void TogglePlay()
    {
        if (SelectedAnimationId is null)
        {
            Status = "Chọn animation trước khi play.";
            return;
        }

        IsPlaying = !IsPlaying;
        OnPropertyChanged(nameof(AnimationIntervalMs));
    }

    [RelayCommand]
    public void StepNext()
    {
        AnimationDefinition? animation = _project?.Animations.FirstOrDefault(a => a.Id == SelectedAnimationId);
        if (animation is null || animation.Frames.Count == 0)
        {
            return;
        }

        IsPlaying = false;
        CurrentFrameIndex = (CurrentFrameIndex + 1) % animation.Frames.Count;
    }

    [RelayCommand]
    public void StepBack()
    {
        AnimationDefinition? animation = _project?.Animations.FirstOrDefault(a => a.Id == SelectedAnimationId);
        if (animation is null || animation.Frames.Count == 0)
        {
            return;
        }

        IsPlaying = false;
        CurrentFrameIndex = (CurrentFrameIndex - 1 + animation.Frames.Count) % animation.Frames.Count;
    }

    private long _lastPlaybackTickUtc;

    public void PlayTick()
    {
        if (!IsPlaying || _project is null)
        {
            _lastPlaybackTickUtc = 0;
            return;
        }

        long now = DateTime.UtcNow.Ticks;
        if (_lastPlaybackTickUtc == 0)
        {
            _lastPlaybackTickUtc = now;
            return;
        }

        double elapsedMs = (now - _lastPlaybackTickUtc) / (double)TimeSpan.TicksPerMillisecond;
        if (elapsedMs < AnimationIntervalMs)
        {
            return;
        }

        _lastPlaybackTickUtc = now;

        AnimationDefinition? animation = _project.Animations.FirstOrDefault(a => a.Id == SelectedAnimationId);
        if (animation is null || animation.Frames.Count == 0)
        {
            IsPlaying = false;
            return;
        }

        (int next, bool continuePlaying) = AnimationPlayback.Advance(animation, CurrentFrameIndex);
        IsPlaying = continuePlaying;
        CurrentFrameIndex = next;
    }

    // ------------------------------------------------------------------
    // Timeline frame ops + pose/marker editing (prompt 03)
    // ------------------------------------------------------------------

    private AnimationFrame? _clipboardFrame;

    public static IReadOnlyList<string> MarkerTypeOptions { get; } =
    [
        FrameMarkerTypes.Footstep, FrameMarkerTypes.Sound, FrameMarkerTypes.Hit,
        FrameMarkerTypes.SpawnEffect, FrameMarkerTypes.SpawnProjectile, FrameMarkerTypes.Interaction,
        FrameMarkerTypes.PickUp, FrameMarkerTypes.Drop, FrameMarkerTypes.Custom,
    ];

    public IReadOnlyList<string> PosePartOptions =>
        CurrentRig?.Parts.Select(p => p.Id).ToList() ?? [];

    public RigDefinition? CurrentRig =>
        _project?.Rigs.FirstOrDefault(r => r.Id == CurrentCharacter?.RigId)
        ?? _project?.Rigs.FirstOrDefault();

    private AnimationDefinition? SelectedAnimation =>
        _project?.Animations.FirstOrDefault(a => a.Id == SelectedAnimationId);

    private AnimationFrame? CurrentFrame
    {
        get
        {
            AnimationDefinition? animation = SelectedAnimation;
            if (animation is null || animation.Frames.Count == 0)
            {
                return null;
            }

            return animation.Frames[Math.Clamp(CurrentFrameIndex, 0, animation.Frames.Count - 1)];
        }
    }

    private void AfterFramesChanged()
    {
        MarkDirty();
        if (SelectedAnimation is { Frames.Count: > 0 } animation)
        {
            CurrentFrameIndex = Math.Clamp(CurrentFrameIndex, 0, animation.Frames.Count - 1);
        }

        RebuildTimeline();
        RenderPreview();
    }

    [RelayCommand]
    public void AddFrame()
    {
        if (CurrentFrame is not { } current || SelectedAnimation is null)
        {
            return;
        }

        Checkpoint();
        var frame = new AnimationFrame
        {
            PoseId = current.PoseId,
            DurationTicks = current.DurationTicks,
            Markers = current.Markers.Select(m => new FrameMarker(m.Type, m.Value)).ToList(),
        };
        SelectedAnimation.Frames.Insert(CurrentFrameIndex + 1, frame);
        CurrentFrameIndex++;
        AfterFramesChanged();
        Status = $"Đã thêm frame #{CurrentFrameIndex} (nhân bản frame hiện tại).";
    }

    [RelayCommand]
    public void DuplicateFrame() => AddFrame();

    [RelayCommand]
    public void DeleteFrame()
    {
        if (SelectedAnimation is not { Frames.Count: > 1 } animation || CurrentFrame is null)
        {
            return;
        }

        Checkpoint();
        animation.Frames.RemoveAt(CurrentFrameIndex);
        AfterFramesChanged();
        Status = $"Đã xóa frame (còn {animation.Frames.Count}).";
    }

    [RelayCommand]
    public void CopyFrame()
    {
        if (CurrentFrame is not { } current)
        {
            return;
        }

        _clipboardFrame = new AnimationFrame
        {
            PoseId = current.PoseId,
            DurationTicks = current.DurationTicks,
            Markers = current.Markers.Select(m => new FrameMarker(m.Type, m.Value)).ToList(),
        };
        Status = $"Đã copy frame #{CurrentFrameIndex} ({current.PoseId}).";
    }

    [RelayCommand]
    public void PasteFrame()
    {
        if (SelectedAnimation is null || _clipboardFrame is null)
        {
            Status = "Clipboard frame trống — Copy trước.";
            return;
        }

        Checkpoint();
        SelectedAnimation.Frames.Insert(CurrentFrameIndex + 1, new AnimationFrame
        {
            PoseId = _clipboardFrame.PoseId,
            DurationTicks = _clipboardFrame.DurationTicks,
            Markers = _clipboardFrame.Markers.Select(m => new FrameMarker(m.Type, m.Value)).ToList(),
        });
        CurrentFrameIndex++;
        AfterFramesChanged();
        Status = "Đã paste frame.";
    }

    [RelayCommand]
    public void MoveFrameLeft()
    {
        if (SelectedAnimation is null || CurrentFrameIndex <= 0 || CurrentFrame is null)
        {
            return;
        }

        Checkpoint();
        SelectedAnimation.Frames.RemoveAt(CurrentFrameIndex);
        SelectedAnimation.Frames.Insert(CurrentFrameIndex - 1, CurrentFrame);
        CurrentFrameIndex--;
        AfterFramesChanged();
    }

    [RelayCommand]
    public void MoveFrameRight()
    {
        if (SelectedAnimation is not { Frames.Count: > 0 } animation ||
            CurrentFrameIndex >= animation.Frames.Count - 1 || CurrentFrame is null)
        {
            return;
        }

        Checkpoint();
        animation.Frames.RemoveAt(CurrentFrameIndex);
        animation.Frames.Insert(CurrentFrameIndex + 1, CurrentFrame);
        CurrentFrameIndex++;
        AfterFramesChanged();
    }

    // ---- Pose editor (edits the frame's PoseDefinition — shared like legacy overrides) ----

    [ObservableProperty]
    private string? _poseEditorPart;

    [ObservableProperty]
    private int _poseEditOffsetX;

    [ObservableProperty]
    private int _poseEditOffsetY;

    [ObservableProperty]
    private bool _poseEditHidden;

    [ObservableProperty]
    private string _poseEditState = string.Empty;

    public PoseDefinition? CurrentFramePose
    {
        get
        {
            string? poseId = CurrentFrame?.PoseId;
            return poseId is null ? null : _project?.Poses.FirstOrDefault(p => p.Id == poseId);
        }
    }

    partial void OnPoseEditorPartChanged(string? value) => LoadPoseEdit();

    [RelayCommand]
    public void LoadPoseEdit()
    {
        if (PoseEditorPart is not { } partId || CurrentFramePose is not { } pose)
        {
            return;
        }

        PartPose part = pose.Parts.TryGetValue(partId, out PartPose? existing)
            ? existing
            : new PartPose();
        PoseEditOffsetX = part.OffsetX;
        PoseEditOffsetY = part.OffsetY;
        PoseEditHidden = part.Hidden;
        PoseEditState = part.States.Count > 0
            ? string.Join(";", part.States.Select(kv => $"{kv.Key}={kv.Value}"))
            : string.Empty;
    }

    [RelayCommand]
    public void ApplyPoseEdit()
    {
        if (PoseEditorPart is not { } partId || CurrentFramePose is not { } pose)
        {
            Status = "Chọn animation + frame + part trước khi áp pose.";
            return;
        }

        Checkpoint();
        PoseDefinition poseToEdit = EnsureEditableFramePose(pose);
        var states = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string pair in PoseEditState.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int eq = pair.IndexOf('=', StringComparison.Ordinal);
            if (eq > 0)
            {
                states[pair[..eq].Trim()] = pair[(eq + 1)..].Trim();
            }
        }

        poseToEdit.Parts[partId] = new PartPose(PoseEditOffsetX, PoseEditOffsetY, PoseEditHidden)
        {
            States = states,
        };
        MarkDirty();
        RenderPreview();
        Status = $"Đã áp pose cho '{partId}' trên pose '{pose.Id}' (frame #{CurrentFrameIndex}).";
    }

    private PoseDefinition EnsureEditableFramePose(PoseDefinition pose)
    {
        if (_project is null || CurrentFrame is null)
        {
            return pose;
        }

        int references = _project.Animations
            .SelectMany(animation => animation.Frames)
            .Count(frame => frame.PoseId.Equals(pose.Id, StringComparison.Ordinal));
        if (references <= 1)
        {
            return pose;
        }

        int suffix = 1;
        string cloneId;
        do
        {
            cloneId = $"{pose.Id}.edit{suffix++}";
        }
        while (_project.Poses.Any(candidate => candidate.Id.Equals(cloneId, StringComparison.Ordinal)));

        PoseDefinition clone = pose.Clone();
        clone.Id = cloneId;
        clone.DisplayName = $"{pose.DisplayName} (frame edit)";
        _project.Poses.Add(clone);
        CurrentFrame.PoseId = clone.Id;
        return clone;
    }

    public int CurrentFrameDurationTicks
    {
        get => CurrentFrame?.DurationTicks ?? 1;
        set
        {
            if (CurrentFrame is null || CurrentFrame.DurationTicks == value)
            {
                return;
            }

            Checkpoint();
            CurrentFrame.DurationTicks = Math.Clamp(value, 1, 120);
            MarkDirty();
            RebuildTimeline();
            OnPropertyChanged();
            NotifyAnimationInterval();
        }
    }

    // ---- Marker editor (current frame) ----

    [ObservableProperty]
    private string _newMarkerType = FrameMarkerTypes.Sound;

    [ObservableProperty]
    private string? _newMarkerValue;

    [RelayCommand]
    public void AddMarkerToCurrentFrame()
    {
        if (CurrentFrame is not { } frame)
        {
            return;
        }

        Checkpoint();
        frame.Markers.Add(new FrameMarker(NewMarkerType, string.IsNullOrWhiteSpace(NewMarkerValue) ? null : NewMarkerValue.Trim()));
        MarkDirty();
        RebuildTimeline();
        Status = $"Đã thêm marker {NewMarkerType} vào frame #{CurrentFrameIndex}.";
    }

    [RelayCommand]
    public void RemoveLastMarkerFromCurrentFrame()
    {
        if (CurrentFrame is not { Markers.Count: > 0 } frame)
        {
            return;
        }

        Checkpoint();
        FrameMarker removed = frame.Markers[^1];
        frame.Markers.RemoveAt(frame.Markers.Count - 1);
        MarkDirty();
        RebuildTimeline();
        Status = $"Đã xóa marker {removed.Type} khỏi frame #{CurrentFrameIndex}.";
    }

    // ---- Behavior duplicate (custom behavior creation) ----

    [RelayCommand]
    public void DuplicateSelectedBehavior()
    {
        if (_project is null || _projectRoot is null || SelectedBehavior is null)
        {
            Status = "Chọn behavior để nhân bản.";
            return;
        }

        Checkpoint();
        string newId = $"{SelectedBehavior.Id}_custom";
        int suffix = 2;
        while (_project.Behaviors.Any(b => b.Id == newId))
        {
            newId = $"{SelectedBehavior.Id}_custom{suffix++}";
        }

        BehaviorDefinition created = _behaviorLibrary.CreateCustom(_project, SelectedBehavior.Id, newId, $"{SelectedBehavior.DisplayName} (custom)");
        MarkDirty();
        RefreshBehaviorItems();
        Status = $"Đã nhân bản thành behavior tùy chỉnh '{created.Id}'.";
    }

    private void RefreshAnimationOptions()
    {
        AnimationOptions.Clear();
        if (_project is null)
        {
            return;
        }

        foreach (AnimationDefinition animation in _project.Animations.OrderBy(a => a.Id, StringComparer.Ordinal))
        {
            AnimationOptions.Add(animation.Id);
        }

        SelectedAnimationId ??= AnimationOptions.FirstOrDefault();
    }

    private void RebuildTimeline()
    {
        TimelineFrames.Clear();
        AnimationDefinition? animation = _project?.Animations.FirstOrDefault(a => a.Id == SelectedAnimationId);
        if (animation is null)
        {
            return;
        }

        for (int i = 0; i < animation.Frames.Count; i++)
        {
            AnimationFrame frame = animation.Frames[i];
            TimelineFrames.Add(new FrameItemViewModel(
                i,
                frame.PoseId,
                frame.DurationTicks,
                string.Join(", ", frame.Markers.Select(m => m.Type + (m.Value is null ? "" : $":{m.Value}")))));
        }
    }

    private void RebuildTimelineSelection()
    {
        foreach (FrameItemViewModel frame in TimelineFrames)
        {
            frame.IsCurrent = frame.Index == CurrentFrameIndex;
        }

        OnPropertyChanged(nameof(HasCurrentFrameMarkers));
    }

    public bool HasSelectedBehavior => SelectedBehavior is not null;

    public void RefreshBehaviorItems()
    {
        BehaviorItems.Clear();
        if (_project is null)
        {
            return;
        }

        BehaviorGroupOptions.Clear();
        BehaviorGroupOptions.Add("(tất cả)");
        foreach (string group in _project.Behaviors.Select(b => b.Group).Distinct(StringComparer.Ordinal).OrderBy(g => g, StringComparer.Ordinal))
        {
            BehaviorGroupOptions.Add(group);
        }

        if (SelectedBehaviorGroup is not null && !BehaviorGroupOptions.Contains(SelectedBehaviorGroup))
        {
            SelectedBehaviorGroup = "(tất cả)";
        }

        string? effectiveGroup = SelectedBehaviorGroup is "(tất cả)" or null ? null : SelectedBehaviorGroup;
        foreach (BehaviorDefinition behavior in _behaviorLibrary.ListByGroup(_project, effectiveGroup))
        {
            string binding = behavior.AnimationId is null ? "chưa gắn animation" : $"anim: {behavior.AnimationId}";
            if (behavior.HeldItemSlotId is not null)
            {
                binding += $" • held: {behavior.HeldItemSlotId}";
            }

            if (behavior.InteractionAnchorId is not null)
            {
                binding += $" • anchor: {behavior.InteractionAnchorId}";
            }

            if (behavior.Markers.Count > 0)
            {
                binding += $" • {behavior.Markers.Count} marker";
            }

            BehaviorItems.Add(new BehaviorItemViewModel(behavior.Id, behavior.DisplayName, behavior.Group, binding));
        }
    }

    partial void OnSelectedBehaviorGroupChanged(string? value) => RefreshBehaviorItems();

    public void ExportSelectedBehavior(string filePath)
    {
        if (_project is null || SelectedBehavior is null)
        {
            return;
        }

        BehaviorDefinition? behavior = _project.Behaviors.FirstOrDefault(b => b.Id == SelectedBehavior.Id);
        if (behavior is null)
        {
            return;
        }

        _behaviorLibrary.ExportTemplate(behavior, filePath);
        Status = $"Đã export behavior template '{behavior.Id}' → '{Path.GetFileName(filePath)}'.";
    }

    public void ImportBehaviorTemplate(string filePath)
    {
        if (_project is null || _projectRoot is null)
        {
            Status = "Cần lưu project trước khi import behavior template.";
            return;
        }

        try
        {
            BehaviorDefinition imported = _behaviorLibrary.ImportTemplate(filePath);
            string newId = imported.Id;
            int suffix = 2;
            while (_project.Behaviors.Any(b => b.Id == newId))
            {
                newId = $"{imported.Id}_{suffix++}";
            }

            BehaviorDefinition installed = _behaviorLibrary.Install(_project, imported, newId);
            _store.Save(_project, _projectRoot);
            RefreshBehaviorItems();
            Status = $"Đã import behavior '{installed.Id}' ({installed.DisplayName}).";
        }
        catch (Exception ex)
        {
            Status = $"Lỗi import behavior template: {ex.Message}";
        }
    }

    [ObservableProperty]
    private BehaviorItemViewModel? _selectedBehavior;

    partial void OnSelectedBehaviorChanged(BehaviorItemViewModel? value)
    {
        OnPropertyChanged(nameof(SelectedBehaviorSummary));
    }

    public string SelectedBehaviorSummary
    {
        get
        {
            if (_project is null || SelectedBehavior is null)
            {
                return "";
            }

            BehaviorDefinition? behavior = _project.Behaviors.FirstOrDefault(b => b.Id == SelectedBehavior.Id);
            return behavior is null
                ? ""
                : $"{behavior.Group} • {binding_(behavior)} • markers: {string.Join("; ", behavior.Markers.Select(m => $"{m.Type}:{m.Name}@{m.FrameIndex}"))}";
        }
    }

    private static string binding_(BehaviorDefinition b) =>
        $"animation: {b.AnimationId ?? "—"} • held: {b.HeldItemSlotId ?? "—"} • anchor: {b.InteractionAnchorId ?? "—"}";

    // ------------------------------------------------------------------
    // Workspaces (prompt 04)
    // ------------------------------------------------------------------

    public static IReadOnlyList<string> WorkspaceOptions { get; } =
    ["Project", "Character", "Library", "Background", "Export"];

    public static IReadOnlyList<string> CharacterSectionOptions { get; } =
    ["Frame", "Equipment", "Animation", "Actions"];

    [ObservableProperty]
    private string _selectedWorkspace = "Character";

    [ObservableProperty]
    private string _selectedCharacterSection = "Frame";

    [ObservableProperty]
    private bool _isDirty;

    private bool _autosaveMatchesCurrentState;

    partial void OnIsDirtyChanged(bool value)
    {
        if (!value) _autosaveMatchesCurrentState = false;
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(DirtyIndicator));
    }

    public string WindowTitle => LauncherVisible
        ? "Pixel Game Studio — Quản lý project"
        : $"Pixel Game Studio — {ProjectName}{(IsDirty ? " ●" : "")}";

    public string DirtyIndicator => IsDirty
        ? _autosaveMatchesCurrentState ? "● chưa lưu · đã có bản autosave" : "● chưa lưu · chờ autosave"
        : "đã lưu";

    [ObservableProperty]
    private string? _selectedRigId;

    public ExportInspectorViewModel ExportInspector { get; }

    public ValidationInspectorViewModel ValidationInspector { get; }

    public RigInspectorViewModel RigInspector => _rigInspector;

    private readonly ProjectInspectorViewModel _projectInspector;
    private readonly RigInspectorViewModel _rigInspector;
    private readonly AnimationInspectorViewModel _animationInspector;
    private readonly BehaviorInspectorViewModel _behaviorInspector;
    private readonly ValidationInspectorViewModel _validationInspector;
    private readonly ExportInspectorViewModel _exportInspector;
    private readonly WorkspaceInspectorRouter _workspaceInspectorRouter;

    public WorkspacePlaceholderViewModel LibraryInspector { get; } = new(
        "Library / Items",
        "This workspace is reserved for a future standalone item and prop editor. The existing browser remains available contextually in Character.");

    public WorkspacePlaceholderViewModel BackgroundInspector { get; } = new(
        "Background",
        "This workspace is reserved for the future background, terrain, tree and building editors.");

    public CharacterInspectorViewModel CharacterInspector { get; }

    public object? Inspector => _workspaceInspectorRouter.Resolve(SelectedWorkspace, SelectedCharacterSection);

    public bool CharacterWorkspaceVisible => SelectedWorkspace == "Character";

    public bool ActionsFooterVisible => SelectedWorkspace == "Character" && SelectedCharacterSection == "Actions";

    public bool TimelineVisible => SelectedWorkspace == "Character" && SelectedCharacterSection == "Animation";

    public bool ContextBrowserVisible => SelectedWorkspace == "Character" &&
                                         SelectedCharacterSection is "Equipment" or "Actions";

    public GridLength LeftDockWidth => ContextBrowserVisible ? new GridLength(280) : new GridLength(0);

    public GridLength LeftDockSplitterWidth => ContextBrowserVisible ? new GridLength(6) : new GridLength(0);

    /// <summary>Direct access for inspector wrappers; throws when no project is open.</summary>
    public Project ProjectObject => _project ?? throw new InvalidOperationException("Chưa có project.");

    public EquipmentInspectorViewModel EquipmentInspector { get; }

    public bool CanUndo => _project is not null && _undoRedo.CanUndo;

    public bool CanRedo => _project is not null && _undoRedo.CanRedo;

    partial void OnSelectedWorkspaceChanged(string value)
    {
        OnPropertyChanged(nameof(Inspector));
        OnPropertyChanged(nameof(CharacterWorkspaceVisible));
        OnPropertyChanged(nameof(ActionsFooterVisible));
        OnPropertyChanged(nameof(TimelineVisible));
        OnPropertyChanged(nameof(ContextBrowserVisible));
        OnPropertyChanged(nameof(LeftDockWidth));
        OnPropertyChanged(nameof(LeftDockSplitterWidth));
        if (value is "Project")
        {
            _validationInspector.Run();
        }
        else if (value is "Export")
        {
            ExportInspector.RefreshChoices();
        }
    }

    partial void OnSelectedCharacterSectionChanged(string value)
    {
        if (value == "Actions")
        {
            RefreshActionTemplates();
        }
        OnPropertyChanged(nameof(Inspector));
        OnPropertyChanged(nameof(ActionsFooterVisible));
        OnPropertyChanged(nameof(TimelineVisible));
        OnPropertyChanged(nameof(ContextBrowserVisible));
        OnPropertyChanged(nameof(LeftDockWidth));
        OnPropertyChanged(nameof(LeftDockSplitterWidth));
    }

    public void Checkpoint()
    {
        if (_project is not null)
        {
            _undoRedo.Checkpoint(_project);
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }
    }

    public void MarkDirty()
    {
        _autosaveMatchesCurrentState = false;
        IsDirty = true;
        OnPropertyChanged(nameof(DirtyIndicator));
    }

    [RelayCommand]
    public void Undo()
    {
        if (_project is null)
        {
            return;
        }

        Project? restored = _undoRedo.Undo(_project);
        if (restored is not null)
        {
            AdoptProject(restored, keepRoot: true);
            MarkDirty();
            Status = "Đã undo.";
        }

        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    [RelayCommand]
    public void Redo()
    {
        if (_project is null)
        {
            return;
        }

        Project? restored = _undoRedo.Redo(_project);
        if (restored is not null)
        {
            AdoptProject(restored, keepRoot: true);
            MarkDirty();
            Status = "Đã redo.";
        }

        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    /// <summary>Swaps the open project object and rebinds every collection.</summary>
    private void AdoptProject(Project project, bool keepRoot)
    {
        _project = project;
        ProjectName = project.Name;
        UpdateSummary();
        RefreshAssets();
        CharacterOptions.Clear();
        foreach (CharacterEntity character in project.Characters)
        {
            CharacterOptions.Add(character.Id);
        }

        SelectedCharacterId = project.Characters.FirstOrDefault(c => c.Id == SelectedCharacterId)?.Id
            ?? project.Characters.FirstOrDefault()?.Id;
        SelectedRigId = project.Rigs.FirstOrDefault()?.Id;
        SelectedAnimationId = project.Animations.Any(a => a.Id == SelectedAnimationId)
            ? SelectedAnimationId
            : project.Animations.FirstOrDefault()?.Id;
        RebuildEquipmentSlots();
        RebuildTimeline();
        RefreshAnimationOptions();
        RefreshBehaviorItems();
        ExportInspector.RefreshChoices();
        RenderPreview();
        OnPropertyChanged(nameof(Inspector));
    }

    public IReadOnlyList<string> RunValidation()
    {
        if (_project is null)
        {
            return ["Chưa có project."];
        }

        var issues = new List<string>(_project.Validate());
        if (_projectRoot is not null)
        {
            issues.AddRange(_assetLibrary.ValidateOnDisk(_project, _projectRoot));
            if (CurrentCharacter is not null)
            {
                issues.AddRange(CharacterQaService.CheckCharacter(
                    _project, _projectRoot, CurrentCharacter, _assetLibrary, _composer));
            }
        }

        return issues;
    }

    public ExportResult ExportCharacter(CharacterEntity character, CharacterExportOptions options, string outputDirectory)
    {
        if (_project is null || _projectRoot is null)
        {
            throw new InvalidOperationException("Chưa có project để export.");
        }

        ExportResult result = _exportService.ExportCharacter(_project, _projectRoot, character, options, outputDirectory);
        return result;
    }

    public BatchExportResult ExportBatch(
        IReadOnlyList<CharacterEntity> characters,
        IReadOnlyList<string?> animationIds,
        CharacterExportOptions options,
        string outputDirectory)
    {
        if (_project is null || _projectRoot is null)
        {
            throw new InvalidOperationException("Chưa có project để export.");
        }

        return _exportService.ExportBatch(_project, _projectRoot, characters, animationIds, options, outputDirectory);
    }

    public void AutosaveNow()
    {
        if (_project is null || _projectRoot is null || !IsDirty)
        {
            return;
        }

        _store.Autosave(_project, _projectRoot);
        _autosaveMatchesCurrentState = true;
        OnPropertyChanged(nameof(DirtyIndicator));
        Status = "Đã autosave (bản khôi phục) — nhớ Save Project để lưu chính thức.";
    }

    public void NotifyAnimationInterval() => OnPropertyChanged(nameof(AnimationIntervalMs));

    private void UpdateSummary()
    {
        if (_project is null)
        {
            ProjectSummary = "Chưa có project";
            return;
        }

        Project p = _project;
        ProjectSummary =
            $"{p.Game.Genre} • {p.View.Perspective} ({p.View.Directions.Count} hướng) • " +
            $"{p.Pixels.CanvasWidth}x{p.Pixels.CanvasHeight}px • palette {p.Palette.Colors.Count}/{p.Palette.MaxColors}";
    }
}
