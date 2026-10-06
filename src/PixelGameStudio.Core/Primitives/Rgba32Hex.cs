namespace PixelGameStudio.Core.Primitives;

public readonly partial record struct Rgba32
{
    /// <summary>Parses "#RGB", "#RRGGBB" or "#RRGGBBAA" (leading '#' optional).</summary>
    public static bool TryParseHex(string? text, out Rgba32 value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string hex = text.Trim().TrimStart('#');
        if (hex.Length is not (3 or 6 or 8) || !hex.All(char.IsAsciiHexDigit))
        {
            return false;
        }

        if (hex.Length == 3)
        {
            hex = string.Concat(hex.Select(c => $"{c}{c}"));
            hex += "FF";
        }
        else if (hex.Length == 6)
        {
            hex += "FF";
        }

        value = new Rgba32(
            Convert.ToByte(hex.Substring(0, 2), 16),
            Convert.ToByte(hex.Substring(2, 2), 16),
            Convert.ToByte(hex.Substring(4, 2), 16),
            Convert.ToByte(hex.Substring(6, 2), 16));
        return true;
    }

    /// <summary>Returns "#RRGGBBAA".</summary>
    public string ToHex() => $"#{R:X2}{G:X2}{B:X2}{A:X2}";
}
