namespace MissionSplat.Rules;

internal readonly record struct CellCoord(int X, int Y);

internal readonly record struct TileCoord(int X, int Y);

// A buried layer keeps the cells it showed at the moment it was covered, not its placement-time orientation:
// a rotate can happen between placement and a later cover, and a reveal must restore what was actually visible.
internal readonly record struct BuriedLayer(Tile Tile, (int X, int Y, Cell Value)[] Cells);

internal sealed class Grid
{
    private readonly Dictionary<CellCoord, Cell> _cells;
    private readonly Dictionary<TileCoord, Tile> _tiles;
    private readonly Dictionary<TileCoord, BuriedLayer[]> _covered;

    private Grid(
        Dictionary<CellCoord, Cell> cells,
        Dictionary<TileCoord, Tile> tiles,
        Dictionary<TileCoord, BuriedLayer[]> covered)
    {
        _cells = cells;
        _tiles = tiles;
        _covered = covered;
    }

    public IReadOnlyDictionary<CellCoord, Cell> Cells => _cells;

    public int TileCount => _tiles.Count;

    public IEnumerable<TileCoord> TilePositions => _tiles.Keys;

    public Tile? TileAt(int tileX, int tileY) =>
        _tiles.TryGetValue(new TileCoord(tileX, tileY), out var tile) ? tile : null;

    public bool HasTile(int tileX, int tileY) => _tiles.ContainsKey(new TileCoord(tileX, tileY));

    public IReadOnlyList<TileId> CoveredTileIds(int tileX, int tileY)
    {
        if (!_covered.TryGetValue(new TileCoord(tileX, tileY), out var layers))
        {
            return [];
        }

        var ids = new TileId[layers.Length];
        for (var i = 0; i < layers.Length; i++)
        {
            ids[i] = layers[i].Tile.Id;
        }

        return ids;
    }

    public Cell? At(int cellX, int cellY) =>
        _cells.TryGetValue(new CellCoord(cellX, cellY), out var cell) ? cell : null;

    public bool SharesFullSide(int tileX, int tileY) =>
        HasTile(tileX + 1, tileY)
        || HasTile(tileX - 1, tileY)
        || HasTile(tileX, tileY + 1)
        || HasTile(tileX, tileY - 1);

    public Grid Place(int tileX, int tileY, Tile tile, IReadOnlyList<(int X, int Y, Cell Value)> located)
    {
        var cells = new Dictionary<CellCoord, Cell>(_cells);
        var tiles = new Dictionary<TileCoord, Tile>(_tiles);
        foreach (var (x, y, value) in located)
        {
            cells.Add(new CellCoord(x, y), value);
        }

        tiles.Add(new TileCoord(tileX, tileY), tile);
        return new Grid(cells, tiles, new Dictionary<TileCoord, BuriedLayer[]>(_covered));
    }

    // Dictionary.Add throws on an occupied cell. Covering replaces the visible cells and keeps every tile underneath.
    // The buried layer snapshots the four cells as they read right before this cover, since both the old and new
    // top tile occupy the same four coordinates regardless of either one's orientation.
    public (Grid Grid, TileId Covered) Cover(int tileX, int tileY, Tile tile, IReadOnlyList<(int X, int Y, Cell Value)> located)
    {
        var coord = new TileCoord(tileX, tileY);
        if (!_tiles.TryGetValue(coord, out var buried))
        {
            throw new InvalidOperationException("Stack covers a tile already on the board.");
        }

        var buriedCells = new (int X, int Y, Cell Value)[located.Count];
        for (var i = 0; i < located.Count; i++)
        {
            var (x, y, _) = located[i];
            buriedCells[i] = (x, y, _cells[new CellCoord(x, y)]);
        }

        var cells = new Dictionary<CellCoord, Cell>(_cells);
        foreach (var (x, y, value) in located)
        {
            cells[new CellCoord(x, y)] = value;
        }

        var tiles = new Dictionary<TileCoord, Tile>(_tiles);
        tiles[coord] = tile;
        var covered = new Dictionary<TileCoord, BuriedLayer[]>(_covered);
        covered[coord] = AppendCovered(coord, new BuriedLayer(buried, buriedCells));
        return (new Grid(cells, tiles, covered), buried.Id);
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
            new Dictionary<TileCoord, Tile>(_tiles),
            new Dictionary<TileCoord, BuriedLayer[]>(_covered));
    }

    // A single-layer position disappears entirely. A stacked position peels only its top tile and restores
    // the layer beneath using the cells it had when it was covered. The caller validates a tile is present.
    public (Grid Grid, Tile Removed, TileId? Revealed) Bounce(int tileX, int tileY)
    {
        var coord = new TileCoord(tileX, tileY);
        if (!_tiles.TryGetValue(coord, out var removed))
        {
            throw new InvalidOperationException("Bounce removes a tile already on the board.");
        }

        var tiles = new Dictionary<TileCoord, Tile>(_tiles);
        var covered = new Dictionary<TileCoord, BuriedLayer[]>(_covered);
        var cells = new Dictionary<CellCoord, Cell>(_cells);

        if (!covered.TryGetValue(coord, out var layers))
        {
            tiles.Remove(coord);
            for (var y = 0; y < 2; y++)
            {
                for (var x = 0; x < 2; x++)
                {
                    cells.Remove(new CellCoord((tileX * 2) + x, (tileY * 2) + y));
                }
            }

            return (new Grid(cells, tiles, covered), removed, null);
        }

        var revealed = layers[^1];
        if (layers.Length == 1)
        {
            covered.Remove(coord);
        }
        else
        {
            covered[coord] = layers[..^1];
        }

        tiles[coord] = revealed.Tile;
        foreach (var (x, y, value) in revealed.Cells)
        {
            cells[new CellCoord(x, y)] = value;
        }

        return (new Grid(cells, tiles, covered), removed, revealed.Tile.Id);
    }

    public static Grid FromStart(Tile tile)
    {
        var located = tile.CellsAt(0, 0, 0);
        return new Grid(
            new Dictionary<CellCoord, Cell>(),
            new Dictionary<TileCoord, Tile>(),
            new Dictionary<TileCoord, BuriedLayer[]>()).Place(0, 0, tile, located);
    }

    private BuriedLayer[] AppendCovered(TileCoord coord, BuriedLayer visible)
    {
        if (!_covered.TryGetValue(coord, out var existing))
        {
            return [visible];
        }

        var appended = new BuriedLayer[existing.Length + 1];
        Array.Copy(existing, appended, existing.Length);
        appended[existing.Length] = visible;
        return appended;
    }
}
