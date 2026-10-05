namespace MissionSplat.Rules;

public sealed class Tile
{
    private readonly Cell[] _cells;

    public Tile(TileId id, Cell x0y0, Cell x1y0, Cell x0y1, Cell x1y1)
    {
        Id = id;
        _cells = [x0y0, x1y0, x0y1, x1y1];
    }

    public TileId Id { get; }

    internal Cell Local(int x, int y) => _cells[x + (2 * y)];

    internal (int X, int Y, Cell Value)[] CellsAt(int tileX, int tileY, int quarterTurnsClockwise)
    {
        var located = new (int X, int Y, Cell Value)[4];
        var index = 0;
        for (var y = 0; y < 2; y++)
        {
            for (var x = 0; x < 2; x++)
            {
                var (turnedX, turnedY) = TurnClockwise(x, y, quarterTurnsClockwise);
                located[index++] = (tileX * 2 + turnedX, tileY * 2 + turnedY, Local(x, y));
            }
        }

        return located;
    }

    // Y increases upward. One clockwise quarter-turn sends (x, y) to (y, 1 - x).
    private static (int X, int Y) TurnClockwise(int x, int y, int quarterTurnsClockwise) =>
        quarterTurnsClockwise switch
        {
            0 => (x, y),
            1 => (y, 1 - x),
            2 => (1 - x, 1 - y),
            3 => (1 - y, x),
            _ => throw new ArgumentOutOfRangeException(
                nameof(quarterTurnsClockwise),
                quarterTurnsClockwise,
                "Quarter-turns are 0, 1, 2, or 3."),
        };
}
