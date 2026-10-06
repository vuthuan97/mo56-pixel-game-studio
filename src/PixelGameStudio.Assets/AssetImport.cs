using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Assets;

namespace PixelGameStudio.Assets;

/// <summary>Options for importing one PNG into the project asset library.</summary>
public sealed class AssetImportOptions
{
    /// <summary>Explicit id; defaults to the slugified source file name.</summary>
    public string? AssetId { get; set; }

    public string? DisplayName { get; set; }

    public string Type { get; set; } = AssetTypes.Custom;

    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <summary>Project direction keys this asset covers; empty = view-independent.</summary>
    public IReadOnlyList<string> Views { get; set; } = [];

    public AssetAnchor? Anchor { get; set; }

    public int ZIndex { get; set; }

    /// <summary>
    /// Audit §10: the importer must NOT silently rescale foreign art. When the
    /// source canvas differs from the project PixelProfile, import fails unless
    /// this is set — and even then the rescale is nearest-neighbor only.
    /// </summary>
    public bool AllowResize { get; set; }

    public bool OverwriteExisting { get; set; }
}

/// <summary>Raised when an import is rejected; the message is user-presentable (Vietnamese).</summary>
public sealed class AssetImportException : Exception
{
    public AssetImportException(string message)
        : base(message)
    {
    }
}

/// <summary>Outcome of a successful import.</summary>
public sealed class AssetImportResult
{
    public required AssetDefinition Definition { get; init; }

    public required IReadOnlyList<string> Warnings { get; init; }

    public required bool Resized { get; init; }
}
