using PixelGameStudio.Core.Primitives;

namespace PixelGameStudio.Rendering.Procedural;

/// <summary>
/// Bit-exact port of the legacy prototype CharacterRenderer (procedural Pillow
/// drawing on a 32x46 canvas). Serves as the golden reference renderer and as a
/// placeholder/template renderer until the asset-driven renderer replaces it.
/// Known quirks are ported as-is (see docs/LEGACY_AUDIT.md §4): cape composites
/// over the body, weapon states ready/recover/back render like idle.
/// </summary>
public sealed class LegacySpriteRenderer : ICharacterLayerRenderer
{
    private readonly Dictionary<string, PixelBuffer> _customOverlays = new();
    private LegacyAnchorStore? _anchorStore;

    public void SetAnchorStore(LegacyAnchorStore store) => _anchorStore = store;

    public void SetCustomOverlay(string slot, PixelBuffer overlay) => _customOverlays[slot] = overlay;

    public void ClearCustomOverlays() => _customOverlays.Clear();

    private static int DirectionOffset(LegacySpriteSpec s) => s.Direction switch
    {
        "Left" => -1,
        "Right" => 1,
        _ => 0,
    };

    private PixelBuffer Blank() => new(LegacyCatalog.CanvasWidth, LegacyCatalog.CanvasHeight);

    private PixelBuffer Body(LegacySpriteSpec s, LegacyPose p)
    {
        var i = Blank();
        (Rgba32 skin, Rgba32 shadow) = LegacyCatalog.SkinTones[s.SkinTone];
        int x;
        int w;
        switch (s.BodyType)
        {
            case "Mảnh":
                x = 12;
                w = 8;
                break;
            case "Đậm":
                x = 10;
                w = 12;
                break;
            default:
                x = 11;
                w = 10;
                break;
        }

        int bob = p.BodyDy;
        int dx = DirectionOffset(s) + p.BodyDx;
        if (s.Direction == "Up")
        {
            PixelDraw.FillRect(i, x - 1 + dx, 22 + bob, x + w + dx, 25 + bob, shadow);
            PixelDraw.FillRect(i, x + dx, 26 + bob, x + w - 1 + dx, 33 + bob, skin);
            PixelDraw.FillRect(i, x + 1 + dx, 34 + bob, x + w - 2 + dx, 37 + bob, skin);
        }
        else if (s.Direction == "Down")
        {
            PixelDraw.FillRect(i, x - 1 + dx, 22 + bob, x + w + dx, 26 + bob, skin);
            PixelDraw.FillRect(i, x + dx, 27 + bob, x + w - 1 + dx, 33 + bob, skin);
            PixelDraw.FillRect(i, x + 1 + dx, 33 + bob, x + w - 2 + dx, 36 + bob, skin);
            PixelDraw.FillRect(i, x + 1 + dx, 35 + bob, x + w - 2 + dx, 36 + bob, shadow);
        }
        else
        {
            PixelDraw.FillRect(i, x + 1 + dx, 22 + bob, x + w - 1 + dx, 25 + bob, skin);
            PixelDraw.FillRect(i, x + dx, 26 + bob, x + w - 3 + dx, 33 + bob, skin);
            PixelDraw.FillRect(i, x + 1 + dx, 34 + bob, x + w - 3 + dx, 37 + bob, skin);
            PixelDraw.FillRect(i, x + 1 + dx, 35 + bob, x + w - 4 + dx, 36 + bob, shadow);
        }

        PixelDraw.FillRect(i, x + 2 + dx, 24 + bob, x + 1 + dx + Math.Max(2, w - 4), 24 + bob, LegacyCatalog.ChestHighlight);
        return i;
    }

    private PixelBuffer Head(LegacySpriteSpec s, LegacyPose p)
    {
        var i = Blank();
        (Rgba32 skin, Rgba32 shadow) = LegacyCatalog.SkinTones[s.SkinTone];
        int dx = DirectionOffset(s) + p.HeadDx;
        int dy = p.HeadDy + p.BodyDy;
        if (s.HeadShape == "Gọn")
        {
            PixelDraw.FillEllipse(i, 9 + dx, 7 + dy, 22 + dx, 21 + dy, skin);
        }
        else
        {
            PixelDraw.FillEllipse(i, 8 + dx, 6 + dy, 23 + dx, 21 + dy, skin);
        }

        if (s.Direction == "Up")
        {
            PixelDraw.FillRect(i, 10 + dx, 7 + dy, 19 + dx, 8 + dy, new Rgba32(255, 240, 225, 140));
        }
        else
        {
            PixelDraw.FillRect(i, 11 + dx, 20 + dy, 20 + dx, 21 + dy, shadow);
        }

        return i;
    }

    private PixelBuffer Face(LegacySpriteSpec s, LegacyPose p)
    {
        var i = Blank();
        if (s.Direction == "Up")
        {
            return i;
        }

        int dx = DirectionOffset(s) + p.HeadDx;
        int dy = p.HeadDy + p.BodyDy;

        if (s.Direction is "Left" or "Right")
        {
            int ex = s.Direction == "Left" ? 12 : 18;
            PixelDraw.FillRect(i, ex + dx, 14 + dy, ex + 1 + dx, 16 + dy, LegacyCatalog.Eye);
            PixelDraw.Point(i, ex + dx, 14 + dy, LegacyCatalog.EyeLight);
            if (s.NoseStyle == "Chấm")
            {
                PixelDraw.Point(i, 16 + dx, 17 + dy, LegacyCatalog.Nose);
            }

            if (s.MouthStyle != "Nghiêm")
            {
                PixelDraw.FillRect(i, 15 + dx, 19 + dy, 15 + dx, 19 + dy, LegacyCatalog.Mouth);
            }

            return i;
        }

        switch (s.EyeStyle)
        {
            case "Lạnh lùng":
                PixelDraw.FillRect(i, 11 + dx, 15 + dy, 12 + dx, 16 + dy, LegacyCatalog.Eye);
                PixelDraw.FillRect(i, 18 + dx, 15 + dy, 19 + dx, 16 + dy, LegacyCatalog.Eye);
                break;
            case "Buồn ngủ":
                PixelDraw.FillRect(i, 11 + dx, 15 + dy, 12 + dx, 15 + dy, LegacyCatalog.Eye);
                PixelDraw.FillRect(i, 18 + dx, 15 + dy, 19 + dx, 15 + dy, LegacyCatalog.Eye);
                break;
            default:
                PixelDraw.FillRect(i, 11 + dx, 14 + dy, 12 + dx, 16 + dy, LegacyCatalog.Eye);
                PixelDraw.Point(i, 11 + dx, 14 + dy, LegacyCatalog.EyeLight);
                PixelDraw.FillRect(i, 18 + dx, 14 + dy, 19 + dx, 16 + dy, LegacyCatalog.Eye);
                PixelDraw.Point(i, 18 + dx, 14 + dy, LegacyCatalog.EyeLight);
                break;
        }

        PixelDraw.FillRect(i, 9 + dx, 18 + dy, 10 + dx, 18 + dy, LegacyCatalog.Blush);
        PixelDraw.FillRect(i, 21 + dx, 18 + dy, 22 + dx, 18 + dy, LegacyCatalog.Blush);
        if (s.NoseStyle == "Chấm")
        {
            PixelDraw.Point(i, 16 + dx, 17 + dy, LegacyCatalog.Nose);
        }

        switch (s.MouthStyle)
        {
            case "Cười":
                PixelDraw.Point(i, 15 + dx, 19 + dy, LegacyCatalog.Mouth);
                PixelDraw.Point(i, 16 + dx, 20 + dy, LegacyCatalog.Mouth);
                PixelDraw.Point(i, 17 + dx, 19 + dy, LegacyCatalog.Mouth);
                break;
            case "Nghiêm":
                PixelDraw.FillRect(i, 15 + dx, 19 + dy, 17 + dx, 19 + dy, LegacyCatalog.MouthSerious);
                break;
            default:
                PixelDraw.FillRect(i, 15 + dx, 19 + dy, 16 + dx, 19 + dy, LegacyCatalog.Mouth);
                break;
        }

        return i;
    }

    private PixelBuffer HairBack(LegacySpriteSpec s, LegacyPose p)
    {
        var i = Blank();
        if (s.HairStyle == "Không tóc")
        {
            return i;
        }

        (_, Rgba32 dark, _) = LegacyCatalog.HairPalettes[s.HairColor];
        int sway = p.HairState == "right" ? 1 : p.HairState == "left" ? -1 : 0;
        int bob = p.BodyDy;
        int dx = DirectionOffset(s);
        PixelDraw.FillEllipse(i, 7 + dx, 4 + bob, 24 + dx, 20 + bob, dark);

        string hs = s.HairStyle;
        if (hs is "Tóc dài" or "Tóc xõa" or "Tóc mái dài")
        {
            PixelDraw.FillRect(i, 6 + dx + sway, 12, 8 + dx + sway, 26, dark);
            PixelDraw.FillRect(i, 23 + dx + sway, 12, 25 + dx + sway, 26, dark);
        }
        else if (hs == "Đuôi ngựa")
        {
            int tailX = s.Direction != "Left" ? 24 : 5;
            PixelDraw.FillRect(i, 23 + dx, 11, 25 + dx, 19, dark);
            PixelDraw.FillRect(i, tailX + sway, 18, tailX + 2 + sway, 25, dark);
        }
        else if (hs == "Hai búi")
        {
            PixelDraw.FillEllipse(i, 5 + dx, 5 + bob, 10 + dx, 10 + bob, dark);
            PixelDraw.FillEllipse(i, 21 + dx, 5 + bob, 26 + dx, 10 + bob, dark);
        }
        else if (hs is "Undercut" or "Húi cua" or "Tóc nam ngắn" or "Rẽ ngôi nam" or "Xoăn nam")
        {
            // Short male cuts keep only a compact side/back silhouette instead
            // of the long side locks used by the original feminine styles.
            PixelDraw.FillRect(i, 7 + dx, 10, 9 + dx, 19, dark);
            PixelDraw.FillRect(i, 22 + dx, 10, 24 + dx, 19, dark);
            if (hs is "Húi cua" or "Tóc nam ngắn")
            {
                PixelDraw.FillRect(i, 9 + dx, 8, 22 + dx, 12, dark);
            }
        }
        else
        {
            PixelDraw.FillRect(i, 6 + dx, 12, 8 + dx, 21, dark);
            PixelDraw.FillRect(i, 23 + dx, 12, 25 + dx, 21, dark);
        }

        return i;
    }

    private PixelBuffer HairFront(LegacySpriteSpec s, LegacyPose p)
    {
        var i = Blank();
        if (s.HairStyle == "Không tóc")
        {
            return i;
        }

        (Rgba32 baseColor, _, Rgba32 light) = LegacyCatalog.HairPalettes[s.HairColor];
        int dx = DirectionOffset(s) + p.HeadDx;
        int dy = p.HeadDy + p.BodyDy;
        string hs = s.HairStyle;

        if (hs is "Húi cua" or "Undercut" or "Tóc nam ngắn" or "Rẽ ngôi nam" or "Xoăn nam")
        {
            if (s.Direction == "Up")
            {
                PixelDraw.FillEllipse(i, 8 + dx, 3 + dy, 23 + dx, 11 + dy, baseColor);
                PixelDraw.FillRect(i, 10 + dx, 5 + dy, 20 + dx, 6 + dy, light);
            }
            else if (s.Direction is "Left" or "Right")
            {
                PixelDraw.FillEllipse(i, 8 + dx, 3 + dy, 23 + dx, 11 + dy, baseColor);
                int sideX = s.Direction == "Left" ? 8 : 21;
                PixelDraw.FillRect(i, sideX + dx, 8 + dy, sideX + 2 + dx, 13 + dy, baseColor);
            }
            else
            {
                PixelDraw.FillEllipse(i, 8 + dx, 3 + dy, 23 + dx, 11 + dy, baseColor);
                PixelDraw.FillRect(i, 10 + dx, 7 + dy, 21 + dx, 10 + dy, baseColor);
            }

            switch (hs)
            {
                case "Undercut":
                    PixelDraw.FillRect(i, 8 + dx, 10 + dy, 10 + dx, 13 + dy, baseColor);
                    break;
                case "Rẽ ngôi nam":
                    PixelDraw.FillRect(i, 9 + dx, 8 + dy, 14 + dx, 14 + dy, baseColor);
                    PixelDraw.FillRect(i, 18 + dx, 6 + dy, 21 + dx, 10 + dy, baseColor);
                    break;
                case "Xoăn nam":
                    PixelDraw.FillEllipse(i, 8 + dx, 7 + dy, 12 + dx, 12 + dy, baseColor);
                    PixelDraw.FillEllipse(i, 19 + dx, 7 + dy, 23 + dx, 12 + dy, baseColor);
                    break;
            }

            PixelDraw.FillRect(i, 11 + dx, 5 + dy, 14 + dx, 5 + dy, light);
            return i;
        }

        if (s.Direction == "Up")
        {
            PixelDraw.FillEllipse(i, 7 + dx, 3 + dy, 24 + dx, 14 + dy, baseColor);
            PixelDraw.FillRect(i, 9 + dx, 5 + dy, 20 + dx, 6 + dy, light);
        }
        else if (s.Direction is "Left" or "Right")
        {
            PixelDraw.FillEllipse(i, 7 + dx, 3 + dy, 24 + dx, 13 + dy, baseColor);
            if (s.Direction == "Left")
            {
                PixelDraw.FillRect(i, 8 + dx, 8 + dy, 12 + dx, 15 + dy, baseColor);
                PixelDraw.FillRect(i, 18 + dx, 10 + dy, 19 + dx, 13 + dy, baseColor);
            }
            else
            {
                PixelDraw.FillRect(i, 19 + dx, 8 + dy, 23 + dx, 15 + dy, baseColor);
                PixelDraw.FillRect(i, 12 + dx, 10 + dy, 13 + dx, 13 + dy, baseColor);
            }

            PixelDraw.FillRect(i, 11 + dx, 5 + dy, 14 + dx, 5 + dy, light);
        }
        else
        {
            PixelDraw.FillEllipse(i, 7 + dx, 3 + dy, 24 + dx, 13 + dy, baseColor);
            if (hs == "Tóc lệch")
            {
                PixelDraw.FillRect(i, 8 + dx, 8 + dy, 13 + dx, 15 + dy, baseColor);
                PixelDraw.FillRect(i, 18 + dx, 9 + dy, 21 + dx, 13 + dy, baseColor);
            }
            else if (hs == "Tóc mái dài")
            {
                PixelDraw.FillRect(i, 8 + dx, 8 + dy, 11 + dx, 15 + dy, baseColor);
                PixelDraw.FillRect(i, 19 + dx, 8 + dy, 22 + dx, 15 + dy, baseColor);
            }
            else if (hs == "Tóc dựng")
            {
                PixelDraw.FillRect(i, 10 + dx, 7 + dy, 12 + dx, 12 + dy, baseColor);
                PixelDraw.FillRect(i, 15 + dx, 6 + dy, 17 + dx, 12 + dy, baseColor);
                PixelDraw.FillRect(i, 20 + dx, 8 + dy, 21 + dx, 12 + dy, baseColor);
            }
            else
            {
                PixelDraw.FillRect(i, 8 + dx, 9 + dy, 10 + dx, 15 + dy, baseColor);
                PixelDraw.FillRect(i, 20 + dx, 9 + dy, 22 + dx, 13 + dy, baseColor);
                PixelDraw.FillRect(i, 12 + dx, 10 + dy, 14 + dx, 13 + dy, baseColor);
            }

            PixelDraw.FillRect(i, 11 + dx, 5 + dy, 14 + dx, 5 + dy, light);
            PixelDraw.FillRect(i, 10 + dx, 6 + dy, 11 + dx, 6 + dy, light);
        }

        switch (hs)
        {
            case "Búi cao":
                PixelDraw.FillEllipse(i, 13 + dx, 0 + dy, 18 + dx, 5 + dy, baseColor);
                PixelDraw.FillRect(i, 14 + dx, 5 + dy, 16 + dx, 5 + dy, LegacyCatalog.Gold);
                break;
            case "Đuôi ngựa":
                PixelDraw.FillEllipse(i, 12 + dx, 1 + dy, 19 + dx, 6 + dy, baseColor);
                break;
            case "Hai búi":
                PixelDraw.FillEllipse(i, 7 + dx, 2 + dy, 12 + dx, 7 + dy, baseColor);
                PixelDraw.FillEllipse(i, 20 + dx, 2 + dy, 25 + dx, 7 + dy, baseColor);
                break;
            case "Tóc võ sĩ":
                PixelDraw.FillRect(i, 12 + dx, 1 + dy, 19 + dx, 3 + dy, baseColor);
                break;
        }

        return i;
    }

    private PixelBuffer Arm(LegacySpriteSpec s, LegacyPose p, string side)
    {
        var i = Blank();
        (Rgba32 skin, Rgba32 shadow) = LegacyCatalog.SkinTones[s.SkinTone];
        string st = side == "left" ? p.LeftArm : p.RightArm;
        int bob = p.BodyDy;
        int dx = DirectionOffset(s) + p.BodyDx;

        (int, int)[] pts = (side, st) switch
        {
            ("left", "up") => [(11, 23 + bob), (9, 20 + bob), (7, 17 + bob)],
            ("left", "forward") => [(11, 23 + bob), (9, 25 + bob), (7, 27 + bob)],
            ("left", "back") => [(11, 23 + bob), (10, 27 + bob), (9, 30 + bob)],
            ("left", _) => [(11, 23 + bob), (10, 27 + bob), (9, 31 + bob)],
            ("right", "up") => [(20, 23 + bob), (22, 20 + bob), (24, 17 + bob)],
            ("right", "forward") => [(20, 23 + bob), (22, 25 + bob), (24, 27 + bob)],
            ("right", "back") => [(20, 23 + bob), (21, 27 + bob), (22, 30 + bob)],
            (_, _) => [(20, 23 + bob), (21, 27 + bob), (22, 31 + bob)],
        };

        pts = pts.Select(pt => (pt.Item1 + dx, pt.Item2)).ToArray();
        ((int sx, int sy), (int ex, int ey), (int hx, int hy)) = (pts[0], pts[1], pts[2]);
        Rgba32 armColor = st == "back" ? shadow : skin;
        PixelDraw.FillRect(i, Math.Min(sx, ex), Math.Min(sy, ey), Math.Min(sx, ex) + 2, Math.Min(sy, ey) + 4, armColor);
        PixelDraw.FillRect(i, Math.Min(ex, hx), Math.Min(ey, hy), Math.Min(ex, hx) + 2, Math.Min(ey, hy) + 4, armColor);
        PixelDraw.FillRect(i, hx, hy, hx + 2, hy + 1, skin);
        return i;
    }

    private PixelBuffer Leg(LegacySpriteSpec s, LegacyPose p, string side)
    {
        var i = Blank();
        (Rgba32 skin, Rgba32 shadow) = LegacyCatalog.SkinTones[s.SkinTone];
        int x = side == "left" ? 12 : 18;
        string st = side == "left" ? p.LeftLeg : p.RightLeg;
        int legDx = (st == "back" ? -1 : st == "forward" ? 1 : 0) + DirectionOffset(s) + p.BodyDx;
        int bob = p.BodyDy;
        PixelDraw.FillRect(i, x + legDx, 35 + bob, x + 2 + legDx, 40 + bob, skin);
        PixelDraw.FillRect(i, x - 1 + legDx, 41 + bob, x + 2 + legDx, 42 + bob, shadow);
        return i;
    }

    private List<(string Name, PixelBuffer Image)> EquipmentLayers(LegacySpriteSpec s, LegacyPose p)
    {
        (Rgba32 robe, Rgba32 robeDark, Rgba32 acc, Rgba32 accDark) =
            LegacyCatalog.Palettes.GetValueOrDefault(s.SkinVariant, LegacyCatalog.Palettes["Mặc định"]);
        int bob = p.BodyDy;
        int dx = DirectionOffset(s) + p.BodyDx;
        var layers = new List<(string, PixelBuffer)>();

        void Add(string name, Action<PixelBuffer> build)
        {
            var im = Blank();
            build(im);
            layers.Add((name, im));
        }

        if (s.Equipment["cape"] != "Không")
        {
            Add("cape", im =>
            {
                bool isLong = s.Equipment["cape"].ToLowerInvariant().Contains("dài");
                int width = s.Direction is "Down" or "Up" ? 14 : 12;
                int bottom = isLong ? 15 : 10;
                PixelDraw.FillRect(im, 9 + dx, 23 + bob, 9 + dx + width - 1, 23 + bob + bottom - 1, robeDark);
                if (s.Direction == "Up")
                {
                    PixelDraw.FillRect(im, 8 + dx, 22 + bob, 8 + dx + width + 1, 22 + bob + (isLong ? 16 : 11) - 1, robeDark);
                }
            });
        }

        if (s.Equipment["pants"] != "Không")
        {
            Add("pants", im =>
            {
                Rgba32 c = s.Equipment["pants"] == "Quần sáng" ? robe : robeDark;
                PixelDraw.FillRect(im, 11 + dx, 34 + bob, 14 + dx, 40 + bob, c);
                PixelDraw.FillRect(im, 17 + dx, 34 + bob, 20 + dx, 40 + bob, c);
            });
        }

        if (s.Equipment["boots"] != "Không")
        {
            Add("boots", im =>
            {
                Rgba32 c = s.Equipment["boots"] switch
                {
                    "Ủng giáp" => LegacyCatalog.BootPlate,
                    "Giày da" => LegacyCatalog.BootLeather,
                    _ => LegacyCatalog.BootCloth,
                };
                PixelDraw.FillRect(im, 10 + dx, 39 + bob, 14 + dx, 42 + bob, c);
                PixelDraw.FillRect(im, 17 + dx, 39 + bob, 21 + dx, 42 + bob, c);
            });
        }

        if (s.Equipment["inner"] != "Không")
        {
            Add("inner", im =>
            {
                Rgba32 c = s.Equipment["inner"] == "Áo trong sáng" ? LegacyCatalog.White : LegacyCatalog.Inner;
                PixelDraw.FillRect(im, 11 + dx, 22 + bob, 20 + dx, 31 + bob, c);
            });
        }

        if (s.Equipment["outer"] != "Không")
        {
            Add("outer", im =>
            {
                string kind = s.Equipment["outer"];
                PixelDraw.FillRect(im, 10 + dx, 24 + bob, 21 + dx, 32 + bob, robe);
                PixelDraw.FillRect(im, 10 + dx, 31 + bob, 21 + dx, 36 + bob, robe);
                switch (kind)
                {
                    case "Kiếm tu":
                        PixelDraw.FillRect(im, 10 + dx, 28 + bob, 21 + dx, 29 + bob, acc);
                        PixelDraw.FillRect(im, 13 + dx, 32 + bob, 14 + dx, 36 + bob, robeDark);
                        PixelDraw.FillRect(im, 18 + dx, 32 + bob, 19 + dx, 36 + bob, robeDark);
                        break;
                    case "Đan tu":
                        PixelDraw.FillRect(im, 9 + dx, 31 + bob, 22 + dx, 36 + bob, robe);
                        PixelDraw.FillRect(im, 12 + dx, 25 + bob, 19 + dx, 26 + bob, acc);
                        PixelDraw.FillRect(im, 9 + dx, 24 + bob, 10 + dx, 34 + bob, accDark);
                        break;
                    case "Phù tu":
                        PixelDraw.FillRect(im, 14 + dx, 25 + bob, 16 + dx, 29 + bob, LegacyCatalog.ScrollPaper);
                        PixelDraw.Point(im, 15 + dx, 27 + bob, LegacyCatalog.ScrollMark);
                        PixelDraw.FillRect(im, 11 + dx, 24 + bob, 11 + dx, 35 + bob, accDark);
                        break;
                    case "Thể tu":
                        PixelDraw.FillRect(im, 11 + dx, 24 + bob, 20 + dx, 30 + bob, robeDark);
                        PixelDraw.FillRect(im, 10 + dx, 31 + bob, 21 + dx, 33 + bob, accDark);
                        PixelDraw.FillRect(im, 10 + dx, 24 + bob, 11 + dx, 31 + bob, acc);
                        PixelDraw.FillRect(im, 20 + dx, 24 + bob, 21 + dx, 31 + bob, acc);
                        break;
                    case "Du hiệp":
                        PixelDraw.FillRect(im, 10 + dx, 24 + bob, 21 + dx, 25 + bob, accDark);
                        PixelDraw.FillRect(im, 11 + dx, 30 + bob, 20 + dx, 31 + bob, acc);
                        PixelDraw.FillRect(im, 9 + dx, 28 + bob, 11 + dx, 34 + bob, robeDark);
                        break;
                }
            });
        }

        if (s.Equipment["chest_armor"] != "Không")
        {
            Add("chest_armor", im =>
            {
                Rgba32 c = s.Equipment["chest_armor"].Contains("nhẹ") ? LegacyCatalog.ChestLight : LegacyCatalog.ChestHeavy;
                PixelDraw.FillRect(im, 12 + dx, 24 + bob, 19 + dx, 29 + bob, c);
                PixelDraw.FillRect(im, 13 + dx, 25 + bob, 18 + dx, 27 + bob, LegacyCatalog.ChestPlate);
            });
        }

        if (s.Equipment["shoulder"] != "Không")
        {
            Add("shoulder", im =>
            {
                Rgba32 c = s.Equipment["shoulder"].Contains("nhẹ") ? LegacyCatalog.ShoulderLight : LegacyCatalog.ShoulderHeavy;
                PixelDraw.FillRect(im, 8 + dx, 23 + bob, 12 + dx, 25 + bob, c);
                PixelDraw.FillRect(im, 20 + dx, 23 + bob, 24 + dx, 25 + bob, c);
            });
        }

        if (s.Equipment["gloves"] != "Không")
        {
            Add("gloves", im =>
            {
                Rgba32 c = s.Equipment["gloves"].Contains("vải") ? LegacyCatalog.GlovesCloth : LegacyCatalog.GlovesPlate;
                PixelDraw.FillRect(im, 7 + dx, 30 + bob, 10 + dx, 32 + bob, c);
                PixelDraw.FillRect(im, 22 + dx, 30 + bob, 25 + dx, 32 + bob, c);
            });
        }

        if (s.Equipment["belt"] != "Không")
        {
            Add("belt", im =>
            {
                Rgba32 c = s.Equipment["belt"] == "Đai giáp" ? LegacyCatalog.BeltPlate : acc;
                PixelDraw.FillRect(im, 11 + dx, 29 + bob, 20 + dx, 30 + bob, c);
                PixelDraw.FillRect(im, 15 + dx, 29 + bob, 16 + dx, 30 + bob, LegacyCatalog.Gold);
                if (s.Equipment["belt"] == "Đai ngọc")
                {
                    PixelDraw.FillRect(im, 16 + dx, 31 + bob, 16 + dx, 32 + bob, LegacyCatalog.Jade);
                }
            });
        }

        if (s.Equipment["head"] != "Không")
        {
            Add("head_equipment", im =>
            {
                switch (s.Equipment["head"])
                {
                    case "Băng trán":
                        PixelDraw.FillRect(im, 8 + dx, 10 + bob, 22 + dx, 10 + bob, LegacyCatalog.White);
                        break;
                    case "Mũ vải":
                        PixelDraw.FillRect(im, 9 + dx, 4 + bob, 22 + dx, 7 + bob, robe);
                        break;
                    case "Mũ giáp":
                        PixelDraw.FillRect(im, 9 + dx, 4 + bob, 22 + dx, 7 + bob, LegacyCatalog.HelmetBase);
                        PixelDraw.FillRect(im, 11 + dx, 2 + bob, 20 + dx, 3 + bob, LegacyCatalog.HelmetTop);
                        break;
                }
            });
        }

        if (s.Equipment["off_hand"] != "Không")
        {
            Add("off_hand", im =>
            {
                switch (s.Equipment["off_hand"])
                {
                    case "Khiên":
                        PixelDraw.FillEllipse(im, 23 + dx, 24 + bob, 29 + dx, 32 + bob, LegacyCatalog.Shield);
                        break;
                    case "Phù":
                        PixelDraw.FillRect(im, 24 + dx, 24 + bob, 26 + dx, 28 + bob, LegacyCatalog.ScrollPaper);
                        PixelDraw.Point(im, 25 + dx, 26 + bob, LegacyCatalog.ScrollMark);
                        break;
                    case "Hồ lô":
                        PixelDraw.FillEllipse(im, 23 + dx, 27 + bob, 27 + dx, 31 + bob, LegacyCatalog.Gourd);
                        PixelDraw.FillRect(im, 24 + dx, 25 + bob, 25 + dx, 27 + bob, LegacyCatalog.GourdDark);
                        break;
                }
            });
        }

        if (s.Equipment["accessory"] != "Không")
        {
            Add("accessory", im =>
            {
                switch (s.Equipment["accessory"])
                {
                    case "Ngọc bội":
                        PixelDraw.FillRect(im, 20 + dx, 31 + bob, 21 + dx, 32 + bob, LegacyCatalog.Jade);
                        break;
                    case "Khuyên tai":
                        PixelDraw.Point(im, 8 + dx, 16 + bob, LegacyCatalog.Gold);
                        PixelDraw.Point(im, 23 + dx, 16 + bob, LegacyCatalog.Gold);
                        break;
                    case "Hồ lô":
                        PixelDraw.FillEllipse(im, 23 + dx, 27 + bob, 27 + dx, 31 + bob, LegacyCatalog.Gourd);
                        PixelDraw.FillRect(im, 24 + dx, 25 + bob, 25 + dx, 27 + bob, LegacyCatalog.GourdDark);
                        break;
                }
            });
        }

        foreach ((string slot, PixelBuffer overlay) in _customOverlays)
        {
            var im = overlay.Width == LegacyCatalog.CanvasWidth && overlay.Height == LegacyCatalog.CanvasHeight
                ? overlay
                : PixelOps.NearestResize(overlay, LegacyCatalog.CanvasWidth, LegacyCatalog.CanvasHeight);
            layers.Add(($"custom_{slot}", im));
        }

        return layers;
    }

    private PixelBuffer Weapon(LegacySpriteSpec s, LegacyPose p)
    {
        var i = Blank();
        string w = s.Equipment["main_hand"];
        if (w == "Không")
        {
            return i;
        }

        int dx = DirectionOffset(s) + p.BodyDx;

        if (p.WeaponState == "slash")
        {
            int baseX = 4 + dx;
            for (int n = 0; n < 9; n++)
            {
                PixelDraw.FillRect(i, baseX + n, 9 + n, baseX + n + 1, 10 + n, LegacyCatalog.BladeDark);
                PixelDraw.Point(i, baseX + n, 9 + n, LegacyCatalog.Blade);
            }

            PixelDraw.FillRect(i, 13 + dx, 18, 16 + dx, 19, LegacyCatalog.GoldDark);
            PixelDraw.FillRect(i, 16 + dx, 19, 17 + dx, 23, LegacyCatalog.Brown);
            return i;
        }

        if (p.WeaponState == "raise")
        {
            PixelDraw.FillRect(i, 5 + dx, 6, 6 + dx, 23, LegacyCatalog.BladeDark);
            PixelDraw.FillRect(i, 5 + dx, 6, 5 + dx, 23, LegacyCatalog.Blade);
            PixelDraw.FillRect(i, 4 + dx, 23, 7 + dx, 24, LegacyCatalog.GoldDark);
            PixelDraw.FillRect(i, 5 + dx, 25, 6 + dx, 29, LegacyCatalog.Brown);
            return i;
        }

        switch (w)
        {
            case "Kiếm":
                PixelDraw.FillRect(i, 3 + dx, 10, 4 + dx, 28, LegacyCatalog.BladeDark);
                PixelDraw.FillRect(i, 3 + dx, 10, 3 + dx, 28, LegacyCatalog.Blade);
                PixelDraw.FillRect(i, 1 + dx, 29, 6 + dx, 30, LegacyCatalog.GoldDark);
                PixelDraw.FillRect(i, 3 + dx, 31, 4 + dx, 34, LegacyCatalog.Brown);
                break;
            case "Đao":
                PixelDraw.FillRect(i, 3 + dx, 10, 5 + dx, 26, LegacyCatalog.BladeDark);
                PixelDraw.FillRect(i, 3 + dx, 10, 3 + dx, 26, LegacyCatalog.Blade);
                PixelDraw.FillRect(i, 2 + dx, 27, 6 + dx, 28, LegacyCatalog.GoldDark);
                PixelDraw.FillRect(i, 4 + dx, 29, 5 + dx, 33, LegacyCatalog.Brown);
                break;
            case "Thương":
                PixelDraw.FillRect(i, 3 + dx, 6, 3 + dx, 35, LegacyCatalog.Brown);
                PixelDraw.FillRect(i, 2 + dx, 4, 4 + dx, 6, LegacyCatalog.BladeDark);
                PixelDraw.Point(i, 3 + dx, 3, LegacyCatalog.Blade);
                break;
            case "Quạt":
                PixelDraw.FillPolygon(i, [(3 + dx, 25), (8 + dx, 20), (9 + dx, 27)], LegacyCatalog.White);
                PixelDraw.DrawLine(i, 3 + dx, 25, 9 + dx, 27, LegacyCatalog.GoldDark);
                PixelDraw.DrawLine(i, 3 + dx, 25, 8 + dx, 20, LegacyCatalog.GoldDark);
                break;
            case "Trượng":
                PixelDraw.FillRect(i, 3 + dx, 7, 3 + dx, 35, LegacyCatalog.Brown);
                PixelDraw.FillEllipse(i, 1 + dx, 3, 5 + dx, 7, LegacyCatalog.Gold);
                PixelDraw.FillEllipse(i, 2 + dx, 4, 4 + dx, 6, LegacyCatalog.StaffOrb);
                break;
        }

        return i;
    }

    private PixelBuffer Effects(LegacySpriteSpec s, LegacyPose p)
    {
        var i = Blank();
        Rgba32 c = LegacyCatalog.ElementColors.GetValueOrDefault(s.Element, LegacyCatalog.DefaultEffectColor);
        if (s.Aura != "Không")
        {
            Rgba32 ac = LegacyCatalog.ElementColors.GetValueOrDefault(s.Aura, c);
            foreach ((int x, int y) in new[] { (5, 16), (26, 14), (4, 28), (27, 30), (8, 37), (23, 36) })
            {
                PixelDraw.FillRect(i, x, y, x + 1, y + 1, ac);
            }
        }

        switch (s.Effect)
        {
            case "Glow":
                foreach ((int x, int y) in new[] { (10, 10), (22, 12), (8, 30), (24, 32) })
                {
                    PixelDraw.FillEllipse(i, x, y, x + 2, y + 2, c);
                }

                break;
            case "Spark":
                foreach ((int x, int y) in new[] { (5, 8), (27, 18), (6, 35), (25, 6) })
                {
                    PixelDraw.Point(i, x, y, c);
                    PixelDraw.Point(i, x + 1, y, c);
                    PixelDraw.Point(i, x, y + 1, c);
                }

                break;
            case "Burst":
                foreach ((int x, int y) in new[] { (3, 20), (27, 20), (16, 3), (16, 40) })
                {
                    PixelDraw.FillRect(i, x, y, x + 1, y + 2, c);
                }

                break;
            case "Slash":
                for (int n = 0; n < 7; n++)
                {
                    PixelDraw.Point(i, 19 + n, 14 + n, c);
                }

                break;
            case "Heal":
                PixelDraw.FillRect(i, 15, 12, 16, 19, c);
                PixelDraw.FillRect(i, 12, 15, 19, 16, c);
                foreach ((int x, int y) in new[] { (9, 18), (23, 20), (12, 28), (20, 29) })
                {
                    PixelDraw.FillEllipse(i, x, y, x + 2, y + 2, c);
                }

                break;
            case "Shield":
                PixelDraw.FillEllipse(i, 7, 8, 25, 39, new Rgba32(c.R, c.G, c.B, 80));
                break;
        }

        return i;
    }

    private PixelBuffer AnchorDebug(LegacySpriteSpec s)
    {
        var i = Blank();
        if (_anchorStore is null)
        {
            return i;
        }

        var cols = new Dictionary<string, Rgba32>
        {
            ["head"] = new(255, 80, 80, 255),
            ["chest"] = new(80, 160, 255, 255),
            ["left_hand"] = new(80, 220, 120, 255),
            ["right_hand"] = new(80, 220, 120, 255),
            ["left_foot"] = new(180, 90, 240, 255),
            ["right_foot"] = new(180, 90, 240, 255),
            ["weapon"] = new(255, 255, 255, 255),
            ["accessory"] = new(255, 110, 210, 255),
        };
        foreach ((string name, Rgba32 c) in cols)
        {
            (int x, int y) = _anchorStore.Get(s.Direction, name);
            PixelDraw.FillRect(i, x - 1, y, x + 1, y, c);
            PixelDraw.FillRect(i, x, y - 1, x, y + 1, c);
        }

        return i;
    }

    public List<(string Name, PixelBuffer Image)> RenderLayers(LegacySpriteSpec s, LegacyPose p)
    {
        PixelBuffer weapon = Weapon(s, p);
        PixelBuffer frontWeapon = p.WeaponState is "raise" or "slash" ? weapon : Blank();
        PixelBuffer backWeapon = p.WeaponState is "raise" or "slash" ? Blank() : weapon;
        var layers = new List<(string, PixelBuffer)>
        {
            ("effect_back", Effects(s, p)),
            ("hair_back", HairBack(s, p)),
            ("weapon_back", backWeapon),
            ("left_leg", Leg(s, p, "left")),
            ("right_leg", Leg(s, p, "right")),
            ("body", Body(s, p)),
        };
        layers.AddRange(EquipmentLayers(s, p));
        layers.AddRange(new[]
        {
            ("left_arm", Arm(s, p, "left")),
            ("right_arm", Arm(s, p, "right")),
            ("head", Head(s, p)),
            ("face", Face(s, p)),
            ("hair_front", HairFront(s, p)),
            ("weapon_front", frontWeapon),
        });
        return layers;
    }

    public PixelBuffer Render(LegacySpriteSpec s, LegacyPose p, LegacyRenderMode mode = LegacyRenderMode.Normal)
    {
        var debugColors = new Dictionary<string, Rgba32>
        {
            ["hair_back"] = new(255, 80, 80, 255),
            ["hair_front"] = new(255, 80, 80, 255),
            ["body"] = new(70, 130, 255, 255),
            ["head"] = new(255, 220, 80, 255),
            ["face"] = new(255, 240, 140, 255),
            ["left_arm"] = new(80, 220, 120, 255),
            ["right_arm"] = new(80, 220, 120, 255),
            ["left_leg"] = new(180, 90, 240, 255),
            ["right_leg"] = new(180, 90, 240, 255),
            ["weapon_back"] = new(255, 255, 255, 255),
            ["weapon_front"] = new(255, 255, 255, 255),
        };
        var fallback = new Rgba32(80, 220, 220, 255);

        var output = Blank();
        foreach ((string name, PixelBuffer layer) in RenderLayers(s, p))
        {
            if (mode == LegacyRenderMode.PartDebug)
            {
                PixelOps.Recolor(layer, debugColors.GetValueOrDefault(name, fallback));
            }

            PixelOps.Composite(output, layer);
        }

        if (mode != LegacyRenderMode.PartDebug)
        {
            output = PixelOps.Outline(output, LegacyCatalog.OutlineColor);
        }

        if (mode == LegacyRenderMode.AnchorDebug)
        {
            PixelOps.Composite(output, AnchorDebug(s));
        }

        if (mode == LegacyRenderMode.Silhouette)
        {
            PixelOps.ToSilhouette(output);
        }
        else if (mode == LegacyRenderMode.Grayscale)
        {
            output = PixelOps.ToGrayscale(output);
        }

        return output;
    }
}
