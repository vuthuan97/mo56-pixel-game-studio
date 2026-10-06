using PixelGameStudio.Domain.Assets;

namespace PixelGameStudio.Domain.Character;

/// <summary>
/// Appearance mutation helpers (Phase 4): state-variant selection and skin
/// palette overrides. Pure domain operations — the UI builds the inputs.
/// </summary>
public static class AppearanceService
{
    /// <summary>Parts whose pixels carry the legacy skin palette.</summary>
    public static readonly string[] SkinPartIds =
        ["torso", "head", "face", "arm_left", "arm_right", "leg_left", "leg_right"];

    /// <summary>
    /// Returns the part's appearance entry, creating an empty one when missing —
    /// empty-base characters (and older projects) can dress parts later.
    /// </summary>
    public static PartAppearance EnsureAppearance(CharacterEntity character, string partId)
    {
        ArgumentNullException.ThrowIfNull(character);
        PartAppearance? appearance = character.AppearanceOf(partId);
        if (appearance is null)
        {
            appearance = new PartAppearance { PartId = partId };
            character.Appearance.Add(appearance);
        }

        return appearance;
    }

    /// <summary>
    /// Records a state value on one part. The choice is stored even when the
    /// part has no variant assets yet — it takes effect as soon as variant
    /// assets are registered (composer falls back to the part's base asset).
    /// </summary>
    public static bool SetState(Project project, CharacterEntity character, string partId, string stateKey, string value)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(character);
        PartAppearance appearance = EnsureAppearance(character, partId);
        appearance.States[stateKey] = value;
        return true;
    }

    /// <summary>Sets one state value on several parts (e.g. hair style on hair_front + hair_back).</summary>
    public static void SetStateOnParts(Project project, CharacterEntity character, IEnumerable<string> partIds, string stateKey, string value)
    {
        foreach (string partId in partIds)
        {
            _ = SetState(project, character, partId, stateKey, value);
        }
    }

    /// <summary>
    /// Applies a skin-tone palette override to every skin-bearing part. The
    /// skin parts' palette overrides are REPLACED (they belong exclusively to
    /// the skin tone by convention), so an empty <paramref name="mappings"/>
    /// list resets to the default tone.
    /// </summary>
    public static void ApplySkinOverride(Project project, CharacterEntity character, IReadOnlyList<PaletteMapping> mappings)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(mappings);
        foreach (string partId in SkinPartIds)
        {
            PartAppearance appearance = EnsureAppearance(character, partId);
            appearance.PaletteOverride.Clear();
            foreach (PaletteMapping mapping in mappings)
            {
                appearance.PaletteOverride.Add(mapping);
            }
        }
    }
}
