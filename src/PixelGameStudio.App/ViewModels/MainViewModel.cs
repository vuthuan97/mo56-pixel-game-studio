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
using Avalonia.Media.Imaging;
using System.Collections.ObjectModel;

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
        SelectedAnimationId = null;
        IsPlaying = false;
        ProvisionProjectContent();
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
        project.Style.CharacterRendererVersion = 2;
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
                a.Id.StartsWith("part.torso.", StringComparison.OrdinalIgnoreCase));
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

        bool hasFaceCombinations = _project.Assets.Any(a =>
            a.Id.StartsWith("part.face.combo.", StringComparison.OrdinalIgnoreCase));
        bool hasModernHair = _project.Assets.Any(a =>
            a.Id.StartsWith("part.hair_front.hui-cua.", StringComparison.OrdinalIgnoreCase)) &&
            _project.Assets.Any(a =>
                a.Id.StartsWith("part.hair_front.khong-toc.", StringComparison.OrdinalIgnoreCase));
        bool rendererNeedsRefresh = _project.Style.CharacterRenderer.Equals("ReferenceGrid", StringComparison.OrdinalIgnoreCase) &&
                                    _project.Style.CharacterRendererVersion < 2;
        if (!hasFaceCombinations || !hasModernHair || rendererNeedsRefresh)
        {
            StarterContentFactory.CreateBaseBody(_project, _projectRoot, _assetLibrary, CurrentCharacter);
            RefreshAssets();
        }
    }

    partial void OnSelectedCharacterIdChanged(string? value)
    {
        CharacterInspector.RefreshFromCharacter();
        RebuildEquipmentSlots();
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
            options.AddRange(_assetLibrary
                .Search(_project, tags: [$"slot:{slot.Id}"])
                .Select(a => a.Id));

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
            vm.OnChanged = UpdateEquipment;
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

            // Character exists but the project isn't saved yet — there are no
            // asset files on disk, so the empty-base body renders as a blank canvas.
            RenderBlankCanvas();
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
        Preview = output.ToWriteableBitmap();
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
            Preview = output.ToWriteableBitmap();
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
        Preview = output.ToWriteableBitmap();
        OnPropertyChanged(nameof(Preview));
        old?.Dispose();

        string previewTag = GamePreview ? " • game-scale 360x640" : $" • {SelectedScale}x";
        Status = $"{ProjectName} • nhân vật thân trống{previewTag} — lưu project rồi import PNG/asset để lên hình.";
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

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _showOnionSkin;

    [ObservableProperty]
    private string? _selectedBehaviorGroup;

    [ObservableProperty]
    private bool _gamePreview;

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
            return animation is { Fps: > 0 } ? 1000.0 / animation.Fps : 160;
        }
    }

    public bool HasCurrentFrameMarkers =>
        TimelineFrames.FirstOrDefault(f => f.Index == CurrentFrameIndex)?.HasMarkers == true;

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

        int next = CurrentFrameIndex + 1;
        if (next >= animation.Frames.Count)
        {
            if (!animation.Loop)
            {
                IsPlaying = false;
                next = animation.Frames.Count - 1;
            }
            else
            {
                next = 0;
            }
        }

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
        var states = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string pair in PoseEditState.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int eq = pair.IndexOf('=', StringComparison.Ordinal);
            if (eq > 0)
            {
                states[pair[..eq].Trim()] = pair[(eq + 1)..].Trim();
            }
        }

        pose.Parts[partId] = new PartPose(PoseEditOffsetX, PoseEditOffsetY, PoseEditHidden)
        {
            States = states,
        };
        MarkDirty();
        RenderPreview();
        Status = $"Đã áp pose cho '{partId}' trên pose '{pose.Id}' (frame #{CurrentFrameIndex}).";
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
    [
        "Project", "Character", "Rig", "Equipment", "Animation", "Behavior", "Validation", "Export",
    ];

    [ObservableProperty]
    private string _selectedWorkspace = "Character";

    [ObservableProperty]
    private bool _isDirty;

    partial void OnIsDirtyChanged(bool value)
    {
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(DirtyIndicator));
    }

    public string WindowTitle => LauncherVisible
        ? "Pixel Game Studio — Quản lý project"
        : $"Pixel Game Studio — {ProjectName}{(IsDirty ? " ●" : "")}";

    public string DirtyIndicator => IsDirty ? "● có thay đổi chưa lưu (autosave đang giữ bản khôi phục)" : "đã lưu";

    [ObservableProperty]
    private string? _selectedRigId;

    public ExportInspectorViewModel ExportInspector { get; }

    public ValidationInspectorViewModel ValidationInspector { get; }

    private readonly ProjectInspectorViewModel _projectInspector;
    private readonly RigInspectorViewModel _rigInspector;
    private readonly AnimationInspectorViewModel _animationInspector;
    private readonly BehaviorInspectorViewModel _behaviorInspector;
    private readonly ValidationInspectorViewModel _validationInspector;
    private readonly ExportInspectorViewModel _exportInspector;

    public CharacterInspectorViewModel CharacterInspector { get; }

    public object? Inspector => SelectedWorkspace switch
    {
        "Project" => _projectInspector,
        "Character" => CharacterInspector,
        "Rig" => _rigInspector,
        "Equipment" => EquipmentInspector,
        "Animation" => _animationInspector,
        "Behavior" => _behaviorInspector,
        "Validation" => _validationInspector,
        "Export" => _exportInspector,
        _ => null,
    };

    public bool TimelineVisible => SelectedWorkspace is "Animation" or "Behavior";

    /// <summary>Direct access for inspector wrappers; throws when no project is open.</summary>
    public Project ProjectObject => _project ?? throw new InvalidOperationException("Chưa có project.");

    public EquipmentInspectorViewModel EquipmentInspector { get; }

    public bool CanUndo => _project is not null && _undoRedo.CanUndo;

    public bool CanRedo => _project is not null && _undoRedo.CanRedo;

    partial void OnSelectedWorkspaceChanged(string value)
    {
        OnPropertyChanged(nameof(Inspector));
        OnPropertyChanged(nameof(TimelineVisible));
        if (value is "Validation")
        {
            _validationInspector.Run();
        }
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
        IsDirty = true;
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
            IsDirty = true;
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
            IsDirty = true;
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
        MarkDirty();
        return result;
    }

    public void AutosaveNow()
    {
        if (_project is null || _projectRoot is null || !IsDirty)
        {
            return;
        }

        _store.Autosave(_project, _projectRoot);
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
