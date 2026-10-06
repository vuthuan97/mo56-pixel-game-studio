using PixelGameStudio.Core.Primitives;

namespace PixelGameStudio.Rendering;

/// <summary>
/// Integer-only drawing primitives whose pixel results are bit-exact with the
/// Pillow primitives used by the legacy prototype (Pillow 12.3.0 behavior):
/// fills overwrite the destination pixel (including alpha), rectangle and
/// ellipse bounds are inclusive, and polygon filling uses the Pillow scanline
/// algorithm (RGBA path with half-open rounding). This parity is what the
/// golden-master tests pin down; do not "improve" it without regenerating
/// fixtures and updating docs/LEGACY_AUDIT.md.
/// </summary>
public static class PixelDraw
{
    /// <summary>Sets a single pixel if inside the buffer.</summary>
    public static void Point(PixelBuffer buffer, int x, int y, Rgba32 ink)
    {
        if (buffer.Contains(x, y))
        {
            buffer[x, y] = ink;
        }
    }

    /// <summary>Horizontal line, inclusive on both ends, clipped to the buffer.</summary>
    public static void HLine(PixelBuffer buffer, int x0, int y, int x1, Rgba32 ink)
    {
        if (y < 0 || y >= buffer.Height)
        {
            return;
        }

        if (x0 < 0)
        {
            x0 = 0;
        }
        else if (x0 >= buffer.Width)
        {
            return;
        }

        if (x1 < 0)
        {
            return;
        }
        else if (x1 >= buffer.Width)
        {
            x1 = buffer.Width - 1;
        }

        for (int x = x0; x <= x1; x++)
        {
            buffer[x, y] = ink;
        }
    }

    /// <summary>Filled rectangle with an inclusive bounding box (Pillow rectangle fill).</summary>
    public static void FillRect(PixelBuffer buffer, int x0, int y0, int x1, int y1, Rgba32 ink)
    {
        if (y0 > y1)
        {
            (y0, y1) = (y1, y0);
        }

        if (y1 < 0 || y0 >= buffer.Height)
        {
            return;
        }

        int ya = Math.Max(y0, 0);
        int yb = Math.Min(y1, buffer.Height - 1);
        for (int y = ya; y <= yb; y++)
        {
            HLine(buffer, x0, y, x1, ink);
        }
    }

    /// <summary>
    /// Line via Pillow's Bresenham (width 1). Matches PIL ImageDraw.line:
    /// segments exclude the far endpoint, then the final point is drawn.
    /// </summary>
    public static void DrawLine(PixelBuffer buffer, int x0, int y0, int x1, int y1, Rgba32 ink)
    {
        int dx = x1 - x0;
        int xs = 1;
        if (dx < 0)
        {
            dx = -dx;
            xs = -1;
        }

        int dy = y1 - y0;
        int ys = 1;
        if (dy < 0)
        {
            dy = -dy;
            ys = -1;
        }

        if (dx == 0 && dy == 0)
        {
            // Pillow draw_lines: no segment iterations, only the explicit last point.
            Point(buffer, x0, y0, ink);
            return;
        }

        if (dx == 0)
        {
            for (int i = 0; i < dy; i++)
            {
                Point(buffer, x0, y0, ink);
                y0 += ys;
            }
        }
        else if (dy == 0)
        {
            for (int i = 0; i < dx; i++)
            {
                Point(buffer, x0, y0, ink);
                x0 += xs;
            }
        }
        else if (dx > dy)
        {
            int n = dx;
            int dy2 = dy + dy;
            int e = dy2 - dx;
            int dx2 = dx + dx;
            for (int i = 0; i < n; i++)
            {
                Point(buffer, x0, y0, ink);
                if (e >= 0)
                {
                    y0 += ys;
                    e -= dx2;
                }

                e += dy2;
                x0 += xs;
            }
        }
        else
        {
            int n = dy;
            int dx2 = dx + dx;
            int e = dx2 - dy;
            int dy2 = dy + dy;
            for (int i = 0; i < n; i++)
            {
                Point(buffer, x0, y0, ink);
                if (e >= 0)
                {
                    x0 += xs;
                    e -= dy2;
                }

                e += dx2;
                y0 += ys;
            }
        }

        Point(buffer, x1, y1, ink);
    }

    /// <summary>Filled ellipse with an inclusive bounding box (Pillow midpoint ellipse).</summary>
    public static void FillEllipse(PixelBuffer buffer, int x0, int y0, int x1, int y1, Rgba32 ink)
    {
        int a = x1 - x0;
        int b = y1 - y0;
        if (a < 0 || b < 0)
        {
            return;
        }

        int width = a + b; // fill mode
        var state = new EllipseState();
        state.Init(a, b, width);
        while (state.Next(out int sx, out int y, out int ex))
        {
            HLine(buffer, x0 + ((sx + a) / 2), y0 + ((y + b) / 2), x0 + ((ex + a) / 2), ink);
        }
    }

    /// <summary>Filled polygon using Pillow's even-odd scanline algorithm (RGBA path).</summary>
    public static void FillPolygon(PixelBuffer buffer, IReadOnlyList<(int X, int Y)> points, Rgba32 ink)
    {
        int count = points.Count;
        if (count <= 0)
        {
            return;
        }

        // Build edge list exactly like ImagingDrawPolygon (fill path).
        var edges = new List<Edge>(count);
        for (int i = 0; i < count - 1; i++)
        {
            int x0 = points[i].X;
            int y0 = points[i].Y;
            int x1 = points[i + 1].X;
            int y1 = points[i + 1].Y;
            if (y0 == y1 && i != 0 && y0 == points[i - 1].Y)
            {
                // horizontal line immediately following another horizontal line
                Edge last = edges[^1];
                if (x1 > x0 && x0 > points[i - 1].X)
                {
                    last.Xmax = x1;
                    edges[^1] = last;
                    continue;
                }
                else if (x1 < x0 && x0 < points[i - 1].X)
                {
                    last.Xmin = x1;
                    edges[^1] = last;
                    continue;
                }
            }

            edges.Add(AddEdge(x0, y0, x1, y1));
        }

        if (points[^1].X != points[0].X || points[^1].Y != points[0].Y)
        {
            edges.Add(AddEdge(points[^1].X, points[^1].Y, points[0].X, points[0].Y));
        }

        FillPolygonGeneric(buffer, edges, ink);
    }

    private static void FillPolygonGeneric(PixelBuffer buffer, List<Edge> edges, Rgba32 ink)
    {
        int n = edges.Count;
        if (n <= 0)
        {
            return;
        }

        var edgeTable = new List<Edge>(n);
        int ymin = int.MaxValue;
        int ymax = int.MinValue;
        foreach (var e in edges)
        {
            ymin = Math.Min(ymin, e.Ymin);
            ymax = Math.Max(ymax, e.Ymax);
            if (e.Ymin == e.Ymax)
            {
                // horizontal edges are handled by DrawHorizontalLines in the RGBA path
                continue;
            }

            edgeTable.Add(e);
        }

        ymin = Math.Max(ymin, 0);
        ymax = Math.Min(ymax, buffer.Height - 1);

        int edgeCount = edgeTable.Count;
        var xx = new float[Math.Max(edgeCount * 2, 1)];
        for (int y = ymin; y <= ymax; y++)
        {
            int j = 0;
            for (int i = 0; i < edgeCount; i++)
            {
                Edge current = edgeTable[i];
                if (y < current.Ymin || y > current.Ymax)
                {
                    continue;
                }

                xx[j++] = ((y - current.Y0) * current.Dx) + current.X0;

                if (y == current.Ymax && y < ymax)
                {
                    // Needed to draw consistent polygons
                    xx[j] = xx[j - 1];
                    j++;
                }
                else if ((y == current.Ymin || y == current.Ymax) && current.Dx != 0)
                {
                    // Connect discontiguous corners
                    for (int k = 0; k < i; k++)
                    {
                        Edge other = edgeTable[k];
                        if ((y != other.Ymin && y != other.Ymax) || other.Dx == 0)
                        {
                            continue;
                        }

                        // Check if the two edges join to make a corner
                        if (RoundAway(xx[j - 1]) == RoundAway(((y - other.Y0) * other.Dx) + other.X0))
                        {
                            int offset = y == current.Ymax ? -1 : 1;
                            float adjacentLineX = ((y + offset - current.Y0) * current.Dx) + current.X0;
                            if (y + offset >= other.Ymin && y + offset <= other.Ymax)
                            {
                                float adjacentLineXOtherEdge = ((y + offset - other.Y0) * other.Dx) + other.X0;
                                if (xx[j - 1] > adjacentLineX + 1 && xx[j - 1] > adjacentLineXOtherEdge + 1)
                                {
                                    xx[j - 1] = RoundAway(MathF.Max(adjacentLineX, adjacentLineXOtherEdge)) + 1;
                                }
                                else if (xx[j - 1] < adjacentLineX - 1 && xx[j - 1] < adjacentLineXOtherEdge - 1)
                                {
                                    xx[j - 1] = RoundAway(MathF.Min(adjacentLineX, adjacentLineXOtherEdge)) - 1;
                                }

                                break;
                            }
                        }
                    }
                }
            }

            Array.Sort(xx, 0, j);
            FillSpansWithHorizontalEdges(buffer, edges, xx, j, y, ink);
        }
    }

    private static void FillSpansWithHorizontalEdges(
        PixelBuffer buffer,
        List<Edge> edges,
        float[] xx,
        int j,
        int y,
        Rgba32 ink)
    {
        // Pillow RGBA path: x_pos threading with horizontal-edge merging.
        int xPos = j == 0 ? -1 : 0;
        for (int i = 1; i < j; i += 2)
        {
            int xEnd = RoundDown(xx[i]);
            if (xEnd < xPos)
            {
                continue;
            }

            DrawHorizontalLines(buffer, edges, ref xPos, y, ink);
            if (xEnd < xPos)
            {
                continue;
            }

            int xStart = RoundUp(xx[i - 1]);
            if (xPos > xStart)
            {
                xStart = xPos;
                if (xEnd < xStart)
                {
                    continue;
                }
            }

            HLine(buffer, xStart, y, xEnd, ink);
            xPos = xEnd + 1;
        }

        DrawHorizontalLines(buffer, edges, ref xPos, y, ink);
    }

    private static void DrawHorizontalLines(
        PixelBuffer buffer,
        List<Edge> edges,
        ref int xPos,
        int y,
        Rgba32 ink)
    {
        for (int i = 0; i < edges.Count; i++)
        {
            Edge e = edges[i];
            if (e.Ymin == y && e.Ymin == e.Ymax)
            {
                int xmin = e.Xmin;
                if (xPos != -1 && xPos < xmin)
                {
                    continue;
                }

                int xmax = e.Xmax;
                if (xPos > xmin)
                {
                    xmin = xPos;
                    if (xmax < xmin)
                    {
                        continue;
                    }
                }

                HLine(buffer, xmin, e.Ymin, xmax, ink);
                xPos = xmax + 1;
            }
        }
    }

    private static Edge AddEdge(int x0, int y0, int x1, int y1)
    {
        var e = new Edge
        {
            Xmin = Math.Min(x0, x1),
            Xmax = Math.Max(x0, x1),
            Ymin = Math.Min(y0, y1),
            Ymax = Math.Max(y0, y1),
            X0 = x0,
            Y0 = y0,
        };

        if (y0 == y1)
        {
            e.Dx = 0f;
        }
        else
        {
            e.Dx = (float)(x1 - x0) / (y1 - y0);
        }

        return e;
    }

    private struct Edge
    {
        public int Xmin, Ymin, Xmax, Ymax;
        public float Dx;
        public int X0, Y0;
    }

    private static float RoundAway(float f) =>
        MathF.Round(f, MidpointRounding.AwayFromZero);

    private static int RoundUp(float f) =>
        f >= 0f ? (int)MathF.Floor(f + 0.5f) : -(int)MathF.Floor(-f + 0.5f);

    private static int RoundDown(float f) =>
        f >= 0f ? (int)MathF.Ceiling(f - 0.5f) : -(int)MathF.Ceiling(-f - 0.5f);

    /// <summary>
    /// Port of Pillow's midpoint ellipse edge generator (Draw.c quarter_state /
    /// ellipse_state). Produces horizontal segments [x0..x1] at row y in a
    /// coordinate space centered on the bounding box.
    /// </summary>
    private struct EllipseState
    {
        private QuarterState _outer;
        private QuarterState _inner;
        private int _py, _pl, _pr;
        private readonly int[] _cl = new int[4];
        private readonly int[] _cy = new int[4];
        private readonly int[] _cr = new int[4];
        private int _bufcnt;
        private bool _finished;
        private int _leftmost;

        public EllipseState()
        {
            _py = _pl = _pr = 0;
            _bufcnt = 0;
            _finished = false;
            _leftmost = 0;
        }

        public void Init(int a, int b, int w)
        {
            _bufcnt = 0;
            _leftmost = a % 2;
            _outer.Init(a, b);
            bool has = _outer.Next(out _pr, out _py);
            if (w < 1 || !has)
            {
                _finished = true;
            }
            else
            {
                _finished = false;
                _inner.Init(a - (2 * (w - 1)), b - (2 * (w - 1)));
                _pl = _leftmost;
            }
        }

        public bool Next(out int rx0, out int ry, out int rx1)
        {
            if (_bufcnt == 0)
            {
                if (_finished)
                {
                    rx0 = ry = rx1 = 0;
                    return false;
                }

                int y = _py;
                int l = _pl;
                int r = _pr;

                bool has = _outer.Next(out int cx, out int cy);
                while (has && cy <= y)
                {
                    has = _outer.Next(out cx, out cy);
                }

                if (!has)
                {
                    _finished = true;
                }
                else
                {
                    _pr = cx;
                    _py = cy;
                }

                has = _inner.Next(out cx, out cy);
                while (has && cy <= y)
                {
                    l = cx;
                    has = _inner.Next(out cx, out cy);
                }

                _pl = !has ? _leftmost : cx;

                if ((l > 0 || l < r) && y > 0)
                {
                    _cl[_bufcnt] = l == 0 ? 2 : l;
                    _cy[_bufcnt] = y;
                    _cr[_bufcnt] = r;
                    _bufcnt++;
                }

                if (y > 0)
                {
                    _cl[_bufcnt] = -r;
                    _cy[_bufcnt] = y;
                    _cr[_bufcnt] = -l;
                    _bufcnt++;
                }

                if (l > 0 || l < r)
                {
                    _cl[_bufcnt] = l == 0 ? 2 : l;
                    _cy[_bufcnt] = -y;
                    _cr[_bufcnt] = r;
                    _bufcnt++;
                }

                _cl[_bufcnt] = -r;
                _cy[_bufcnt] = -y;
                _cr[_bufcnt] = -l;
                _bufcnt++;
            }

            _bufcnt--;
            rx0 = _cl[_bufcnt];
            ry = _cy[_bufcnt];
            rx1 = _cr[_bufcnt];
            return true;
        }
    }

    private struct QuarterState
    {
        private int _cx, _cy, _ex, _ey;
        private long _a2, _b2, _a2b2;
        private bool _finished;

        public void Init(int a, int b)
        {
            if (a < 0 || b < 0)
            {
                _finished = true;
            }
            else
            {
                _cx = a;
                _cy = b % 2;
                _ex = a % 2;
                _ey = b;
                _a2 = (long)a * a;
                _b2 = (long)b * b;
                _a2b2 = _a2 * _b2;
                _finished = false;
            }
        }

        private long Delta(long x, long y)
        {
            long d = (_a2 * y * y) + (_b2 * x * x) - _a2b2;
            return d < 0 ? -d : d;
        }

        /// <summary>Returns the current point and advances; false when exhausted.</summary>
        public bool Next(out int rx, out int ry)
        {
            rx = _cx;
            ry = _cy;
            if (_finished)
            {
                return false;
            }

            if (_cx == _ex && _cy == _ey)
            {
                _finished = true;
            }
            else
            {
                int nx = _cx;
                int ny = _cy + 2;
                long nd = Delta(nx, ny);
                if (nx > 1)
                {
                    long nd2 = Delta(_cx - 2, _cy + 2);
                    if (nd > nd2)
                    {
                        nx = _cx - 2;
                        ny = _cy + 2;
                        nd = nd2;
                    }

                    nd2 = Delta(_cx - 2, _cy);
                    if (nd > nd2)
                    {
                        nx = _cx - 2;
                        ny = _cy;
                    }
                }

                _cx = nx;
                _cy = ny;
            }

            return true;
        }
    }
}
