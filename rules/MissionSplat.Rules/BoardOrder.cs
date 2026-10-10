namespace MissionSplat.Rules;

internal static class BoardOrder
{
    // The one listing order for board positions: X, then Y.
    public static List<BoardPosition> Sorted(IEnumerable<BoardPosition> positions)
    {
        var sorted = new List<BoardPosition>(positions);
        sorted.Sort((a, b) => a.TileX != b.TileX ? a.TileX.CompareTo(b.TileX) : a.TileY.CompareTo(b.TileY));
        return sorted;
    }
}
