using System.Globalization;
using System.Text;

namespace PixelGameStudio.Domain.Templates;

/// <summary>
/// Catalog of known appearance variants (display names in Vietnamese) shared by
/// the starter-content factory (which renders the variant art) and the character
/// inspector (which offers the choices). Stored state values use ASCII slugs so
/// they stay stable across asset ids and save files.
/// </summary>
public static class AppearanceCatalog
{
    public static readonly string[] BodyVariants = ["Tiêu chuẩn", "Mảnh", "Đậm"];

    public static readonly string[] EyeStyles = ["Bình tĩnh", "Vui", "Lạnh lùng", "Nghiêm túc", "Buồn ngủ"];

    public static readonly string[] MouthStyles = ["Trung tính", "Cười", "Nghiêm"];

    public static readonly string[] HairStyles =
    [
        "Không tóc", "Búi cao", "Tóc dài", "Tóc ngắn", "Đuôi ngựa", "Tóc xõa",
        "Tóc dựng", "Tóc lệch", "Hai búi", "Tóc mái dài", "Tóc võ sĩ",
        "Húi cua", "Undercut", "Tóc nam ngắn", "Rẽ ngôi nam", "Xoăn nam",
    ];

    /// <summary>ASCII slug: strips Vietnamese diacritics, lowercases, non-alphanumerics become '-'.</summary>
    public static string Slug(string input)
    {
        // Horn vowels and stroke-d don't decompose under FormD — map them first.
        var premapped = new StringBuilder(input.Length);
        foreach (char c in input)
        {
            premapped.Append(c switch
            {
                'đ' => 'd',
                'Đ' => 'd',
                'ơ' => 'o',
                'Ơ' => 'o',
                'ư' => 'u',
                'Ư' => 'u',
                _ => c,
            });
        }

        string normalized = premapped.ToString().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (char c in normalized)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            char ch = char.ToLowerInvariant(c);
            sb.Append(char.IsAsciiLetterOrDigit(ch) ? ch : '-');
        }

        string slug = sb.ToString();
        while (slug.Contains("--"))
        {
            slug = slug.Replace("--", "-");
        }

        return slug.Trim('-');
    }

    /// <summary>Maps a stored slug back to its display name; unknown slugs pass through.</summary>
    public static string? DisplayFor(string[] options, string? slug)
    {
        if (slug is null)
        {
            return null;
        }

        return options.FirstOrDefault(option => Slug(option).Equals(slug, StringComparison.Ordinal)) ?? slug;
    }
}
