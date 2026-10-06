namespace PixelGameStudio.Domain.Profiles;

/// <summary>Well-known game genres from the product spec. Projects may also use any custom genre string.</summary>
public static class GameGenres
{
    public const string ManagementTycoon = "Quản lý/Tycoon";
    public const string Survival = "Sinh tồn";
    public const string TopDownRpg = "Nhập vai top-down";
    public const string SideScroller = "Đi cảnh ngang";
    public const string Platformer = "Nền tảng";
    public const string Adventure = "Phiêu lưu/Trải nghiệm";
    public const string Tactics = "Chiến thuật";
    public const string Custom = "Tùy chỉnh";

    public static IReadOnlyList<string> All { get; } =
    [
        ManagementTycoon, Survival, TopDownRpg, SideScroller, Platformer, Adventure, Tactics, Custom,
    ];
}

/// <summary>Which game the project produces assets for. The genre is descriptive metadata; nothing branches on it.</summary>
public sealed class GameProfile
{
    public string Genre { get; set; } = GameGenres.TopDownRpg;

    /// <summary>Free-form notes (platform, target resolution, art bible link, ...).</summary>
    public string Notes { get; set; } = string.Empty;
}
