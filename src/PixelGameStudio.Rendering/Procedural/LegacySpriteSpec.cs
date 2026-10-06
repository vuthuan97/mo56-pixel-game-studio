namespace PixelGameStudio.Rendering.Procedural;

/// <summary>
/// Character appearance input for the legacy reference renderer, ported
/// verbatim from the prototype CharacterSpec (fixed class/slot hard-codes are
/// intentionally preserved here so golden-master tests can pin behavior; the
/// data-driven equivalent lives in the Domain layer from Phase 3 onwards).
/// </summary>
public sealed class LegacySpriteSpec
{
    public string Name { get; set; } = "Lăng Hàn";
    public string Gender { get; set; } = "Nam";
    public string ClassType { get; set; } = "Kiếm tu";
    public string BodyType { get; set; } = "Tiêu chuẩn";
    public string SkinTone { get; set; } = "Sáng";
    public string HeadShape { get; set; } = "Tròn";
    public string EyeStyle { get; set; } = "Bình tĩnh";
    public string NoseStyle { get; set; } = "Chấm";
    public string MouthStyle { get; set; } = "Trung tính";
    public string HairStyle { get; set; } = "Búi cao";
    public string HairColor { get; set; } = "Đen tím";
    public string Direction { get; set; } = "Down";
    public string SkinVariant { get; set; } = "Mặc định";
    public string Aura { get; set; } = "Không";
    public string Effect { get; set; } = "Không";
    public string Element { get; set; } = "Kim";

    public Dictionary<string, string> Equipment { get; set; } = DefaultEquipment();

    public static Dictionary<string, string> DefaultEquipment() => new()
    {
        ["head"] = "Không",
        ["inner"] = "Không",
        ["outer"] = "Kiếm tu",
        ["chest_armor"] = "Không",
        ["shoulder"] = "Không",
        ["gloves"] = "Không",
        ["pants"] = "Quần tối",
        ["boots"] = "Giày da",
        ["belt"] = "Đai ngọc",
        ["cape"] = "Không",
        ["main_hand"] = "Kiếm",
        ["off_hand"] = "Không",
        ["accessory"] = "Ngọc bội",
    };
}

public enum LegacyRenderMode
{
    Normal,
    Grayscale,
    Silhouette,
    PartDebug,
    AnchorDebug,
}

/// <summary>Named anchors per direction, ported from the legacy AnchorStore defaults.</summary>
public sealed class LegacyAnchorStore
{
    public static readonly Dictionary<string, Dictionary<string, (int X, int Y)>> Defaults = new()
    {
        ["Down"] = new()
        {
            ["head"] = (16, 12),
            ["chest"] = (16, 26),
            ["left_hand"] = (9, 31),
            ["right_hand"] = (23, 31),
            ["left_foot"] = (12, 42),
            ["right_foot"] = (20, 42),
            ["weapon"] = (4, 29),
            ["accessory"] = (20, 31),
        },
        ["Up"] = new()
        {
            ["head"] = (16, 12),
            ["chest"] = (16, 26),
            ["left_hand"] = (10, 30),
            ["right_hand"] = (22, 30),
            ["left_foot"] = (12, 42),
            ["right_foot"] = (20, 42),
            ["weapon"] = (4, 28),
            ["accessory"] = (19, 31),
        },
        ["Left"] = new()
        {
            ["head"] = (15, 12),
            ["chest"] = (15, 26),
            ["left_hand"] = (8, 30),
            ["right_hand"] = (21, 30),
            ["left_foot"] = (11, 42),
            ["right_foot"] = (19, 42),
            ["weapon"] = (4, 29),
            ["accessory"] = (19, 31),
        },
        ["Right"] = new()
        {
            ["head"] = (17, 12),
            ["chest"] = (17, 26),
            ["left_hand"] = (11, 30),
            ["right_hand"] = (24, 30),
            ["left_foot"] = (13, 42),
            ["right_foot"] = (21, 42),
            ["weapon"] = (5, 29),
            ["accessory"] = (21, 31),
        },
    };

    private Dictionary<string, Dictionary<string, (int X, int Y)>> _data = CloneDefaults();

    public (int X, int Y) Get(string direction, string name) => _data[direction][name];

    public void Set(string direction, string name, int x, int y)
    {
        if (!_data.TryGetValue(direction, out var dir))
        {
            dir = [];
            _data[direction] = dir;
        }

        dir[name] = (x, y);
    }

    public void Reset() => _data = CloneDefaults();

    private static Dictionary<string, Dictionary<string, (int X, int Y)>> CloneDefaults() =>
        Defaults.ToDictionary(kv => kv.Key, kv => new Dictionary<string, (int, int)>(kv.Value));
}
