using PixelGameStudio.Domain.Character;

using PixelGameStudio.Domain.Assets;

namespace PixelGameStudio.Domain.Templates;

/// <summary>
/// Humanoid rig template mirroring the legacy 17-layer structure as DATA:
/// parts carry explicit z-order (smaller = further back) and named anchors.
/// The rig is a template — projects may edit, extend or replace it freely.
/// Asset ids are filled in by the project (e.g. StarterContentFactory).
/// </summary>
public static class RigTemplates
{
    public const string HumanoidTopDownId = "rig.humanoid.topdown4";

    public static RigDefinition HumanoidTopDown4() => new()
    {
        Id = HumanoidTopDownId,
        DisplayName = "Humanoid — top-down 4 hướng",
        Parts =
        [
            new PartNode
            {
                Id = "torso", DisplayName = "Thân", ParentId = null, ZIndex = 5,
                States = { ["body"] = "tieu-chuan" }, Tags = ["body"],
            },
            new PartNode
            {
                Id = "head", DisplayName = "Đầu", ParentId = "torso", AnchorId = "anchor.head", ZIndex = 20,
                Pivot = new AssetAnchor(16, 21), Tags = ["body", "head"],
            },
            new PartNode
            {
                Id = "face", DisplayName = "Mặt", ParentId = "head", AnchorId = "anchor.face", ZIndex = 21,
                States = { ["eye"] = "binh-tinh", ["mouth"] = "trung-tinh" }, Tags = ["face"],
            },
            new PartNode
            {
                Id = "hair_front", DisplayName = "Tóc trước", ParentId = "head", AnchorId = "anchor.hair_front", ZIndex = 22,
                States = { ["hair"] = "bui-cao" }, Tags = ["hair"],
            },
            new PartNode
            {
                Id = "hair_back", DisplayName = "Tóc sau", ParentId = "head", AnchorId = "anchor.hair_back", ZIndex = 1,
                States = { ["hair"] = "bui-cao" }, Tags = ["hair"],
            },
            new PartNode
            {
                Id = "arm_left", DisplayName = "Tay trái", ParentId = "torso", AnchorId = "anchor.arm_left", ZIndex = 18,
                Pivot = new AssetAnchor(10, 24), States = { ["hand"] = "down" }, Tags = ["arm"],
            },
            new PartNode
            {
                Id = "arm_right", DisplayName = "Tay phải", ParentId = "torso", AnchorId = "anchor.arm_right", ZIndex = 19,
                Pivot = new AssetAnchor(21, 24), States = { ["hand"] = "down" }, Tags = ["arm"],
            },
            new PartNode
            {
                Id = "leg_left", DisplayName = "Chân trái", ParentId = "torso", AnchorId = "anchor.leg_left", ZIndex = 3,
                States = { ["leg"] = "neutral" }, Tags = ["leg"],
            },
            new PartNode
            {
                Id = "leg_right", DisplayName = "Chân phải", ParentId = "torso", AnchorId = "anchor.leg_right", ZIndex = 4,
                States = { ["leg"] = "neutral" }, Tags = ["leg"],
            },
            new PartNode
            {
                Id = "weapon", DisplayName = "Vũ khí", ParentId = "torso", AnchorId = "anchor.right_hand", ZIndex = 2,
                States = { ["weapon"] = "idle" }, Tags = ["held"],
            },
        ],
        Anchors =
        [
            new AnchorPoint("anchor.head", "torso", 0, 0),
            new AnchorPoint("anchor.face", "head", 0, 0),
            new AnchorPoint("anchor.hair_front", "head", 0, 0),
            new AnchorPoint("anchor.hair_back", "head", 0, 0),
            new AnchorPoint("anchor.arm_left", "torso", 0, 0),
            new AnchorPoint("anchor.arm_right", "torso", 0, 0),
            new AnchorPoint("anchor.leg_left", "torso", 0, 0),
            new AnchorPoint("anchor.leg_right", "torso", 0, 0),
            new AnchorPoint("anchor.chest", "torso", 0, 0),
            new AnchorPoint("anchor.left_hand", "arm_left", 0, 0),
            new AnchorPoint("anchor.right_hand", "arm_right", 0, 0),
            new AnchorPoint("anchor.left_foot", "leg_left", 0, 0),
            new AnchorPoint("anchor.right_foot", "leg_right", 0, 0),
            new AnchorPoint("anchor.weapon", "weapon", 0, 0),
            new AnchorPoint("anchor.accessory", "torso", 0, 0),
            new AnchorPoint("anchor.interact", "torso", 0, 0),
        ],
        EquipmentSlots =
        [
            new EquipmentSlotDef { Id = "main_hand", DisplayName = "Vũ khí chính", AnchorId = "anchor.weapon", ZIndex = 2 },
            new EquipmentSlotDef { Id = "off_hand", DisplayName = "Tay phụ", AnchorId = "anchor.left_hand", ZIndex = 16 },
            new EquipmentSlotDef { Id = "head", DisplayName = "Đầu", AnchorId = "anchor.head", ZIndex = 15 },
            new EquipmentSlotDef { Id = "inner", DisplayName = "Áo trong", AnchorId = "anchor.chest", ZIndex = 7 },
            new EquipmentSlotDef { Id = "outer", DisplayName = "Áo ngoài", AnchorId = "anchor.chest", ZIndex = 10 },
            new EquipmentSlotDef { Id = "chest_armor", DisplayName = "Giáp ngực", AnchorId = "anchor.chest", ZIndex = 11 },
            new EquipmentSlotDef { Id = "shoulder", DisplayName = "Giáp vai", AnchorId = "anchor.chest", ZIndex = 12 },
            new EquipmentSlotDef { Id = "gloves", DisplayName = "Bao tay", AnchorId = "anchor.arm_right", ZIndex = 13 },
            new EquipmentSlotDef { Id = "pants", DisplayName = "Quần", AnchorId = "anchor.leg_left", ZIndex = 8 },
            new EquipmentSlotDef { Id = "boots", DisplayName = "Giày", AnchorId = "anchor.leg_left", ZIndex = 9 },
            new EquipmentSlotDef { Id = "belt", DisplayName = "Đai", AnchorId = "anchor.chest", ZIndex = 14 },
            new EquipmentSlotDef { Id = "cape", DisplayName = "Áo choàng", AnchorId = "anchor.chest", ZIndex = 6 },
            new EquipmentSlotDef { Id = "accessory", DisplayName = "Phụ kiện", AnchorId = "anchor.accessory", ZIndex = 17 },
        ],
    };
}
