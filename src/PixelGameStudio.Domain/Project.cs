using PixelGameStudio.Domain.Animation;
using PixelGameStudio.Domain.Assets;
using PixelGameStudio.Domain.Behaviors;
using PixelGameStudio.Domain.Character;
using PixelGameStudio.Domain.Profiles;
using PixelGameStudio.Domain.Role;

namespace PixelGameStudio.Domain;

/// <summary>
/// Aggregate root of one game's asset production environment. Owns the shared
/// profiles every asset must follow; asset PNG binaries live under the project
/// assets/ folder — this JSON only carries their definitions, never binaries.
/// </summary>
public sealed class Project
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public string ProjectId { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "New Project";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime ModifiedAtUtc { get; set; } = DateTime.UtcNow;

    public GameProfile Game { get; set; } = new();

    public ViewProfile View { get; set; } = ViewProfile.TopDown4();

    public PixelProfile Pixels { get; set; } = new();

    public PaletteProfile Palette { get; set; } = new();

    public ProjectStyleProfile Style { get; set; } = new();

    public List<AssetDefinition> Assets { get; set; } = [];

    public List<RigDefinition> Rigs { get; set; } = [];

    public List<CharacterEntity> Characters { get; set; } = [];

    public List<PoseDefinition> Poses { get; set; } = [];

    public List<AnimationDefinition> Animations { get; set; } = [];

    public List<BehaviorDefinition> Behaviors { get; set; } = [];

    public List<RoleDefinition> Roles { get; set; } = [];

    public IReadOnlyList<string> Validate() => ProfileRules.Validate(this);
}
