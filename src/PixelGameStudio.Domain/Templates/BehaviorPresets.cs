using PixelGameStudio.Domain.Behaviors;

namespace PixelGameStudio.Domain.Templates;

/// <summary>
/// Preset behaviors from docs/PRODUCT_OVERVIEW.md (5 groups). Pure data —
/// the UI lists them by group and never hard-codes them. Behaviors without a
/// matching preset animation leave AnimationId null for the user to bind.
/// </summary>
public static class BehaviorPresets
{
    public static IReadOnlyList<BehaviorDefinition> All => Build().Select(b => b.Clone()).ToList();

    private static readonly HashSet<string> PresetIds =
        new(Build().Select(b => b.Id), StringComparer.Ordinal);

    public static bool IsPresetId(string id) => PresetIds.Contains(id);

    private static BehaviorDefinition B(string id, string name, string group, string? animation = null,
        string? heldSlot = null, string? interactAnchor = null,
        (string Type, string Name, int Frame)[]? markers = null, string notes = "")
    {
        var behavior = new BehaviorDefinition
        {
            Id = id,
            DisplayName = name,
            Group = group,
            AnimationId = animation,
            HeldItemSlotId = heldSlot,
            InteractionAnchorId = interactAnchor,
            Notes = notes,
        };
        if (markers is not null)
        {
            behavior.Markers.AddRange(markers.Select(m => new BehaviorMarker(m.Type, m.Name, m.Frame)));
        }

        return behavior;
    }

    private static IReadOnlyList<BehaviorDefinition> Build()
    {
        var list = new List<BehaviorDefinition>
        {
            // Di chuyển
            B("beh.stand", "Đứng", BehaviorGroups.Movement, "idle"),
            B("beh.walk", "Đi", BehaviorGroups.Movement, "walk"),
            B("beh.run", "Chạy", BehaviorGroups.Movement, "walk", notes: "Có thể tăng FPS khi dựng animation riêng."),
            B("beh.jump", "Nhảy", BehaviorGroups.Movement),
            B("beh.fall", "Rơi", BehaviorGroups.Movement),
            B("beh.land", "Tiếp đất", BehaviorGroups.Movement),
            B("beh.crawl", "Bò", BehaviorGroups.Movement),
            B("beh.climb", "Leo", BehaviorGroups.Movement),
            B("beh.dodge", "Né", BehaviorGroups.Movement),
            B("beh.roll", "Lăn", BehaviorGroups.Movement),
            B("beh.slide", "Trượt", BehaviorGroups.Movement),

            // Chiến đấu
            B("beh.guard", "Thủ thế", BehaviorGroups.Combat, "attack", heldSlot: "main_hand"),
            B("beh.attack_light", "Đánh nhẹ", BehaviorGroups.Combat, "attack", heldSlot: "main_hand",
                markers: [("Gameplay", "HitTarget", 2)]),
            B("beh.attack_heavy", "Đánh mạnh", BehaviorGroups.Combat, "attack", heldSlot: "main_hand",
                markers: [("Gameplay", "HitTarget", 2), ("Sound", "heavy_swing", 2)]),
            B("beh.combo", "Combo", BehaviorGroups.Combat, "attack", heldSlot: "main_hand"),
            B("beh.block", "Đỡ", BehaviorGroups.Combat, heldSlot: "off_hand"),
            B("beh.counter", "Phản đòn", BehaviorGroups.Combat, "attack"),
            B("beh.shoot", "Bắn", BehaviorGroups.Combat, "cast",
                markers: [("Gameplay", "SpawnProjectile", 1)]),
            B("beh.throw", "Ném", BehaviorGroups.Combat,
                markers: [("Gameplay", "SpawnProjectile", 1)]),
            B("beh.skill", "Dùng kỹ năng", BehaviorGroups.Combat, "cast",
                markers: [("Effect", "skill_glow", 1), ("Sound", "skill_cast", 1)]),
            B("beh.hurt", "Bị đánh", BehaviorGroups.Combat, "hurt",
                markers: [("Sound", "hurt_voice", 0)]),
            B("beh.stun", "Choáng", BehaviorGroups.Combat, "hurt"),
            B("beh.die", "Chết", BehaviorGroups.Combat, "death"),
            B("beh.victory", "Chiến thắng", BehaviorGroups.Combat, "cast"),

            // Sinh tồn
            B("beh.chop_tree", "Chặt cây", BehaviorGroups.Survival, "attack", heldSlot: "main_hand",
                interactAnchor: "anchor.interact",
                markers: [("Gameplay", "HitResource", 2), ("Sound", "chop_wood", 2), ("Effect", "wood_chips", 2)]),
            B("beh.mine_rock", "Đào đá", BehaviorGroups.Survival, "attack", heldSlot: "main_hand",
                interactAnchor: "anchor.interact",
                markers: [("Gameplay", "HitResource", 2), ("Sound", "mine_hit", 2)]),
            B("beh.dig", "Đào đất", BehaviorGroups.Survival, "attack", heldSlot: "main_hand",
                interactAnchor: "anchor.interact", markers: [("Gameplay", "HitResource", 2)]),
            B("beh.fish", "Câu cá", BehaviorGroups.Survival, heldSlot: "main_hand", interactAnchor: "anchor.interact"),
            B("beh.harvest", "Hái", BehaviorGroups.Survival, interactAnchor: "anchor.interact"),
            B("beh.collect", "Thu thập", BehaviorGroups.Survival, interactAnchor: "anchor.interact"),
            B("beh.craft", "Chế tạo", BehaviorGroups.Survival, "cast"),
            B("beh.build", "Xây dựng", BehaviorGroups.Survival, "attack", heldSlot: "main_hand"),
            B("beh.repair", "Sửa chữa", BehaviorGroups.Survival, "attack", heldSlot: "main_hand"),
            B("beh.eat", "Ăn", BehaviorGroups.Survival),
            B("beh.drink", "Uống", BehaviorGroups.Survival),
            B("beh.carry", "Mang đồ", BehaviorGroups.Survival),
            B("beh.push", "Đẩy", BehaviorGroups.Survival),
            B("beh.pull", "Kéo", BehaviorGroups.Survival),

            // Quản lý / Tycoon
            B("beh.work", "Làm việc", BehaviorGroups.Management, "idle"),
            B("beh.mine", "Đào mỏ", BehaviorGroups.Management, "attack", heldSlot: "main_hand",
                interactAnchor: "anchor.interact", markers: [("Gameplay", "HitResource", 2)]),
            B("beh.farm", "Làm ruộng", BehaviorGroups.Management, "attack", interactAnchor: "anchor.interact"),
            B("beh.smith", "Rèn", BehaviorGroups.Management, "attack", heldSlot: "main_hand",
                markers: [("Sound", "anvil_hit", 2)]),
            B("beh.cook", "Nấu", BehaviorGroups.Management, "cast"),
            B("beh.serve", "Phục vụ", BehaviorGroups.Management, "walk"),
            B("beh.trade", "Mua bán", BehaviorGroups.Management, "idle"),
            B("beh.transport", "Vận chuyển", BehaviorGroups.Management, "walk"),
            B("beh.sleep", "Ngủ", BehaviorGroups.Management),
            B("beh.rest", "Nghỉ", BehaviorGroups.Management, "idle"),
            B("beh.talk", "Nói chuyện", BehaviorGroups.Management, "idle"),
            B("beh.queue", "Xếp hàng", BehaviorGroups.Management, "idle"),

            // Phiêu lưu / Trải nghiệm
            B("beh.sit", "Ngồi", BehaviorGroups.Adventure),
            B("beh.lie", "Nằm", BehaviorGroups.Adventure),
            B("beh.wave", "Vẫy tay", BehaviorGroups.Adventure, "cast"),
            B("beh.point", "Chỉ tay", BehaviorGroups.Adventure),
            B("beh.think", "Suy nghĩ", BehaviorGroups.Adventure, "idle"),
            B("beh.laugh", "Cười", BehaviorGroups.Adventure, "idle"),
            B("beh.cry", "Khóc", BehaviorGroups.Adventure, "hurt"),
            B("beh.angry", "Tức giận", BehaviorGroups.Adventure, "idle"),
            B("beh.surprised", "Bất ngờ", BehaviorGroups.Adventure, "hurt"),
            B("beh.read", "Đọc", BehaviorGroups.Adventure, "idle"),
            B("beh.write", "Viết", BehaviorGroups.Adventure, "idle"),
            B("beh.open_door", "Mở cửa", BehaviorGroups.Adventure, interactAnchor: "anchor.interact"),
            B("beh.pick_up", "Nhặt đồ", BehaviorGroups.Adventure, interactAnchor: "anchor.interact",
                markers: [("Gameplay", "PickUp", 1)]),
            B("beh.give", "Đưa đồ", BehaviorGroups.Adventure, interactAnchor: "anchor.interact"),
            B("beh.receive", "Nhận đồ", BehaviorGroups.Adventure, interactAnchor: "anchor.interact"),
            B("beh.interact_object", "Tương tác vật thể", BehaviorGroups.Adventure, interactAnchor: "anchor.interact"),
        };
        return list;
    }
}
