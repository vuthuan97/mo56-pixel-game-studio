namespace PixelGameStudio.Domain.Character;

/// <summary>Per-character gender and construction data with bounded renderer deltas.</summary>
public sealed class CharacterBuildProfile
{
    public const int DefaultHeadHeightPx = 12;
    public const int DefaultTorsoHeightPx = 14;
    public const int DefaultArmLengthPx = 10;
    public const int DefaultLegLengthPx = 16;
    public const int DefaultFootWidthPx = 4;

    public string Gender { get; set; } = "Unspecified";
    public string BodyType { get; set; } = "Standard";
    public int HeadHeightPx { get; set; } = DefaultHeadHeightPx;
    public int TorsoHeightPx { get; set; } = DefaultTorsoHeightPx;
    public int ArmLengthPx { get; set; } = DefaultArmLengthPx;
    public int LegLengthPx { get; set; } = DefaultLegLengthPx;
    public int FootWidthPx { get; set; } = DefaultFootWidthPx;

    /// <summary>Clamps manual edits to the native character envelope.</summary>
    public void Normalize()
    {
        HeadHeightPx = Math.Clamp(HeadHeightPx, 8, 24);
        TorsoHeightPx = Math.Clamp(TorsoHeightPx, 8, 24);
        ArmLengthPx = Math.Clamp(ArmLengthPx, 6, 20);
        LegLengthPx = Math.Clamp(LegLengthPx, 8, 24);
        FootWidthPx = Math.Clamp(FootWidthPx, 2, 10);
        Gender = string.IsNullOrWhiteSpace(Gender) ? "Unspecified" : Gender.Trim();
        BodyType = string.IsNullOrWhiteSpace(BodyType) ? "Standard" : BodyType.Trim();
    }

    /// <summary>Small integer offsets applied to a matching rig part/anchor.</summary>
    public (int X, int Y) PartOffset(string partId)
    {
        int headDelta = Math.Clamp(HeadHeightPx - DefaultHeadHeightPx, -6, 6);
        int torsoDelta = Math.Clamp(TorsoHeightPx - DefaultTorsoHeightPx, -6, 6);
        int armDelta = Math.Clamp(ArmLengthPx - DefaultArmLengthPx, -4, 4);
        int legDelta = Math.Clamp(LegLengthPx - DefaultLegLengthPx, -6, 6);
        return partId switch
        {
            "head" or "face" or "hair_front" or "hair_back" => (0, -torsoDelta - headDelta),
            "arm_left" or "arm_right" => (0, torsoDelta - armDelta),
            "leg_left" or "leg_right" => (0, torsoDelta),
            "weapon" => (0, torsoDelta - armDelta),
            _ => (0, torsoDelta + legDelta),
        };
    }
}
