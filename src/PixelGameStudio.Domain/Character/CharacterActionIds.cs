using System.Security.Cryptography;
using System.Text;

namespace PixelGameStudio.Domain.Character;

/// <summary>Stable project IDs for generated, character-owned action output.</summary>
public static class CharacterActionIds
{
    public static string Animation(string characterId, string templateId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterId);
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);
        string owner = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(characterId)))[..16]
            .ToLowerInvariant();
        return $"action.{templateId}.{owner}";
    }

    public static string Pose(string characterId, string templateId, int frameIndex) =>
        $"{Animation(characterId, templateId)}.{frameIndex}";

    /// <summary>Pre-MO56 projects stored action output globally. Keep it readable.</summary>
    public static string LegacyAnimation(string templateId) => $"action.{templateId}";
}
