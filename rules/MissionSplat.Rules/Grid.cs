namespace MissionSplat.Rules;

internal readonly record struct CellCoord(int X, int Y);

internal readonly record struct TileCoord(int X, int Y);

internal sealed class Grid
{
    private readonly Dictionary<CellCoord, Cell> _cells;
    private readonly Dictionary<TileCoord, TileId> _tiles;
    private readonly Dictionary<TileCoord, TileId[]> _covered;

    private Grid(
        Dictionary<CellCoord, Cell> cells,
        Dictionary<TileCoord, TileId> tiles,
        Dictionary<TileCoord, TileId[]> covered)
    {
        _cells = cells;
        _tiles = tiles;
        _covered = covered;
    }

    public IReadOnlyDictionary<CellCoord, Cell> Cells => _cells;

    public int TileCount => _tiles.Count;

    public bool HasTile(int tileX, int tileY) => _tiles.ContainsKey(new TileCoord(tileX, tileY));

    public IReadOnlyList<TileId> CoveredTileIds(int tileX, int tileY)
    {
        if (!_covered.TryGetValue(new TileCoord(tileX, tileY), out var ids))
        {
            return [];
        }

        var copy = new TileId[ids.Length];
        Array.Copy(ids, copy, ids.Length);
        return copy;
    }

    public Cell? At(int cellX, int cellY) =>
        _cells.TryGetValue(new CellCoord(cellX, cellY), out var cell) ? cell : null;

    public bool SharesFullSide(int tileX, int tileY) =>
        HasTile(tileX + 1, tileY)
        || HasTile(tileX - 1, tileY)
        || HasTile(tileX, tileY + 1)
        || HasTile(tileX, tileY - 1);

    public bool Overlaps(IReadOnlyList<(int X, int Y, Cell Value)> located)
    {
        foreach (var (x, y, _) in located)
        {
            if (_cells.ContainsKey(new CellCoord(x, y)))
            {
                return true;
            }
        }

        return false;
    }

    public Grid Place(int tileX, int tileY, TileId id, IReadOnlyList<(int X, int Y, Cell Value)> located)
    {
        var cells = new Dictionary<CellCoord, Cell>(_cells);
        var tiles = new Dictionary<TileCoord, TileId>(_tiles);
        foreach (var (x, y, value) in located)
        {
            cells.Add(new CellCoord(x, y), value);
        }

        tiles.Add(new TileCoord(tileX, tileY), id);
        return new Grid(cells, tiles, new Dictionary<TileCoord, TileId[]>(_covered));
    }

    // Dictionary.Add throws on an occupied cell. Covering replaces the visible cells and keeps every tile underneath.
    public Grid Cover(int tileX, int tileY, TileId id, IReadOnlyList<(int X, int Y, Cell Value)> located)
    {
        var coord = new TileCoord(tileX, tileY);
        if (!_tiles.TryGetValue(coord, out var buried))
        {
            throw new InvalidOperationException("Stack covers a tile already on the board.");
        }

        var cells = new Dictionary<CellCoord, Cell>(_cells);
        foreach (var (x, y, value) in located)
        {
            cells[new CellCoord(x, y)] = value;
        }

        var tiles = new Dictionary<TileCoord, TileId>(_tiles);
        tiles[coord] = id;
        var covered = new Dictionary<TileCoord, TileId[]>(_covered);
        covered[coord] = AppendCovered(coord, buried);
        return new Grid(cells, tiles, covered);
    }

    // Occupied cells cannot be Add-ed. Replacing the four visible values leaves buried tiles under this position.
    public Grid TurnClockwise(int tileX, int tileY, int quarterTurnsClockwise)
    {
        if (!_tiles.ContainsKey(new TileCoord(tileX, tileY)))
        {
            throw new InvalidOperationException("Rotate turns a tile already on the board.");
        }

        var moved = new Cell[4];
        for (var y = 0; y < 2; y++)
        {
            for (var x = 0; x < 2; x++)
            {
                if (!_cells.TryGetValue(new CellCoord((tileX * 2) + x, (tileY * 2) + y), out var value))
                {
                    throw new InvalidOperationException("A board tile is missing one of its cells.");
                }

                moved[x + (2 * y)] = value;
            }
        }

        var cells = new Dictionary<CellCoord, Cell>(_cells);
        for (var y = 0; y < 2; y++)
        {
            for (var x = 0; x < 2; x++)
            {
                var (turnedX, turnedY) = Tile.TurnClockwise(x, y, quarterTurnsClockwise);
                cells[new CellCoord((tileX * 2) + turnedX, (tileY * 2) + turnedY)] = moved[x + (2 * y)];
            }
        }

        return new Grid(
            cells,
            new Dictionary<TileCoord, TileId>(_tiles),
            new Dictionary<TileCoord, TileId[]>(_covered));
    }

    public static Grid FromStart(Tile tile)
    {
        var located = tile.CellsAt(0, 0, 0);
        return new Grid(
            new Dictionary<CellCoord, Cell>(),
            new Dictionary<TileCoord, TileId>(),
            new Dictionary<TileCoord, TileId[]>()).Place(0, 0, tile.Id, located);
    }

    private TileId[] AppendCovered(TileCoord coord, TileId visible)
    {
        if (!_covered.TryGetValue(coord, out var existing))
        {
            return [visible];
        }

        var appended = new TileId[existing.Length + 1];
        Array.Copy(existing, appended, existing.Length);
        appended[existing.Length] = visible;
        return appended;
    }
}
