namespace PixelGameStudio.Domain.Profiles;

/// <summary>A single palette color. Hex is stored as "#RRGGBBAA" text so JSON stays human-readable.</summary>
public sealed class PaletteEntry
{
    public string Name { get; set; } = string.Empty;

    public string Hex { get; set; } = "#000000FF";
}

/// <summary>Shared color palette of the project with a soft max-color budget.</summary>
public sealed class PaletteProfile
{
    public string Name { get; set; } = "Mặc định";

    public int MaxColors { get; set; } = 32;

    public List<PaletteEntry> Colors { get; set; } = DefaultColors();

    public static List<PaletteEntry> DefaultColors() =>
    [
        new PaletteEntry { Name = "Outline", Hex = "#16101EFF" },
        new PaletteEntry { Name = "Da sáng", Hex = "#FFDEC8FF" },
        new PaletteEntry { Name = "Da tối", Hex = "#E8B29EFF" },
        new PaletteEntry { Name = "Tóc tối", Hex = "#1C1C36FF" },
        new PaletteEntry { Name = "Vải sáng", Hex = "#FAFAFFFF" },
        new PaletteEntry { Name = "Vải vừa", Hex = "#C4CEE8FF" },
        new PaletteEntry { Name = "Accent vàng", Hex = "#F6C854FF" },
        new PaletteEntry { Name = "Accent xanh", Hex = "#64C882FF" },
    ];
}
