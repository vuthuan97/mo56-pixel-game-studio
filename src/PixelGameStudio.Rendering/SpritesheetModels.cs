using PixelGameStudio.Core.Primitives;

namespace PixelGameStudio.Rendering;

/// <summary>One cell of a packed spritesheet: which frame key landed at which grid position.</summary>
public sealed record SpritesheetCell(string Key, int Column, int Row);

/// <summary>Result of packing frames into a spritesheet, with the frame map the legacy exporter lacked (audit §9).</summary>
public sealed record SpritesheetPackResult(PixelBuffer Sheet, IReadOnlyList<SpritesheetCell> Cells, int CellWidth, int CellHeight);
