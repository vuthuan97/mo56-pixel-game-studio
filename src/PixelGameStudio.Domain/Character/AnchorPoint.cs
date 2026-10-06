namespace PixelGameStudio.Domain.Character;

/// <summary>
/// A named point attached to a part, in that part's canvas coordinates
/// (offset relative to the part's pivot/origin). Used for attaching equipment,
/// held items, interaction anchors and effect markers.
/// </summary>
public sealed record AnchorPoint(string Id, string PartId, int OffsetX, int OffsetY);
