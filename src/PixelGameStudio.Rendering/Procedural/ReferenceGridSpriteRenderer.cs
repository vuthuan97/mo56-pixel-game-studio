using PixelGameStudio.Core.Primitives;

namespace PixelGameStudio.Rendering.Procedural;

/// <summary>
/// Small, integer-only starter-art renderer. The legacy renderer remains the
/// reference renderer; this renderer is used only while the starter library is
/// generated. Output is split into body, face, hair and individual limbs so
/// the asset composer can move each part independently.
/// </summary>
public sealed class ReferenceGridSpriteRenderer : ICharacterLayerRenderer
{
    /// <summary>Increment when generated starter art changes.</summary>
    public const int CurrentVersion = 3;

    private const int DesignWidth = 32;
    private const int DesignHeight = 46;
    private readonly LegacySpriteRenderer _legacy = new();

    private readonly record struct Layout(int OffsetX, int OffsetY, int Width, int Height)
    {
        public static Layout From(LegacySpriteSpec spec)
        {
            int canvasWidth = Math.Max(1, spec.CanvasWidth);
            int canvasHeight = Math.Max(1, spec.CanvasHeight);
            int width = Math.Max(1, Math.Min(canvasWidth, canvasHeight * DesignWidth / DesignHeight));
            int height = Math.Max(1, Math.Min(canvasHeight, canvasWidth * DesignHeight / DesignWidth));
            return new Layout((canvasWidth - width) / 2, (canvasHeight - height) / 2, width, height);
        }

        public int X(int designX) => OffsetX + (int)Math.Round(designX * (Width / (double)DesignWidth), MidpointRounding.AwayFromZero);

        public int Y(int designY) => OffsetY + (int)Math.Round(designY * (Height / (double)DesignHeight), MidpointRounding.AwayFromZero);

        public void Rect(PixelBuffer image, int left, int top, int right, int bottom, Rgba32 color)
        {
            int x1 = Math.Min(X(left), X(right));
            int x2 = Math.Max(X(left), X(right));
            int y1 = Math.Min(Y(top), Y(bottom));
            int y2 = Math.Max(Y(top), Y(bottom));
            PixelDraw.FillRect(image, x1, y1, x2, y2, color);
        }

        public void Point(PixelBuffer image, int x, int y, Rgba32 color)
        {
            int px = X(x);
            int py = Y(y);
            if (image.Contains(px, py))
            {
                image[px, py] = color;
            }
        }
    }

    private static PixelBuffer Blank(LegacySpriteSpec spec) => new(spec.CanvasWidth, spec.CanvasHeight);

    private static IReadOnlyDictionary<char, Rgba32> Palette(LegacySpriteSpec spec)
    {
        (Rgba32 skin, Rgba32 skinShadow) = LegacyCatalog.SkinTones.GetValueOrDefault(
            spec.SkinTone, LegacyCatalog.SkinTones.Values.First());
        (Rgba32 hair, Rgba32 hairDark, Rgba32 hairLight) = LegacyCatalog.HairPalettes.GetValueOrDefault(
            spec.HairColor, LegacyCatalog.HairPalettes.Values.First());

        return new Dictionary<char, Rgba32>
        {
            ['S'] = skin,
            ['s'] = skinShadow,
            ['H'] = hair,
            ['h'] = hairDark,
            ['J'] = hairLight,
            ['E'] = LegacyCatalog.Eye,
            ['e'] = LegacyCatalog.EyeLight,
            ['M'] = LegacyCatalog.Mouth,
            ['m'] = LegacyCatalog.MouthSerious,
            ['c'] = LegacyCatalog.Blush,
        };
    }

    private static int BodyDx(LegacyPose pose) => pose.BodyDx;

    private static int BodyDy(LegacyPose pose) => pose.BodyDy;

    private static int HeadDy(LegacyPose pose) => pose.BodyDy + pose.HeadDy;

    private static bool Is(LegacySpriteSpec spec, string direction) =>
        string.Equals(spec.Direction, direction, StringComparison.OrdinalIgnoreCase);

    private static bool IsNoHair(string style) =>
        style.Equals("Không tóc", StringComparison.OrdinalIgnoreCase);

    private static bool IsLongHair(string style) => style is "Tóc dài" or "Tóc xõa" or "Tóc mái dài";

    private static bool IsMaleHair(string style) =>
        style.Contains("nam", StringComparison.OrdinalIgnoreCase) ||
        style.Equals("Undercut", StringComparison.OrdinalIgnoreCase) ||
        style.Equals("Húi cua", StringComparison.OrdinalIgnoreCase);

    private static void DrawHead(
        LegacySpriteSpec spec, LegacyPose pose, Layout layout,
        IReadOnlyDictionary<char, Rgba32> palette, PixelBuffer image)
    {
        int dx = BodyDx(pose) + pose.HeadDx;
        int dy = HeadDy(pose);
        Rgba32 skin = palette['S'];
        Rgba32 shadow = palette['s'];

        if (Is(spec, "Left"))
        {
            layout.Rect(image, 8 + dx, 8 + dy, 20 + dx, 20 + dy, skin);
            layout.Rect(image, 9 + dx, 6 + dy, 19 + dx, 8 + dy, skin);
            layout.Rect(image, 8 + dx, 17 + dy, 10 + dx, 20 + dy, shadow);
            layout.Rect(image, 9 + dx, 21 + dy, 17 + dx, 22 + dy, shadow);
            return;
        }

        if (Is(spec, "Right"))
        {
            layout.Rect(image, 12 + dx, 8 + dy, 24 + dx, 20 + dy, skin);
            layout.Rect(image, 13 + dx, 6 + dy, 23 + dx, 8 + dy, skin);
            layout.Rect(image, 22 + dx, 17 + dy, 24 + dx, 20 + dy, shadow);
            layout.Rect(image, 15 + dx, 21 + dy, 23 + dx, 22 + dy, shadow);
            return;
        }

        layout.Rect(image, 10 + dx, 6 + dy, 21 + dx, 7 + dy, skin);
        layout.Rect(image, 8 + dx, 8 + dy, 23 + dx, 19 + dy, skin);
        layout.Rect(image, 10 + dx, 20 + dy, 21 + dx, 21 + dy, shadow);
        if (Is(spec, "Up"))
        {
            layout.Rect(image, 11 + dx, 7 + dy, 20 + dx, 8 + dy, shadow);
            layout.Rect(image, 10 + dx, 18 + dy, 21 + dx, 20 + dy, shadow);
        }
    }

    private static PixelBuffer Head(LegacySpriteSpec spec, LegacyPose pose, IReadOnlyDictionary<char, Rgba32> palette)
    {
        var image = Blank(spec);
        DrawHead(spec, pose, Layout.From(spec), palette, image);
        return image;
    }

    private static PixelBuffer FaceDetails(LegacySpriteSpec spec, LegacyPose pose, IReadOnlyDictionary<char, Rgba32> palette)
    {
        var image = Blank(spec);
        if (Is(spec, "Up"))
        {
            return image;
        }

        Layout layout = Layout.From(spec);
        int dx = BodyDx(pose) + pose.HeadDx;
        int dy = HeadDy(pose);
        Rgba32 eye = palette['E'];
        Rgba32 eyeLight = palette['e'];
        Rgba32 mouth = palette['M'];
        Rgba32 serious = palette['m'];

        if (Is(spec, "Left") || Is(spec, "Right"))
        {
            int side = Is(spec, "Left") ? -1 : 1;
            int eyeX = Is(spec, "Left") ? 10 : 20;
            layout.Rect(image, eyeX + dx, 14 + dy, eyeX + 1 + dx, 16 + dy, eye);
            layout.Point(image, eyeX + dx, 14 + dy, eyeLight);
            layout.Point(image, eyeX + 3 * side + dx, 18 + dy, palette['c']);
            DrawMouth(layout, image, 14 + dx, 19 + dy, spec.MouthStyle, mouth, serious, side);
            return image;
        }

        switch (spec.EyeStyle)
        {
            case "Buồn ngủ":
                layout.Rect(image, 11 + dx, 15 + dy, 12 + dx, 15 + dy, eye);
                layout.Rect(image, 18 + dx, 15 + dy, 19 + dx, 15 + dy, eye);
                break;
            case "Lạnh lùng":
            case "Nghiêm túc":
                layout.Rect(image, 11 + dx, 15 + dy, 13 + dx, 16 + dy, eye);
                layout.Rect(image, 17 + dx, 15 + dy, 19 + dx, 16 + dy, eye);
                break;
            case "Vui":
                layout.Rect(image, 11 + dx, 14 + dy, 13 + dx, 15 + dy, eye);
                layout.Rect(image, 17 + dx, 14 + dy, 19 + dx, 15 + dy, eye);
                break;
            default:
                layout.Rect(image, 11 + dx, 14 + dy, 12 + dx, 16 + dy, eye);
                layout.Rect(image, 18 + dx, 14 + dy, 19 + dx, 16 + dy, eye);
                layout.Point(image, 11 + dx, 14 + dy, eyeLight);
                layout.Point(image, 18 + dx, 14 + dy, eyeLight);
                break;
        }

        layout.Point(image, 9 + dx, 18 + dy, palette['c']);
        layout.Point(image, 21 + dx, 18 + dy, palette['c']);
        DrawMouth(layout, image, 15 + dx, 19 + dy, spec.MouthStyle, mouth, serious, 0);
        return image;
    }

    private static void DrawMouth(Layout layout, PixelBuffer image, int x, int y, string style,
        Rgba32 normal, Rgba32 serious, int side)
    {
        if (style.Equals("Nghiêm", StringComparison.OrdinalIgnoreCase))
        {
            layout.Rect(image, x, y, x + (side == 0 ? 2 : side), y, serious);
            return;
        }

        if (style.Equals("Cười", StringComparison.OrdinalIgnoreCase))
        {
            layout.Point(image, x, y, normal);
            layout.Point(image, x + (side == 0 ? 1 : side), y + 1, normal);
            layout.Point(image, x + (side == 0 ? 2 : side * 2), y, normal);
            return;
        }

        layout.Rect(image, x, y, x + (side == 0 ? 1 : side), y, normal);
    }

    private static PixelBuffer Torso(LegacySpriteSpec spec, LegacyPose pose, IReadOnlyDictionary<char, Rgba32> palette)
    {
        var image = Blank(spec);
        Layout layout = Layout.From(spec);
        Rgba32 skin = palette['S'];
        Rgba32 shadow = palette['s'];
        int dx = BodyDx(pose);
        int dy = BodyDy(pose);

        (int shoulderLeft, int shoulderRight, int waistLeft, int waistRight) = spec.BodyType switch
        {
            "Mảnh" => (12, 19, 12, 19),
            "Đậm" => (10, 21, 9, 22),
            _ => (11, 20, 11, 20),
        };

        if (Is(spec, "Left"))
        {
            layout.Rect(image, 10 + dx, 24 + dy, 19 + dx, 34 + dy, skin);
            layout.Rect(image, 11 + dx, 23 + dy, 17 + dx, 25 + dy, skin);
            layout.Rect(image, 10 + dx, 30 + dy, 12 + dx, 35 + dy, shadow);
            return image;
        }

        if (Is(spec, "Right"))
        {
            layout.Rect(image, 13 + dx, 24 + dy, 22 + dx, 34 + dy, skin);
            layout.Rect(image, 15 + dx, 23 + dy, 21 + dx, 25 + dy, skin);
            layout.Rect(image, 20 + dx, 30 + dy, 22 + dx, 35 + dy, shadow);
            return image;
        }

        layout.Rect(image, shoulderLeft + dx, 23 + dy, shoulderRight + dx, 34 + dy, skin);
        layout.Rect(image, waistLeft + dx, 34 + dy, waistRight + dx, 36 + dy, skin);
        layout.Rect(image, shoulderLeft + dx, 31 + dy, shoulderLeft + 1 + dx, 35 + dy, shadow);
        layout.Rect(image, shoulderRight - 1 + dx, 31 + dy, shoulderRight + dx, 35 + dy,
            Is(spec, "Up") ? shadow : skin);
        if (Is(spec, "Up"))
        {
            layout.Rect(image, shoulderLeft + 2 + dx, 24 + dy, shoulderRight - 2 + dx, 25 + dy, shadow);
        }

        return image;
    }

    private static PixelBuffer Limb(LegacySpriteSpec spec, LegacyPose pose, string side,
        IReadOnlyDictionary<char, Rgba32> palette)
    {
        var image = Blank(spec);
        Layout layout = Layout.From(spec);
        string state = side == "left" ? pose.LeftArm : pose.RightArm;
        bool left = side == "left";
        int dy = BodyDy(pose);
        int sideSign = left ? -1 : 1;
        int shoulder = Is(spec, "Left") ? (left ? 9 : 13) :
            Is(spec, "Right") ? (left ? 18 : 22) : (left ? 10 : 21);
        if (Is(spec, "Up"))
        {
            shoulder = left ? 10 : 21;
        }

        Rgba32 color = state.Equals("back", StringComparison.OrdinalIgnoreCase) ? palette['s'] : palette['S'];
        int x = shoulder + BodyDx(pose);
        switch (state)
        {
            case "up":
                layout.Rect(image, x, 23 + dy, x + 2, 26 + dy, color);
                layout.Rect(image, x + sideSign * 2, 19 + dy, x + sideSign * 2 + 2, 23 + dy, color);
                layout.Rect(image, x + sideSign * 2, 17 + dy, x + sideSign * 2 + 2, 19 + dy, palette['S']);
                break;
            case "forward":
                layout.Rect(image, x, 23 + dy, x + 2, 27 + dy, color);
                layout.Rect(image, x + sideSign * 2, 27 + dy, x + sideSign * 4, 31 + dy, color);
                layout.Rect(image, x + sideSign * 4, 30 + dy, x + sideSign * 5, 33 + dy, palette['S']);
                break;
            case "back":
                layout.Rect(image, x, 23 + dy, x + 2, 27 + dy, color);
                layout.Rect(image, x - sideSign, 28 + dy, x - sideSign * 2, 33 + dy, color);
                layout.Rect(image, x - sideSign * 2, 32 + dy, x - sideSign * 3, 34 + dy, palette['S']);
                break;
            default:
                layout.Rect(image, x, 23 + dy, x + 2, 28 + dy, color);
                layout.Rect(image, x + sideSign, 28 + dy, x + sideSign * 2, 33 + dy, color);
                layout.Rect(image, x + sideSign * 2, 32 + dy, x + sideSign * 3, 34 + dy, palette['S']);
                break;
        }

        return image;
    }

    private static PixelBuffer Leg(LegacySpriteSpec spec, LegacyPose pose, string side,
        IReadOnlyDictionary<char, Rgba32> palette)
    {
        // This layer is intentionally one leg only. Never stamp a shared pants
        // grid here and never composite the unshifted image back.
        var image = Blank(spec);
        Layout layout = Layout.From(spec);
        bool left = side == "left";
        string state = left ? pose.LeftLeg : pose.RightLeg;
        int baseX = Is(spec, "Left") ? (left ? 11 : 16) :
            Is(spec, "Right") ? (left ? 13 : 18) : (left ? 11 : 17);
        int dx = state.Equals("forward", StringComparison.OrdinalIgnoreCase) ? (left ? -1 : 1) :
            state.Equals("back", StringComparison.OrdinalIgnoreCase) ? (left ? 1 : -1) : 0;
        int dy = BodyDy(pose);
        Rgba32 skin = palette['S'];
        Rgba32 shadow = palette['s'];
        Rgba32 legColor = state.Equals("back", StringComparison.OrdinalIgnoreCase) ? shadow : skin;

        layout.Rect(image, baseX + dx, 35 + dy, baseX + 2 + dx, 41 + dy, legColor);
        layout.Rect(image, baseX - 1 + dx, 41 + dy, baseX + 3 + dx, 43 + dy, shadow);
        return image;
    }

    private static PixelBuffer Hair(LegacySpriteSpec spec, LegacyPose pose,
        IReadOnlyDictionary<char, Rgba32> palette, bool back)
    {
        var image = Blank(spec);
        if (IsNoHair(spec.HairStyle))
        {
            return image;
        }

        Layout layout = Layout.From(spec);
        int dx = BodyDx(pose) + pose.HeadDx;
        int dy = HeadDy(pose);
        string style = spec.HairStyle;
        Rgba32 hair = palette['H'];
        Rgba32 dark = palette['h'];
        Rgba32 light = palette['J'];

        // Up is the back-of-head silhouette: only hair_front carries it after
        // head composition; hair_back stays empty and cannot leak locks.
        if (Is(spec, "Up"))
        {
            if (back)
            {
                return image;
            }

            layout.Rect(image, 8 + dx, 4 + dy, 23 + dx, 17 + dy, hair);
            layout.Rect(image, 10 + dx, 6 + dy, 21 + dx, 8 + dy, light);
            layout.Rect(image, 9 + dx, 15 + dy, 22 + dx, 23 + dy, dark);
            AddHairStyleExtras(layout, image, style, dx, dy, hair, dark, light, upView: true);
            return image;
        }

        if (back)
        {
            if (IsLongHair(style))
            {
                if (Is(spec, "Left"))
                    layout.Rect(image, 7 + dx, 10 + dy, 11 + dx, 28 + dy, dark);
                else if (Is(spec, "Right"))
                    layout.Rect(image, 21 + dx, 10 + dy, 25 + dx, 28 + dy, dark);
                else
                {
                    layout.Rect(image, 6 + dx, 11 + dy, 9 + dx, 27 + dy, dark);
                    layout.Rect(image, 22 + dx, 11 + dy, 25 + dx, 27 + dy, dark);
                }
            }
            else if (Is(spec, "Left"))
            {
                layout.Rect(image, 7 + dx, 10 + dy, 10 + dx, 21 + dy, dark);
            }
            else if (Is(spec, "Right"))
            {
                layout.Rect(image, 22 + dx, 10 + dy, 25 + dx, 21 + dy, dark);
            }
            else
            {
                layout.Rect(image, 7 + dx, 10 + dy, 9 + dx, 22 + dy, dark);
                layout.Rect(image, 22 + dx, 10 + dy, 24 + dx, 22 + dy, dark);
            }

            if (style.Equals("Đuôi ngựa", StringComparison.OrdinalIgnoreCase))
            {
                int tailX = Is(spec, "Left") ? 5 : 24;
                layout.Rect(image, tailX + dx, 17 + dy, tailX + 3 + dx, 27 + dy, dark);
            }

            return image;
        }

        layout.Rect(image, 8 + dx, 3 + dy, 23 + dx, 10 + dy, hair);
        layout.Rect(image, 11 + dx, 5 + dy, 14 + dx, 5 + dy, light);
        AddHairStyleExtras(layout, image, style, dx, dy, hair, dark, light, upView: false);
        return image;
    }

    private static void AddHairStyleExtras(Layout layout, PixelBuffer image, string style,
        int dx, int dy, Rgba32 hair, Rgba32 dark, Rgba32 light, bool upView)
    {
        if (style.Equals("Búi cao", StringComparison.OrdinalIgnoreCase))
        {
            layout.Rect(image, 13 + dx, 0 + dy, 18 + dx, 4 + dy, hair);
            layout.Point(image, 15 + dx, 4 + dy, LegacyCatalog.Gold);
        }
        else if (style.Equals("Hai búi", StringComparison.OrdinalIgnoreCase))
        {
            layout.Rect(image, 5 + dx, 3 + dy, 10 + dx, 8 + dy, dark);
            layout.Rect(image, 21 + dx, 3 + dy, 26 + dx, 8 + dy, dark);
        }
        else if (style.Equals("Tóc dựng", StringComparison.OrdinalIgnoreCase))
        {
            layout.Rect(image, 10 + dx, 1 + dy, 12 + dx, 7 + dy, hair);
            layout.Rect(image, 16 + dx, 0 + dy, 18 + dx, 7 + dy, light);
            layout.Rect(image, 20 + dx, 2 + dy, 21 + dx, 7 + dy, hair);
        }
        else if (style.Equals("Tóc lệch", StringComparison.OrdinalIgnoreCase) ||
                 style.Equals("Rẽ ngôi nam", StringComparison.OrdinalIgnoreCase))
        {
            layout.Rect(image, 8 + dx, 7 + dy, 14 + dx, 14 + dy, hair);
            layout.Rect(image, 18 + dx, 5 + dy, 21 + dx, 10 + dy, hair);
        }
        else if (style.Equals("Tóc mái dài", StringComparison.OrdinalIgnoreCase))
        {
            layout.Rect(image, 8 + dx, 8 + dy, 11 + dx, 16 + dy, hair);
            layout.Rect(image, 20 + dx, 8 + dy, 23 + dx, 16 + dy, hair);
        }
        else if (IsMaleHair(style))
        {
            layout.Rect(image, 9 + dx, 8 + dy, 12 + dx, 13 + dy, hair);
            layout.Rect(image, 20 + dx, 8 + dy, 22 + dx, 12 + dy, hair);
            if (style.Equals("Húi cua", StringComparison.OrdinalIgnoreCase))
            {
                layout.Rect(image, 10 + dx, 5 + dy, 21 + dx, 8 + dy, hair);
            }
        }

        if (upView && style.Equals("Tóc dựng", StringComparison.OrdinalIgnoreCase))
        {
            layout.Rect(image, 12 + dx, 2 + dy, 19 + dx, 5 + dy, light);
        }
    }

    private static PixelBuffer NormalizeLegacyLayer(PixelBuffer source, LegacySpriteSpec spec)
    {
        if (source.Width == spec.CanvasWidth && source.Height == spec.CanvasHeight)
        {
            return source;
        }

        Layout layout = Layout.From(spec);
        PixelBuffer scaled = PixelOps.NearestResize(source, layout.Width, layout.Height);
        var result = Blank(spec);
        PixelOps.Composite(result, scaled, layout.OffsetX, layout.OffsetY);
        return result;
    }

    public List<(string Name, PixelBuffer Image)> RenderLayers(LegacySpriteSpec spec, LegacyPose pose)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(pose);
        if (spec.CanvasWidth <= 0 || spec.CanvasHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(spec), "Canvas size must be positive.");
        }

        IReadOnlyDictionary<char, Rgba32> palette = Palette(spec);
        Dictionary<string, PixelBuffer> legacy = _legacy.RenderLayers(spec, pose)
            .ToDictionary(x => x.Name, x => NormalizeLegacyLayer(x.Image, spec), StringComparer.Ordinal);

        var layers = new List<(string Name, PixelBuffer Image)>
        {
            ("effect_back", legacy["effect_back"]),
            ("hair_back", Hair(spec, pose, palette, back: true)),
            ("weapon_back", legacy["weapon_back"]),
            ("left_leg", Leg(spec, pose, "left", palette)),
            ("right_leg", Leg(spec, pose, "right", palette)),
            ("body", Torso(spec, pose, palette)),
            ("left_arm", Limb(spec, pose, "left", palette)),
            ("right_arm", Limb(spec, pose, "right", palette)),
            ("head", Head(spec, pose, palette)),
            ("face", FaceDetails(spec, pose, palette)),
            ("hair_front", Hair(spec, pose, palette, back: false)),
            ("weapon_front", legacy["weapon_front"]),
        };

        // Equipment/effects are supplied by the legacy catalogue, but are
        // normalized to the same full-canvas contract. Generated equipment
        // has absolute pixels and zero anchor offset; the composer must not add
        // an anchor a second time.
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
