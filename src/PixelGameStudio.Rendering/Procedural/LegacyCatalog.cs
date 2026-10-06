using PixelGameStudio.Core.Primitives;

namespace PixelGameStudio.Rendering.Procedural;

/// <summary>
/// Color catalog ported verbatim from the legacy prototype catalog.py /
/// effects/library.py. Kept as reference data for the legacy renderer and for
/// building the future data-driven StyleProfile defaults.
/// </summary>
public static class LegacyCatalog
{
    public const int CanvasWidth = 32;
    public const int CanvasHeight = 46;

    public static readonly Rgba32 OutlineColor = new(22, 16, 30, 255);
    public static readonly Rgba32 Inner = new(52, 54, 78, 255);
    public static readonly Rgba32 White = new(250, 250, 255, 255);
    public static readonly Rgba32 Gold = new(246, 200, 84, 255);
    public static readonly Rgba32 GoldDark = new(196, 140, 48, 255);
    public static readonly Rgba32 Brown = new(128, 82, 64, 255);
    public static readonly Rgba32 Blade = new(236, 246, 255, 255);
    public static readonly Rgba32 BladeDark = new(150, 190, 236, 255);
    public static readonly Rgba32 Eye = new(34, 58, 130, 255);
    public static readonly Rgba32 EyeLight = new(150, 200, 255, 255);
    public static readonly Rgba32 Blush = new(255, 150, 160, 255);
    public static readonly Rgba32 Mouth = new(214, 84, 88, 255);
    public static readonly Rgba32 ChestHighlight = new(255, 240, 225, 150);
    public static readonly Rgba32 Nose = new(210, 150, 140, 255);
    public static readonly Rgba32 MouthSerious = new(130, 70, 70, 255);
    public static readonly Rgba32 ScrollPaper = new(246, 220, 130, 255);
    public static readonly Rgba32 ScrollMark = new(190, 80, 60, 255);
    public static readonly Rgba32 Gourd = new(190, 140, 70, 255);
    public static readonly Rgba32 GourdDark = new(170, 110, 50, 255);
    public static readonly Rgba32 Jade = new(100, 230, 210, 255);
    public static readonly Rgba32 ChestLight = new(120, 130, 155, 255);
    public static readonly Rgba32 ChestHeavy = new(92, 100, 125, 255);
    public static readonly Rgba32 ChestPlate = new(170, 180, 205, 255);
    public static readonly Rgba32 ShoulderLight = new(150, 155, 180, 255);
    public static readonly Rgba32 ShoulderHeavy = new(105, 110, 140, 255);
    public static readonly Rgba32 GlovesCloth = new(96, 82, 70, 255);
    public static readonly Rgba32 GlovesPlate = new(120, 125, 150, 255);
    public static readonly Rgba32 BeltPlate = new(140, 145, 165, 255);
    public static readonly Rgba32 HelmetBase = new(120, 125, 150, 255);
    public static readonly Rgba32 HelmetTop = new(160, 165, 185, 255);
    public static readonly Rgba32 Shield = new(120, 125, 150, 255);
    public static readonly Rgba32 BootCloth = new(75, 66, 92, 255);
    public static readonly Rgba32 BootPlate = new(130, 135, 155, 255);
    public static readonly Rgba32 BootLeather = new(88, 62, 52, 255);
    public static readonly Rgba32 StaffOrb = new(120, 200, 255, 255);

    /// <summary>skin tone → (base, shadow).</summary>
    public static readonly Dictionary<string, (Rgba32 Base, Rgba32 Shadow)> SkinTones = new()
    {
        ["Sáng"] = (new Rgba32(255, 222, 200, 255), new Rgba32(232, 178, 158, 255)),
        ["Trung bình"] = (new Rgba32(226, 185, 153, 255), new Rgba32(197, 145, 116, 255)),
        ["Ngăm"] = (new Rgba32(190, 145, 110, 255), new Rgba32(154, 105, 78, 255)),
    };

    /// <summary>hair color → (base, dark, light).</summary>
    public static readonly Dictionary<string, (Rgba32 Base, Rgba32 Dark, Rgba32 Light)> HairPalettes = new()
    {
        ["Đen tím"] = (new Rgba32(44, 46, 78, 255), new Rgba32(28, 28, 54, 255), new Rgba32(98, 108, 158, 255)),
        ["Trắng bạc"] = (new Rgba32(222, 226, 240, 255), new Rgba32(160, 168, 200, 255), new Rgba32(255, 255, 255, 255)),
        ["Đỏ"] = (new Rgba32(190, 70, 60, 255), new Rgba32(130, 40, 40, 255), new Rgba32(240, 140, 110, 255)),
        ["Xanh lá"] = (new Rgba32(120, 200, 130, 255), new Rgba32(70, 150, 90, 255), new Rgba32(190, 240, 180, 255)),
        ["Nâu"] = (new Rgba32(112, 76, 58, 255), new Rgba32(72, 48, 40, 255), new Rgba32(168, 116, 86, 255)),
        ["Vàng"] = (new Rgba32(220, 188, 92, 255), new Rgba32(154, 120, 48, 255), new Rgba32(255, 230, 150, 255)),
    };

    /// <summary>skin variant / theme → (robe, robe dark, accent, accent dark).</summary>
    public static readonly Dictionary<string, (Rgba32 Robe, Rgba32 RobeDark, Rgba32 Accent, Rgba32 AccentDark)> Palettes = new()
    {
        ["Mặc định"] = (new Rgba32(250, 250, 255, 255), new Rgba32(196, 206, 232, 255), new Rgba32(86, 132, 214, 255), new Rgba32(56, 92, 170, 255)),
        ["Lạnh"] = (new Rgba32(116, 164, 230, 255), new Rgba32(72, 118, 196, 255), new Rgba32(46, 86, 160, 255), new Rgba32(32, 62, 122, 255)),
        ["Hỏa"] = (new Rgba32(230, 126, 126, 255), new Rgba32(186, 84, 84, 255), new Rgba32(214, 72, 84, 255), new Rgba32(150, 44, 50, 255)),
        ["U tối"] = (new Rgba32(96, 100, 118, 255), new Rgba32(64, 68, 84, 255), new Rgba32(52, 58, 80, 255), new Rgba32(34, 38, 58, 255)),
        ["Mộc"] = (new Rgba32(166, 210, 160, 255), new Rgba32(112, 166, 108, 255), new Rgba32(96, 180, 110, 255), new Rgba32(56, 130, 76, 255)),
    };

    /// <summary>element → effect color.</summary>
    public static readonly Dictionary<string, Rgba32> ElementColors = new()
    {
        ["Kim"] = new(246, 210, 100, 180),
        ["Mộc"] = new(100, 220, 140, 180),
        ["Thủy"] = new(110, 170, 255, 180),
        ["Hỏa"] = new(255, 100, 70, 180),
        ["Thổ"] = new(190, 150, 90, 180),
    };

    public static readonly Rgba32 DefaultEffectColor = new(255, 255, 255, 180);

    /// <summary>Legacy equipment catalog (slot → item list), ported from catalog.py — reference data for templates.</summary>
    public static readonly Dictionary<string, string[]> EquipmentLibrary = new()
    {
        ["head"] = ["Không", "Băng trán", "Mũ vải", "Mũ giáp"],
        ["inner"] = ["Không", "Áo trong sáng", "Áo trong tối"],
        ["outer"] = ["Không", "Kiếm tu", "Đan tu", "Phù tu", "Thể tu", "Du hiệp"],
        ["chest_armor"] = ["Không", "Giáp ngực nhẹ", "Giáp ngực nặng"],
        ["shoulder"] = ["Không", "Giáp vai nhẹ", "Giáp vai nặng"],
        ["gloves"] = ["Không", "Bao tay vải", "Bao tay giáp"],
        ["pants"] = ["Không", "Quần tối", "Quần sáng", "Quần chiến"],
        ["boots"] = ["Không", "Giày vải", "Giày da", "Ủng giáp"],
        ["belt"] = ["Không", "Đai vải", "Đai ngọc", "Đai giáp"],
        ["cape"] = ["Không", "Áo choàng ngắn", "Áo choàng dài"],
        ["main_hand"] = ["Không", "Kiếm", "Đao", "Thương", "Quạt", "Trượng"],
        ["off_hand"] = ["Không", "Khiên", "Phù", "Hồ lô"],
        ["accessory"] = ["Không", "Ngọc bội", "Khuyên tai", "Hồ lô"],
    };
}
