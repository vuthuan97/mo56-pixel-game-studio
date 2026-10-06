using PixelGameStudio.Assets;
using PixelGameStudio.Assets.Composition;
using PixelGameStudio.Behaviors;
using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Character;
using PixelGameStudio.Domain.Role;
using PixelGameStudio.Domain.Templates;
using PixelGameStudio.ProjectSystem;
using PixelGameStudio.Rendering;
using Xunit;

namespace PixelGameStudio.ProjectSystem.Tests;

/// <summary>Phase 9: role system — data-driven roles, cultivation demo templates, apply semantics.</summary>
public class RoleSystemTests : IDisposable
{
    private readonly string _root;
    private readonly ProjectStore _store = new();
    private readonly AssetLibraryService _library = new();
    private readonly RoleService _roleService = new();

    public RoleSystemTests()
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

    private (Project Project, CharacterEntity Hero) Create()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        _store.Save(project, _root);
        StarterContentFactory.CreateHumanoidDemo(project, _root, _library);
        new BehaviorLibraryService().InstallPresets(project);
        CharacterEntity hero = project.Characters.Single(c => c.Id == "char.hero");
        return (project, hero);
    }

    [Fact]
    public void InstallCultivationTemplates_AddsFiveDemoRolesOnce()
    {
        (Project project, _) = Create();
        heroEquipmentCountStub(project);

        int first = _roleService.InstallTemplates(project, CultivationRoleTemplates.All);
        int second = _roleService.InstallTemplates(project, CultivationRoleTemplates.All);

        Assert.Equal(5, first);
        Assert.Equal(0, second);
        Assert.All(project.Roles, role => Assert.Contains("demo", role.Tags));
        Assert.Empty(project.Validate());
    }

    private static void heroEquipmentCountStub(Project project) => _ = project;

    [Fact]
    public void ApplyRole_SwapsEquipmentWithPerViewAssets_AndSetsRoleId()
    {
        (Project project, CharacterEntity hero) = Create();
        _roleService.InstallTemplates(project, CultivationRoleTemplates.All);

        RoleApplyResult result = _roleService.ApplyToCharacter(project, hero, "role.kiem-tu");

        Assert.Empty(result.Warnings);
        Assert.Equal(3, result.EquippedCount);
        Assert.Equal("role.kiem-tu", hero.RoleId);

        // main_hand: per-view assets resolved from the family
        EquippedItem weapon = hero.Equipment.Single(e => e.SlotId == "main_hand");
        Assert.Equal("eq.main_hand.kiem.down", weapon.AssetId);
        Assert.Equal("eq.main_hand.kiem.left", weapon.ViewAssets["Left"]);
        Assert.Equal("eq.main_hand.kiem.up", weapon.ViewAssets["Up"]);

        // outer swapped to Kiếm tu family
        EquippedItem outer = hero.Equipment.Single(e => e.SlotId == "outer");
        Assert.Equal("eq.outer.kiem-tu.down", outer.AssetId);

        // starting-equipment semantics: the loadout is exactly the role's set
        Assert.Equal(["belt", "main_hand", "outer"], hero.Equipment.Select(e => e.SlotId).OrderBy(x => x).ToArray());

        Assert.Empty(project.Validate());
    }

    [Fact]
    public void ApplyRole_RendersComposedSprite_DifferentPerRole()
    {
        (Project project, CharacterEntity hero) = Create();
        _roleService.InstallTemplates(project, CultivationRoleTemplates.All);
        var composer = new RigSpriteComposer(_library);

        _roleService.ApplyToCharacter(project, hero, "role.kiem-tu");
        PixelBuffer swordMaster = composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });

        _roleService.ApplyToCharacter(project, hero, "role.dan-tu");
        PixelBuffer alchemist = composer.Compose(project, _root, hero, new CompositionOptions { View = "Down" });

        Assert.NotEqual(swordMaster[4, 20], alchemist[4, 20]); // weapon/cape region differs
        Assert.Equal("role.dan-tu", hero.RoleId);
    }

    [Fact]
    public void ApplyRole_MissingAssetFamily_SkipsSlotWithWarning()
    {
        (Project project, CharacterEntity hero) = Create();
        project.Roles.Add(new RoleDefinition
        {
            Id = "role.broken",
            DisplayName = "Role hỏng",
            StartingEquipment = [new RoleEquipmentBinding("main_hand", "eq.main_hand.khong-ton-tai")],
        });

        RoleApplyResult result = _roleService.ApplyToCharacter(project, hero, "role.broken");

        Assert.Equal(0, result.EquippedCount);
        Assert.Single(result.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains("không có asset nào"));
    }

    [Fact]
    public void CreateCustom_Clones_AndValidationCatchesBrokenRefs()
    {
        (Project project, _) = Create();
        _roleService.InstallTemplates(project, CultivationRoleTemplates.All);

        RoleDefinition custom = _roleService.CreateCustom(project, "role.kiem-tu", "role.kiem-tu-do", "Kiếm tu đỏ");
        Assert.Equal("Custom", custom.Group);
        Assert.Equal(3, custom.StartingEquipment.Count);

        custom.StartingEquipment[1] = custom.StartingEquipment[1] with { AssetFamily = "eq.outer.hong" };
        custom.BehaviorIds.Add("beh.missing");

        IReadOnlyList<string> issues = project.Validate();
        Assert.Contains(issues, i => i.Contains("asset family không tồn tại 'eq.outer.hong'"));
        Assert.Contains(issues, i => i.Contains("behavior không tồn tại 'beh.missing'"));
    }

    [Fact]
    public void ProjectRoundTrip_KeepsRoles()
    {
        (Project project, _) = Create();
        _roleService.InstallTemplates(project, CultivationRoleTemplates.All);
        _store.Save(project, _root);

        Project loaded = _store.Open(_root);

        Assert.Equal(5, loaded.Roles.Count);
        RoleDefinition swordMaster = loaded.Roles.Single(r => r.Id == "role.kiem-tu");
        Assert.Equal("attack", swordMaster.DefaultAnimationId);
        Assert.Equal("eq.main_hand.kiem",
            swordMaster.StartingEquipment.Single(b => b.SlotId == "main_hand").AssetFamily);
        Assert.Empty(loaded.Validate());
    }
}
