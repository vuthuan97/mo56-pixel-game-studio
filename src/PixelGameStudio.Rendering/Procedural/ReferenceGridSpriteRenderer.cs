using PixelGameStudio.Core.Primitives;

namespace PixelGameStudio.Rendering.Procedural;

/// <summary>
/// Compact, grid-authored character source inspired by the supplied card-style
/// pixel renderer. It deliberately keeps the existing 32x46 project canvas and
/// layer names, while using block silhouettes, hue-shifted shadows and explicit
/// face/hair masks instead of ellipse-heavy shapes.
/// </summary>
public sealed class ReferenceGridSpriteRenderer : ICharacterLayerRenderer
{
    private const int OriginX = 6;
    private const int OriginY = 8;
    private readonly LegacySpriteRenderer _legacy = new();

    private static readonly string[] Face =
    [
        "....................",
        "....................",
        "....................",
        "....SSSSSSSSSSSS....",
        "...SSSSSSSSSSSSSS...",
        "...SSSSSSSSSSSSSS...",
        "...SSSSSSSSSSSSSS...",
        "...SSSSSSSSSSSSSS...",
        "...SSSEESSSSEESSS...",
        "...SSSEwSSSSEwSSS...",
        "...SSSEESSSSEESSS...",
        "...SccSSSSSSSSccS...",
        "...SSSSSSmmSSSSSS...",
        "....sSSSSSSSSSSs....",
        ".....sSSSSSSSSs.....",
        "........ssss........",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
    ];

    private static readonly string[] Outfit =
    [
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "......TTKKKKTT......",
        "...TTTTTTTTTTTTTT...",
        "...LTtTTTTTTTTtTt...",
        "...LTtTTTTTTTTtTt...",
        ".....TTTTTTTTTT.....",
        ".....TTTTTTTTTT.....",
        ".....tttttttttt.....",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
    ];

    private static readonly string[] Pants =
    [
        "....................", "....................", "....................", "....................",
        "....................", "....................", "....................", "....................",
        "....................", "....................", "....................", "....................",
        "....................", "....................", "....................", "....................",
        "....................", "....................", "....................", "....................",
        "....................", "....................", "....................",
        "......pPP.....PPp...",
        "......pPP.....PPp...",
        ".....fFFF.....FFFf..",
        ".....FFFF.....FFFF..",
        ".....dddd.....dddd..",
        "....................",
    ];

    private static readonly string[] HairBase =
    [
        "....................",
        "......HHHHHHHH......",
        "....HJJJHHHHHHHH....",
        "...HHHJJJJHHHHHHH...",
        "..HHHHHHJJHHHHHHHH..",
        "..HHHHHHHHHHHHHHHH..",
        "..HHHHHHHHHHHHHHHH..",
        "..HHHHHHH...HHHHHH..",
        "..HHH.........HHH...",
        "..HHh.........hHH...",
        "...Hh.........hH....",
        "....h.........h.....",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
    ];

    private static readonly string[] Bun =
    [
        "........JHHH........", "....................", "....................", "....................",
        "....................", "....................", "....................", "....................",
        "....................", "....................", "....................", "....................",
        "....................", "....................", "....................", "....................",
        "....................", "....................", "....................", "....................",
        "....................", "....................", "....................", "....................",
        "....................", "....................", "....................", "....................",
    ];

    private static readonly string[] Pigtails =
    [
        "....................", "....................", "....................", "....................",
        "....................", "....................", "....................", ".HH..............HH.",
        "HHH..............HHH", "HHh..............hHH", "HHh..............hHH", ".Hh..............hH.",
        ".hh..............hh.", "..h..............h..", "....................", "....................",
        "....................", "....................", "....................", "....................",
        "....................", "....................", "....................", "....................",
        "....................", "....................", "....................", "....................",
    ];

    private static Rgba32 Mix(Rgba32 a, Rgba32 b, byte amount)
    {
        int t = amount;
        return new Rgba32(
            (byte)(a.R + ((b.R - a.R) * t / 255)),
            (byte)(a.G + ((b.G - a.G) * t / 255)),
            (byte)(a.B + ((b.B - a.B) * t / 255)),
            255);
    }

    private static void Stamp(PixelBuffer target, string[] layer, int yOffset,
        IReadOnlyDictionary<char, Rgba32> palette, Func<char, bool>? filter = null)
    {
        for (int y = 0; y < layer.Length; y++)
        {
            string row = layer[y];
            for (int x = 0; x < row.Length; x++)
            {
                char key = row[x];
                if (key == '.' || (filter is not null && !filter(key)) || !palette.TryGetValue(key, out Rgba32 color))
                {
                    continue;
                }

                int px = OriginX + x;
                int py = OriginY + y + yOffset;
                if (target.Contains(px, py))
                {
                    target[px, py] = color;
                }
            }
        }
    }

    private static PixelBuffer Blank() => new(LegacyCatalog.CanvasWidth, LegacyCatalog.CanvasHeight);

    private static IReadOnlyDictionary<char, Rgba32> Palette(LegacySpriteSpec spec)
    {
        (Rgba32 skin, Rgba32 skinShadow) = LegacyCatalog.SkinTones.GetValueOrDefault(
            spec.SkinTone, LegacyCatalog.SkinTones.Values.First());
        (Rgba32 hair, Rgba32 hairDark, Rgba32 hairLight) = LegacyCatalog.HairPalettes.GetValueOrDefault(
            spec.HairColor, LegacyCatalog.HairPalettes.Values.First());
        (Rgba32 tunic, Rgba32 tunicDark, Rgba32 belt, Rgba32 beltDark) = LegacyCatalog.Palettes.GetValueOrDefault(
            spec.SkinVariant, LegacyCatalog.Palettes["Mặc định"]);

        Rgba32 purpleShadow = new(40, 30, 90, 255);
        Rgba32 warmLight = new(255, 240, 200, 255);
        return new Dictionary<char, Rgba32>
        {
            ['S'] = skin,
            ['s'] = Mix(skinShadow, purpleShadow, 45),
            ['c'] = Mix(skin, new Rgba32(255, 110, 120, 255), 90),
            ['m'] = Mix(skinShadow, new Rgba32(120, 40, 50, 255), 150),
            ['E'] = new Rgba32(38, 28, 48, 255),
            ['w'] = new Rgba32(255, 255, 255, 255),
            ['H'] = hair,
            ['J'] = Mix(hair, warmLight, 90),
            ['h'] = Mix(hairDark, purpleShadow, 45),
            ['T'] = tunic,
            ['L'] = Mix(tunic, warmLight, 76),
            ['t'] = Mix(tunicDark, purpleShadow, 76),
            ['K'] = Mix(tunic, warmLight, 140),
            ['B'] = belt,
            ['G'] = LegacyCatalog.Gold,
            ['P'] = tunicDark,
            ['p'] = Mix(tunicDark, purpleShadow, 76),
            ['F'] = LegacyCatalog.BootLeather,
            ['f'] = LegacyCatalog.BootCloth,
            ['d'] = Mix(LegacyCatalog.BootLeather, purpleShadow, 120),
            ['Q'] = Mix(beltDark, purpleShadow, 110),
        };
    }

    private static PixelBuffer Head(LegacySpriteSpec spec, LegacyPose pose, IReadOnlyDictionary<char, Rgba32> palette)
    {
        var image = Blank();
        Stamp(image, Face, pose.BodyDy + pose.HeadDy, palette, c => c is 'S' or 's');
        return image;
    }

    private static PixelBuffer FaceDetails(LegacySpriteSpec spec, LegacyPose pose, IReadOnlyDictionary<char, Rgba32> palette)
    {
        var image = Blank();
        if (spec.Direction == "Up")
        {
            return image;
        }

        Stamp(image, Face, pose.BodyDy + pose.HeadDy, palette, c => c is 'E' or 'w' or 'c' or 'm');

        // The reference face is intentionally sparse. Apply the editor's
        // selected controls without adding a thick mouth/eye contour.
        if (spec.EyeStyle == "Buồn ngủ")
        {
            ClearRow(image, OriginY + 8 + pose.BodyDy + pose.HeadDy);
        }

        if (spec.MouthStyle == "Nghiêm")
        {
            ClearRow(image, OriginY + 12 + pose.BodyDy + pose.HeadDy);
            Rgba32 mouth = palette['m'];
            int x = OriginX + 8;
            int y = OriginY + 12 + pose.BodyDy + pose.HeadDy;
            if (image.Contains(x, y)) image[x, y] = mouth;
            if (image.Contains(x + 1, y)) image[x + 1, y] = mouth;
            if (image.Contains(x + 2, y)) image[x + 2, y] = mouth;
        }
        else if (spec.MouthStyle == "Cười")
        {
            int x = OriginX + 8;
            int y = OriginY + 12 + pose.BodyDy + pose.HeadDy;
            Rgba32 mouth = palette['m'];
            foreach ((int dx, int dy) in new[] { (0, 0), (1, 1), (2, 0) })
            {
                if (image.Contains(x + dx, y + dy)) image[x + dx, y + dy] = mouth;
            }
        }

        return image;
    }

    private static void ClearRow(PixelBuffer image, int y)
    {
        if (y < 0 || y >= image.Height) return;
        for (int x = 0; x < image.Width; x++)
        {
            if (image[x, y].A > 0) image[x, y] = default;
        }
    }

    private static PixelBuffer Torso(LegacySpriteSpec spec, LegacyPose pose, IReadOnlyDictionary<char, Rgba32> palette)
    {
        var image = Blank();
        Stamp(image, Outfit, pose.BodyDy, palette, c => c is 'T' or 'L' or 't' or 'K' or 'B' or 'G');
        return image;
    }

    private static PixelBuffer Limb(LegacySpriteSpec spec, LegacyPose pose, string side, IReadOnlyDictionary<char, Rgba32> palette)
    {
        var image = Blank();
        string state = side == "left" ? pose.LeftArm : pose.RightArm;
        int x = side == "left" ? OriginX + 2 : OriginX + 15;
        int y = OriginY + 18 + pose.BodyDy;
        Rgba32 skin = palette['S'];
        Rgba32 shadow = palette['s'];
        Rgba32 color = state == "back" ? shadow : skin;
        int dx = state == "forward" ? (side == "left" ? -1 : 1) : state == "back" ? (side == "left" ? 1 : -1) : 0;
        FillBlock(image, x + dx, y, 2, 5, color);
        FillBlock(image, x + dx + (side == "left" ? -1 : 1), y + 4, 2, 4, color);
        return image;
    }

    private static PixelBuffer Leg(string side, LegacyPose pose, IReadOnlyDictionary<char, Rgba32> palette)
    {
        var image = Blank();
        int x = side == "left" ? OriginX + 5 : OriginX + 11;
        int y = OriginY + 23;
        string state = side == "left" ? pose.LeftLeg : pose.RightLeg;
        int dx = state == "forward" ? (side == "left" ? -1 : 1) : state == "back" ? (side == "left" ? 1 : -1) : 0;
        Stamp(image, Pants, 0, palette, c => c is 'P' or 'p' or 'F' or 'f' or 'd');
        // Keep leg motion readable while the shoe/pants palette comes from the
        // same grid, as in the reference implementation's fixed leg row.
        if (dx != 0)
        {
            PixelBuffer moved = new(LegacyCatalog.CanvasWidth, LegacyCatalog.CanvasHeight);
            for (int yy = y; yy < image.Height; yy++)
            {
                for (int xx = 0; xx < image.Width; xx++)
                {
                    if (image[xx, yy].A > 0 && xx >= x - 2 && xx <= x + 4)
                    {
                        int nx = xx + dx;
                        if (moved.Contains(nx, yy)) moved[nx, yy] = image[xx, yy];
                    }
                }
            }

            PixelOps.Composite(moved, image);
            return moved;
        }

        return image;
    }

    private static PixelBuffer Hair(LegacySpriteSpec spec, LegacyPose pose, IReadOnlyDictionary<char, Rgba32> palette, bool back)
    {
        var image = Blank();
        if (spec.HairStyle == "Không tóc")
        {
            return image;
        }

        int dy = pose.BodyDy + pose.HeadDy;

        // When looking upward the viewer sees the back of the head. Keep the
        // face layer empty and put the complete hair mass in the front slot so
        // the head skin cannot incorrectly read as a face or a fringe.
        if (spec.Direction == "Up")
        {
            if (back)
            {
                return image;
            }

            Stamp(image, HairBase, dy, palette);
            for (int row = 7; row <= 15; row++)
            {
                int left = row switch
                {
                    <= 9 => 2,
                    <= 12 => 3,
                    13 => 4,
                    14 => 5,
                    _ => 7,
                };
                int right = row switch
                {
                    <= 9 => 17,
                    <= 12 => 16,
                    13 => 15,
                    14 => 14,
                    _ => 12,
                };
                PixelDraw.FillRect(image, OriginX + left, OriginY + row + dy,
                    OriginX + right, OriginY + row + dy, palette[row is >= 13 ? 'h' : 'H']);
            }

            if (spec.HairStyle == "Búi cao")
            {
                Stamp(image, Bun, dy, palette);
            }
            else if (spec.HairStyle == "Hai búi")
            {
                Stamp(image, Pigtails, dy, palette);
            }

            return image;
        }

        Stamp(image, HairBase, dy, palette);
        switch (spec.HairStyle)
        {
            case "Búi cao":
                Stamp(image, Bun, dy, palette);
                break;
            case "Hai búi":
                Stamp(image, Pigtails, dy, palette);
                break;
            case "Tóc dựng":
                Stamp(image, HairBase, dy - 1, palette, c => c is 'H' or 'J');
                break;
            case "Tóc lệch":
            case "Rẽ ngôi nam":
                FillBlock(image, OriginX + 3, OriginY + 8 + dy, 3, 5, palette['H']);
                break;
            case "Undercut":
            case "Húi cua":
            case "Tóc nam ngắn":
                FillBlock(image, OriginX + 2, OriginY + 8 + dy, 2, 4, palette['H']);
                FillBlock(image, OriginX + 16, OriginY + 8 + dy, 2, 4, palette['H']);
                break;
            case "Xoăn nam":
                FillBlock(image, OriginX + 3, OriginY + 7 + dy, 3, 3, palette['H']);
                FillBlock(image, OriginX + 14, OriginY + 7 + dy, 3, 3, palette['H']);
                break;
        }

        if (back)
        {
            // Only retain side/back locks in the back layer. The crown remains
            // in hair_front so it is composited after face details.
            PixelBuffer result = Blank();
            for (int y = OriginY + 9 + dy; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    if (image[x, y].A > 0 && (x <= OriginX + 3 || x >= OriginX + 16))
                    {
                        result[x, y] = image[x, y];
                    }
                }
            }

            return result;
        }

        return image;
    }

    private static void FillBlock(PixelBuffer image, int x, int y, int width, int height, Rgba32 color)
    {
        PixelDraw.FillRect(image, x, y, x + width - 1, y + height - 1, color);
    }

    public List<(string Name, PixelBuffer Image)> RenderLayers(LegacySpriteSpec spec, LegacyPose pose)
    {
        IReadOnlyDictionary<char, Rgba32> palette = Palette(spec);
        Dictionary<string, PixelBuffer> legacy = _legacy.RenderLayers(spec, pose)
            .ToDictionary(x => x.Name, x => x.Image, StringComparer.Ordinal);
        var layers = new List<(string Name, PixelBuffer Image)>
        {
            ("effect_back", legacy["effect_back"]),
            ("hair_back", Hair(spec, pose, palette, back: true)),
            ("weapon_back", legacy["weapon_back"]),
            ("left_leg", Leg("left", pose, palette)),
            ("right_leg", Leg("right", pose, palette)),
            ("body", Torso(spec, pose, palette)),
            ("left_arm", Limb(spec, pose, "left", palette)),
            ("right_arm", Limb(spec, pose, "right", palette)),
            ("head", Head(spec, pose, palette)),
            ("face", FaceDetails(spec, pose, palette)),
            ("hair_front", Hair(spec, pose, palette, back: false)),
            ("weapon_front", legacy["weapon_front"]),
        };

        // Equipment remains data-driven and keeps the existing slot shapes.
        // This renderer only changes the character's body art source.
        foreach ((string name, PixelBuffer image) in legacy)
        {
            if (!layers.Any(x => x.Name == name) && name is not "head_equipment")
            {
                layers.Insert(Math.Min(6, layers.Count), (name, image));
            }
        }

        if (legacy.TryGetValue("head_equipment", out PixelBuffer? headEquipment))
        {
            layers.Add(("head_equipment", headEquipment));
        }

        return layers;
    }
}
