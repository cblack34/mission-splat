namespace MissionSplat.Rules;

internal readonly record struct CellCoord(int X, int Y);

internal readonly record struct TileCoord(int X, int Y);

internal sealed class Grid
{
    private readonly Dictionary<CellCoord, Cell> _cells;
    private readonly Dictionary<TileCoord, TileId> _tiles;

    private Grid(Dictionary<CellCoord, Cell> cells, Dictionary<TileCoord, TileId> tiles)
    {
        _cells = cells;
        _tiles = tiles;
    }

    public IReadOnlyDictionary<CellCoord, Cell> Cells => _cells;

    public int TileCount => _tiles.Count;

    public bool HasTile(int tileX, int tileY) => _tiles.ContainsKey(new TileCoord(tileX, tileY));

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
        return new Grid(cells, tiles);
    }

    public static Grid FromStart(Tile tile)
    {
        var located = tile.CellsAt(0, 0, 0);
        return new Grid(new Dictionary<CellCoord, Cell>(), new Dictionary<TileCoord, TileId>())
            .Place(0, 0, tile.Id, located);
    }
}
