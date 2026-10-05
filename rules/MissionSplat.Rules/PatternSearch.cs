namespace MissionSplat.Rules;

internal static class PatternSearch
{
    private static readonly (int X, int Y)[][] Rows =
    [
        [(0, 0), (1, 0), (2, 0), (3, 0)],
        [(0, 0), (0, 1), (0, 2), (0, 3)],
        [(0, 0), (1, 1), (2, 2), (3, 3)],
        [(0, 0), (1, -1), (2, -2), (3, -3)],
    ];

    private static readonly (int X, int Y)[][] Squares =
    [
        [(0, 0), (1, 0), (0, 1), (1, 1)],
    ];

    // Length-3 arm with a foot on one end. Eight rotations and reflections.
    // A T, a skew, and a square are not here.
    private static readonly (int X, int Y)[][] Ls =
    [
        [(0, 0), (0, 1), (0, 2), (1, 0)],
        [(0, 0), (0, 1), (1, 1), (2, 1)],
        [(0, 2), (1, 0), (1, 1), (1, 2)],
        [(0, 0), (1, 0), (2, 0), (2, 1)],
        [(0, 0), (1, 0), (1, 1), (1, 2)],
        [(0, 0), (0, 1), (1, 0), (2, 0)],
        [(0, 0), (0, 1), (0, 2), (1, 2)],
        [(0, 1), (1, 1), (2, 0), (2, 1)],
    ];

    public static bool WasCompletedBy(
        MissionPattern pattern,
        ColorId color,
        IReadOnlyDictionary<CellCoord, Cell> cells,
        HashSet<CellCoord> written)
    {
        foreach (var shape in Shapes(pattern))
        {
            foreach (var origin in cells.Keys)
            {
                foreach (var pin in shape)
                {
                    if (Matches(origin.X - pin.X, origin.Y - pin.Y, shape, color, cells, written))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static (int X, int Y)[][] Shapes(MissionPattern pattern) => pattern switch
    {
        MissionPattern.Row => Rows,
        MissionPattern.Square => Squares,
        MissionPattern.L => Ls,
        _ => throw new ArgumentOutOfRangeException(nameof(pattern), pattern, "That pattern has no matcher."),
    };

    private static bool Matches(
        int originX,
        int originY,
        (int X, int Y)[] shape,
        ColorId color,
        IReadOnlyDictionary<CellCoord, Cell> cells,
        HashSet<CellCoord> written)
    {
        var touchesPlacement = false;
        foreach (var (x, y) in shape)
        {
            var coord = new CellCoord(originX + x, originY + y);
            if (!cells.TryGetValue(coord, out var cell) || !cell.CountsAs(color))
            {
                return false;
            }

            if (written.Contains(coord))
            {
                touchesPlacement = true;
            }
        }

        return touchesPlacement;
    }
}
