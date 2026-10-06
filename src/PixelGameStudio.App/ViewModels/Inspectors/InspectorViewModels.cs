using CommunityToolkit.Mvvm.ComponentModel;
using PixelGameStudio.Core.Primitives;
using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Animation;
using PixelGameStudio.Domain.Assets;
using PixelGameStudio.Domain.Behaviors;
using PixelGameStudio.Domain.Character;
using PixelGameStudio.Domain.Profiles;
using PixelGameStudio.Domain.Templates;
using PixelGameStudio.Export;

namespace PixelGameStudio.App.ViewModels.Inspectors;

/// <summary>
/// Context-sensitive inspector models for the workspace tabs. These wrap the
/// open Project: getters read it, setters checkpoint undo history and mark the
/// session dirty. No domain logic lives here — only delegation.
/// </summary>
public abstract class InspectorViewModel : ObservableObject
{
    protected InspectorViewModel(MainViewModel main) => Main = main;

    protected MainViewModel Main { get; }

    protected Project Project => Main.ProjectObject;
}

/// <summary>Project workspace: shared profiles of the whole project.</summary>
public class ProjectInspectorViewModel : InspectorViewModel
{
    public ProjectInspectorViewModel(MainViewModel main)
        : base(main)
    {
    }

    public IReadOnlyList<string> GenreOptions => GameGenres.All;

    public IReadOnlyList<string> PerspectiveOptions => KnownPerspectives.All;

    public string Genre
    {
        get => ProjectObject.Game.Genre;
        set
        {
            Main.Checkpoint();
            ProjectObject.Game.Genre = value;
            Main.MarkDirty();
            OnPropertyChanged();
        }
    }

    public string Perspective
    {
        get => ProjectObject.View.Perspective;
        set
        {
            Main.Checkpoint();
            ProjectObject.View.Perspective = value;
            Main.MarkDirty();
            OnPropertyChanged();
        }
    }

    public int CanvasWidth
    {
        get => ProjectObject.Pixels.CanvasWidth;
        set
        {
            if (value == ProjectObject.Pixels.CanvasWidth)
            {
                return;
            }

            Main.Checkpoint();
            ProjectObject.Pixels.CanvasWidth = Math.Clamp(value, 8, 1024);
            Main.MarkDirty();
            OnPropertyChanged();
        }
    }

    public int CanvasHeight
    {
        get => ProjectObject.Pixels.CanvasHeight;
        set
        {
            if (value == ProjectObject.Pixels.CanvasHeight)
            {
                return;
            }

            Main.Checkpoint();
            ProjectObject.Pixels.CanvasHeight = Math.Clamp(value, 8, 1024);
            Main.MarkDirty();
            OnPropertyChanged();
        }
    }

    public int PaletteColorCount => ProjectObject.Palette.Colors.Count;

    public int PaletteMaxColors => ProjectObject.Palette.MaxColors;

    public int DirectionCount => ProjectObject.View.Directions.Count;

    private Project ProjectObject => Main.ProjectObject;
}

/// <summary>Rig workspace: part tree + editable part properties.</summary>
public class RigInspectorViewModel : InspectorViewModel
{
    private PartNode? _selectedPart;

    public RigInspectorViewModel(MainViewModel main)
        : base(main)
    {
    }

    public RigDefinition? Rig => ProjectObject.Rigs.FirstOrDefault(r => r.Id == Main.SelectedRigId)
        ?? ProjectObject.Rigs.FirstOrDefault();

    public string RigDisplayName => Rig?.DisplayName ?? "(chưa có rig)";

    public IReadOnlyList<PartNode> Parts => Rig?.Parts ?? [];

    public PartNode? SelectedPart
    {
        get => _selectedPart;
        set
        {
            _selectedPart = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PartDisplayName));
            OnPropertyChanged(nameof(PartZIndex));
            OnPropertyChanged(nameof(PartOffsetX));
            OnPropertyChanged(nameof(PartOffsetY));
            OnPropertyChanged(nameof(PartParentId));
        }
    }

    public string PartDisplayName
    {
        get => _selectedPart?.DisplayName ?? "";
        set
        {
            if (_selectedPart is null || _selectedPart.DisplayName == value)
            {
                return;
            }

            Main.Checkpoint();
            _selectedPart.DisplayName = value;
            Main.MarkDirty();
            OnPropertyChanged();
        }
    }

    public int PartZIndex
    {
        get => _selectedPart?.ZIndex ?? 0;
        set
        {
            if (_selectedPart is null || _selectedPart.ZIndex == value)
            {
                return;
            }

            Main.Checkpoint();
            _selectedPart.ZIndex = value;
            Main.MarkDirty();
            OnPropertyChanged();
        }
    }

    public int PartOffsetX
    {
        get => _selectedPart?.Transform.OffsetX ?? 0;
        set
        {
            if (_selectedPart is null || _selectedPart.Transform.OffsetX == value)
            {
                return;
            }

            Main.Checkpoint();
            _selectedPart.Transform.OffsetX = value;
            Main.MarkDirty();
            OnPropertyChanged();
            Main.RenderPreviewCommand.Execute(null);
        }
    }

    public int PartOffsetY
    {
        get => _selectedPart?.Transform.OffsetY ?? 0;
        set
        {
            if (_selectedPart is null || _selectedPart.Transform.OffsetY == value)
            {
                return;
            }

            Main.Checkpoint();
            _selectedPart.Transform.OffsetY = value;
            Main.MarkDirty();
            OnPropertyChanged();
            Main.RenderPreviewCommand.Execute(null);
        }
    }

    public string PartParentId => _selectedPart?.ParentId ?? "(root)";

    private Project ProjectObject => Main.ProjectObject;
}

/// <summary>Animation workspace: playback properties of the selected animation.</summary>
public class AnimationInspectorViewModel : InspectorViewModel
{
    public AnimationInspectorViewModel(MainViewModel main)
        : base(main)
    {
    }

    private AnimationDefinition? Animation => Main.ProjectObject.Animations
        .FirstOrDefault(a => a.Id == Main.SelectedAnimationId);

    public string AnimationDisplayName
    {
        get => Animation?.DisplayName ?? "";
        set
        {
            if (Animation is null || Animation.DisplayName == value)
            {
                return;
            }

            Main.Checkpoint();
            Animation.DisplayName = value;
            Main.MarkDirty();
            OnPropertyChanged();
        }
    }

    public int Fps
    {
        get => Animation?.Fps ?? 6;
        set
        {
            if (Animation is null || Animation.Fps == value)
            {
                return;
            }

            Main.Checkpoint();
            Animation.Fps = Math.Clamp(value, 1, 60);
            Main.MarkDirty();
            OnPropertyChanged();
            Main.NotifyAnimationInterval();
        }
    }

    public bool Loop
    {
        get => Animation?.Loop ?? true;
        set
        {
            if (Animation is null || Animation.Loop == value)
            {
                return;
            }

            Main.Checkpoint();
            Animation.Loop = value;
            Main.MarkDirty();
            OnPropertyChanged();
        }
    }

    public double DurationSeconds => Animation?.DurationSeconds ?? 0;

    public int FrameCount => Animation?.Frames.Count ?? 0;

    // Proxies to the timeline editor state on MainViewModel (the Animation
    // DataTemplate binds through this inspector).
    public IReadOnlyList<string> PosePartOptions => Main.PosePartOptions;

    public string? PoseEditorPart
    {
        get => Main.PoseEditorPart;
        set => Main.PoseEditorPart = value;
    }

    public int PoseEditOffsetX
    {
        get => Main.PoseEditOffsetX;
        set => Main.PoseEditOffsetX = value;
    }

    public int PoseEditOffsetY
    {
        get => Main.PoseEditOffsetY;
        set => Main.PoseEditOffsetY = value;
    }

    public bool PoseEditHidden
    {
        get => Main.PoseEditHidden;
        set => Main.PoseEditHidden = value;
    }

    public string PoseEditState
    {
        get => Main.PoseEditState;
        set => Main.PoseEditState = value;
    }

    public System.Windows.Input.ICommand LoadPoseEditCommand => Main.LoadPoseEditCommand;

    public System.Windows.Input.ICommand ApplyPoseEditCommand => Main.ApplyPoseEditCommand;

    public IReadOnlyList<string> MarkerTypeOptions => MainViewModel.MarkerTypeOptions;

    public string NewMarkerType
    {
        get => Main.NewMarkerType;
        set => Main.NewMarkerType = value;
    }

    public string? NewMarkerValue
    {
        get => Main.NewMarkerValue;
        set => Main.NewMarkerValue = value;
    }

    public System.Windows.Input.ICommand AddMarkerToCurrentFrameCommand => Main.AddMarkerToCurrentFrameCommand;

    public System.Windows.Input.ICommand RemoveLastMarkerFromCurrentFrameCommand => Main.RemoveLastMarkerFromCurrentFrameCommand;

    private Project ProjectObject => Main.ProjectObject;
}

/// <summary>Behavior workspace: bindings of the selected behavior.</summary>
public class BehaviorInspectorViewModel : InspectorViewModel
{
    public BehaviorInspectorViewModel(MainViewModel main)
        : base(main)
    {
    }

    private BehaviorDefinition? Behavior => Main.ProjectObject.Behaviors
        .FirstOrDefault(b => b.Id == Main.SelectedBehavior?.Id);

    public IReadOnlyList<string> AnimationOptions => Main.AnimationOptions.ToList();

    public IReadOnlyList<string> SlotOptions =>
        ["(none)", .. Main.ProjectObject.Rigs.SelectMany(r => r.EquipmentSlots).Select(sl => sl.Id).Distinct(StringComparer.Ordinal)];

    public IReadOnlyList<string> AnchorOptions =>
        ["(none)", .. Main.ProjectObject.Rigs.SelectMany(r => r.Anchors).Select(a => a.Id).Distinct(StringComparer.Ordinal)];

    public IReadOnlyList<string> GroupOptions { get; } =
    [
        BehaviorGroups.Movement, BehaviorGroups.Combat, BehaviorGroups.Survival,
        BehaviorGroups.Management, BehaviorGroups.Adventure, "Tùy chỉnh",
    ];

    public string Group
    {
        get => Behavior?.Group ?? "";
        set
        {
            if (Behavior is null || Behavior.Group == value)
            {
                return;
            }

            Main.Checkpoint();
            Behavior.Group = value;
            Main.MarkDirty();
            OnPropertyChanged();
            Main.RefreshBehaviorItems();
        }
    }

    public string DisplayName
    {
        get => Behavior?.DisplayName ?? "";
        set
        {
            if (Behavior is null || Behavior.DisplayName == value)
            {
                return;
            }

            Main.Checkpoint();
            Behavior.DisplayName = value;
            Main.MarkDirty();
            OnPropertyChanged();
        }
    }

    public string? AnimationId
    {
        get => Behavior?.AnimationId;
        set
        {
            if (Behavior is null || Behavior.AnimationId == value)
            {
                return;
            }

            Main.Checkpoint();
            Behavior.AnimationId = value is "(none)" ? null : value;
            Main.MarkDirty();
            OnPropertyChanged();
            Main.RefreshBehaviorItems();
        }
    }

    public string? HeldItemSlotId
    {
        get => Behavior?.HeldItemSlotId;
        set
        {
            if (Behavior is null || Behavior.HeldItemSlotId == value)
            {
                return;
            }

            Main.Checkpoint();
            Behavior.HeldItemSlotId = value is "(none)" ? null : value;
            Main.MarkDirty();
            OnPropertyChanged();
            Main.RefreshBehaviorItems();
        }
    }

    public string? InteractionAnchorId
    {
        get => Behavior?.InteractionAnchorId;
        set
        {
            if (Behavior is null || Behavior.InteractionAnchorId == value)
            {
                return;
            }

            Main.Checkpoint();
            Behavior.InteractionAnchorId = value is "(none)" ? null : value;
            Main.MarkDirty();
            OnPropertyChanged();
            Main.RefreshBehaviorItems();
        }
    }

    public string MarkersText
    {
        get
        {
            if (Behavior is null)
            {
                return "";
            }

            return string.Join("\n", Behavior.Markers.Select(m => $"{m.Type}: {m.Name} @ frame {m.FrameIndex}"));
        }
    }

    private Project ProjectObject => Main.ProjectObject;
}

/// <summary>Validation workspace: runs project + on-disk validation and lists issues.</summary>
public class ValidationInspectorViewModel : InspectorViewModel
{
    private IReadOnlyList<string> _issues = [];

    public ValidationInspectorViewModel(MainViewModel main)
        : base(main)
    {
    }

    public IReadOnlyList<string> Issues
    {
        get => _issues;
        private set
        {
            _issues = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Summary));
        }
    }

    public string Summary => Issues.Count == 0 ? "Không phát hiện vấn đề." : $"{Issues.Count} vấn đề";

    public void Run()
    {
        Issues = Main.RunValidation();
    }
}

/// <summary>Export workspace: options + run.</summary>
public class ExportInspectorViewModel : InspectorViewModel
{
    private string _outputSummary = "";

    public ExportInspectorViewModel(MainViewModel main)
        : base(main)
    {
    }

    public IReadOnlyList<string> AnimationOptions => Main.AnimationOptions.ToList();

    public string? SelectedAnimation { get; set; }

    public bool IncludeFrames { get; set; } = true;

    public bool IncludeSpritesheet { get; set; } = true;

    public string OutputSummary
    {
        get => _outputSummary;
        private set
        {
            _outputSummary = value;
            OnPropertyChanged();
        }
    }

    public void SetAnimationOptions(IReadOnlyList<string> animationIds)
    {
        // kept simple: options shown in the UI come from MainViewModel.AnimationOptions
    }

    public string Run(string outputDirectory)
    {
        CharacterEntity? character = Main.CurrentCharacter;
        if (character is null)
        {
            return "Cần chọn nhân vật để export.";
        }

        try
        {
            Export.CharacterExportOptions options = new()
            {
                AnimationId = string.IsNullOrWhiteSpace(SelectedAnimation) ? null : SelectedAnimation,
                IncludeFrames = IncludeFrames,
                IncludeSpritesheet = IncludeSpritesheet,
            };
            ExportResult result = Main.ExportCharacter(character, options, outputDirectory);
            OutputSummary = $"{result.FrameCount} frame, {result.SheetCount} spritesheet → {result.OutputDirectory}";
            return OutputSummary;
        }
        catch (Exception ex)
        {
            OutputSummary = $"Lỗi export: {ex.Message}";
            return OutputSummary;
        }
    }

    private Project ProjectObject => Main.ProjectObject;
}

/// <summary>Character workspace: identity of the selected character.</summary>
public class CharacterInspectorViewModel : ObservableObject
{
    public CharacterInspectorViewModel(MainViewModel main) => Main = main;

    private MainViewModel Main { get; }

    public IReadOnlyList<string> CharacterOptions => Main.CharacterOptions;

    /// <summary>Id + display name pairs for the character picker combo.</summary>
    public System.Collections.ObjectModel.ObservableCollection<CharacterOptionItem> CharacterDisplayItems => Main.CharacterDisplayItems;

    public string? SelectedCharacter
    {
        get => Main.SelectedCharacterId;
        set => Main.SelectedCharacterId = value;
    }

    public System.Windows.Input.ICommand AddCharacterCommand => Main.AddCharacterCommand;

    /// <summary>Re-reads every character-derived field (called when the selected character changes).</summary>
    public void RefreshFromCharacter()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Notes));
        OnPropertyChanged(nameof(BodyVariant));
        OnPropertyChanged(nameof(EyeStyle));
        OnPropertyChanged(nameof(MouthStyle));
        OnPropertyChanged(nameof(HairStyle));
        OnPropertyChanged(nameof(SkinTone));
        OnPropertyChanged(nameof(CharacterDisplayItems));
    }

    public string Name
    {
        get => Main.ProjectObject.Characters.FirstOrDefault(c => c.Id == Main.SelectedCharacterId)?.Name ?? "";
        set
        {
            CharacterEntity? character = Main.ProjectObject.Characters
                .FirstOrDefault(c => c.Id == Main.SelectedCharacterId);
            if (character is null || character.Name == value)
            {
                return;
            }

            Main.Checkpoint();
            character.Name = value;
            Main.ProjectName = value;
            Main.MarkDirty();
            OnPropertyChanged();
        }
    }

    public string Notes
    {
        get => Main.ProjectObject.Characters.FirstOrDefault(c => c.Id == Main.SelectedCharacterId)?.Notes ?? "";
        set
        {
            CharacterEntity? character = Main.ProjectObject.Characters
                .FirstOrDefault(c => c.Id == Main.SelectedCharacterId);
            if (character is null || character.Notes == value)
            {
                return;
            }

            Main.Checkpoint();
            character.Notes = value;
            Main.MarkDirty();
            OnPropertyChanged();
        }
    }

    // ---- Appearance variants (Phase 4) — options always offered from the shared
    // catalog; a choice is stored (as slug) even before variant assets exist and
    // takes effect once the part registers state assets. ----

    private PartAppearance? AppearanceOf(string partId) =>
        Main.CurrentCharacter?.AppearanceOf(partId);

    /// <summary>Catalog display names plus any custom variants registered on the part (custom ones show by slug).</summary>
    private IReadOnlyList<string> VariantOptions(string[] catalog, string partId, string stateKey)
    {
        var names = new List<string>(catalog);
        PartAppearance? appearance = AppearanceOf(partId);
        if (appearance?.StateAssets.TryGetValue(stateKey, out Dictionary<string, StateAssetVariant>? byValue) == true)
        {
            foreach (string slug in byValue.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                string display = AppearanceCatalog.DisplayFor(catalog, slug);
                if (!names.Contains(display, StringComparer.Ordinal))
                {
                    names.Add(display);
                }
            }
        }

        return names;
    }

    private string? GetStateValue(string partId, string stateKey)
    {
        PartAppearance? appearance = AppearanceOf(partId);
        return appearance?.States.TryGetValue(stateKey, out string? value) == true ? value : null;
    }

    private void SetStateValue(string partId, string stateKey, string value)
    {
        CharacterEntity? character = Main.CurrentCharacter;
        if (character is null)
        {
            return;
        }

        Main.EnsureAppearanceVariants();
        Main.Checkpoint();
        AppearanceService.SetState(Main.ProjectObject, character, partId, stateKey, AppearanceCatalog.Slug(value));
        Main.MarkDirty();
        OnPropertyChanged(nameof(BodyVariant));
        OnPropertyChanged(nameof(EyeStyle));
        OnPropertyChanged(nameof(MouthStyle));
        OnPropertyChanged(nameof(HairStyle));
        Main.RenderPreviewCommand.Execute(null);
    }

    public IReadOnlyList<string> BodyVariantOptions => VariantOptions(AppearanceCatalog.BodyVariants, "torso", "body");

    public string? BodyVariant
    {
        get => AppearanceCatalog.DisplayFor(AppearanceCatalog.BodyVariants, GetStateValue("torso", "body"));
        set
        {
            if (value is not null)
            {
                SetStateValue("torso", "body", value);
            }
        }
    }

    public IReadOnlyList<string> EyeStyleOptions => VariantOptions(AppearanceCatalog.EyeStyles, "face", "eye");

    public string? EyeStyle
    {
        get => AppearanceCatalog.DisplayFor(AppearanceCatalog.EyeStyles, GetStateValue("face", "eye"));
        set
        {
            if (value is not null)
            {
                SetStateValue("face", "eye", value);
            }
        }
    }

    public IReadOnlyList<string> MouthStyleOptions => VariantOptions(AppearanceCatalog.MouthStyles, "face", "mouth");

    public string? MouthStyle
    {
        get => AppearanceCatalog.DisplayFor(AppearanceCatalog.MouthStyles, GetStateValue("face", "mouth"));
        set
        {
            if (value is not null)
            {
                SetStateValue("face", "mouth", value);
            }
        }
    }

    public IReadOnlyList<string> HairStyleOptions => VariantOptions(AppearanceCatalog.HairStyles, "hair_front", "hair");

    public string? HairStyle
    {
        get => AppearanceCatalog.DisplayFor(AppearanceCatalog.HairStyles, GetStateValue("hair_front", "hair"));
        set
        {
            if (value is null || Main.CurrentCharacter is null)
            {
                return;
            }

            Main.EnsureAppearanceVariants();
            Main.Checkpoint();
            AppearanceService.SetStateOnParts(Main.ProjectObject, Main.CurrentCharacter,
                ["hair_front", "hair_back"], "hair", AppearanceCatalog.Slug(value));
            Main.MarkDirty();
            OnPropertyChanged(nameof(HairStyle));
            Main.RenderPreviewCommand.Execute(null);
        }
    }

    /// <summary>Skin tone variants remap the legacy skin palette on skin-bearing parts (Phase 4).</summary>
    public IReadOnlyList<string> SkinToneOptions => PixelGameStudio.Rendering.Procedural.LegacyCatalog.SkinTones.Keys.ToList();

    public string? SkinTone
    {
        get
        {
            CharacterEntity? character = Main.CurrentCharacter;
            return character is not null && character.Notes.StartsWith("skin:", StringComparison.Ordinal)
                ? character.Notes[5..]
                : "Sáng";
        }
        set
        {
            if (value is null || Main.CurrentCharacter is null)
            {
                return;
            }

            Main.Checkpoint();
            if (!PixelGameStudio.Rendering.Procedural.LegacyCatalog.SkinTones.TryGetValue(value, out (Rgba32 Base, Rgba32 Shadow) tone))
            {
                return;
            }

            (Rgba32 baseFrom, Rgba32 shadowFrom) = PixelGameStudio.Rendering.Procedural.LegacyCatalog.SkinTones["Sáng"];
            var mappings = value == "Sáng"
                ? []
                : new List<PaletteMapping>
                {
                    new(baseFrom.ToHex(), tone.Base.ToHex()),
                    new(shadowFrom.ToHex(), tone.Shadow.ToHex()),
                };
            AppearanceService.ApplySkinOverride(Main.ProjectObject, Main.CurrentCharacter, mappings);
            Main.CurrentCharacter.Notes = $"skin:{value}";
            Main.MarkDirty();
            OnPropertyChanged(nameof(SkinTone));
            Main.RenderPreviewCommand.Execute(null);
        }
    }

}

/// <summary>Equipment workspace: hosts the data-driven slot editor.</summary>
public class EquipmentInspectorViewModel : ObservableObject
{
    public EquipmentInspectorViewModel(MainViewModel main) => Main = main;

    private MainViewModel Main { get; }

    public System.Collections.ObjectModel.ObservableCollection<EquipmentSlotViewModel> Slots => Main.EquipmentSlots;

    public IReadOnlyList<string> CharacterOptions => Main.CharacterOptions;

    /// <summary>Id + display name pairs for the character picker combo.</summary>
    public System.Collections.ObjectModel.ObservableCollection<CharacterOptionItem> CharacterDisplayItems => Main.CharacterDisplayItems;

    public string? SelectedCharacter
    {
        get => Main.SelectedCharacterId;
        set => Main.SelectedCharacterId = value;
    }

    public System.Windows.Input.ICommand CreateStarterCommand => Main.CreateStarterContentCommand;
}
