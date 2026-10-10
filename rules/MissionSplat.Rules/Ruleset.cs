namespace MissionSplat.Rules;

// The stateless rules engine: given the symbols and patterns in play, it answers every legality and completion
// question from a board, the drawn tile, the uses spent, and a hand. It holds no seat, deck, or turn.
internal sealed class Ruleset
{
    private readonly SymbolId[] _symbols;
    private readonly MissionPattern[] _patterns;

    public Ruleset(IReadOnlyList<SymbolId> symbolsInPlay, IReadOnlyList<MissionPattern> patternsInPlay)
    {
        _symbols = symbolsInPlay.ToArray();
        _patterns = patternsInPlay.ToArray();
    }

    // Distinct in-play powers only: an unlisted power reads as a blank, and two cells of one power are not mixed.
    public bool ShowsMixedPowers(Tile tile)
    {
        var distinct = 0;
        foreach (var power in new[] { OrdinaryCatalog.Rotate, OrdinaryCatalog.Stack, OrdinaryCatalog.Bounce })
        {
            if (IsInPlay(power) && CountSymbol(tile, power) > 0)
            {
                distinct++;
            }
        }

        return distinct > 1;
    }

    // A power symbol the setup does not list is a blank: it grants no use and no on-top placement.
    public int Remaining(Tile drawn, SymbolId power, int spent) =>
        IsInPlay(power) ? CountSymbol(drawn, power) - spent : 0;

    public List<Mission> CompletedMissions(
        IReadOnlyList<Mission> hand,
        IReadOnlyDictionary<CellCoord, Cell> cells,
        HashSet<CellCoord> written)
    {
        var completed = new List<Mission>();
        foreach (var mission in hand)
        {
            if (!PatternIsActive(mission.Pattern))
            {
                continue;
            }

            if (PatternSearch.WasCompletedBy(mission.Pattern, mission.Color, cells, written))
            {
                completed.Add(mission);
            }
        }

        return completed;
    }

    // Placement and stack. The single placement decision, shared by Apply and LegalPlacements so they cannot
    // disagree. Orientation only changes which cells are written, never where the tile may go.
    public Rejection? PlacementRefusal(Grid grid, Tile drawn, int tileX, int tileY)
    {
        if (grid.TileCount == 0)
        {
            return tileX == 0 && tileY == 0
                ? null
                : new Rejection(RejectionReason.NotAtOrigin, "An empty board takes the drawn tile only at the origin.");
        }

        if (grid.HasTile(tileX, tileY))
        {
            return ShowsStack(drawn)
                ? null
                : new Rejection(RejectionReason.CellOccupied, "That position is already occupied.");
        }

        return grid.SharesFullSide(tileX, tileY)
            ? null
            : new Rejection(
                RejectionReason.DoesNotShareFullSide,
                "A tile has to share a full side with a tile already on the board.");
    }

    public PlacementKind KindAt(Grid grid, int tileX, int tileY) =>
        grid.HasTile(tileX, tileY) ? PlacementKind.OnTop : PlacementKind.Beside;

    // Every position the drawn tile could be offered: each occupied one and its four neighbors, or the origin alone.
    public List<BoardPosition> PlacementCandidates(Grid grid)
    {
        var candidates = new HashSet<BoardPosition>();
        if (grid.TileCount == 0)
        {
            candidates.Add(new BoardPosition(0, 0));
        }

        foreach (var coord in grid.TilePositions)
        {
            candidates.Add(new BoardPosition(coord.X, coord.Y));
            candidates.Add(new BoardPosition(coord.X + 1, coord.Y));
            candidates.Add(new BoardPosition(coord.X - 1, coord.Y));
            candidates.Add(new BoardPosition(coord.X, coord.Y + 1));
            candidates.Add(new BoardPosition(coord.X, coord.Y - 1));
        }

        return BoardOrder.Sorted(candidates);
    }

    // The single decision for a power use, shared by Apply and LegalTargets: a charge must remain, then the target
    // must be legal. Each use power answers for itself; only rotate and bounce are uses.
    public Rejection? UseRefusal(Grid grid, Tile drawn, SymbolId power, int spent, int tileX, int tileY)
    {
        if (power.Equals(OrdinaryCatalog.Rotate))
        {
            return RotateRefusal(grid, drawn, spent, tileX, tileY);
        }

        if (power.Equals(OrdinaryCatalog.Bounce))
        {
            return BounceRefusal(grid, drawn, spent, tileX, tileY);
        }

        throw new ArgumentOutOfRangeException(nameof(power), power, "Only rotate and bounce are uses.");
    }

    private Rejection? RotateRefusal(Grid grid, Tile drawn, int spent, int tileX, int tileY)
    {
        if (Remaining(drawn, OrdinaryCatalog.Rotate, spent) == 0)
        {
            return new Rejection(RejectionReason.NoUseRemaining, "The drawn tile has no unused rotate cell.");
        }

        if (!grid.HasTile(tileX, tileY))
        {
            return new Rejection(RejectionReason.NoTileToRotate, "There is no tile at that position to rotate.");
        }

        return IsSurrounded(grid, tileX, tileY)
            ? new Rejection(RejectionReason.TileSurrounded, "That tile is completely surrounded.")
            : null;
    }

    private Rejection? BounceRefusal(Grid grid, Tile drawn, int spent, int tileX, int tileY)
    {
        if (Remaining(drawn, OrdinaryCatalog.Bounce, spent) == 0)
        {
            return new Rejection(RejectionReason.NoUseRemaining, "The drawn tile has no unused bounce cell.");
        }

        return grid.HasTile(tileX, tileY)
            ? null
            : new Rejection(RejectionReason.NoTileToBounce, "There is no tile at that position to bounce.");
    }

    // Completely surrounded counts only tiles on the board; a stacked position is one occupied neighbor.
    private static bool IsSurrounded(Grid grid, int tileX, int tileY) =>
        grid.HasTile(tileX + 1, tileY)
        && grid.HasTile(tileX - 1, tileY)
        && grid.HasTile(tileX, tileY + 1)
        && grid.HasTile(tileX, tileY - 1);

    private bool ShowsStack(Tile tile) => IsInPlay(OrdinaryCatalog.Stack) && CountSymbol(tile, OrdinaryCatalog.Stack) > 0;

    private bool PatternIsActive(MissionPattern pattern) => Array.IndexOf(_patterns, pattern) >= 0;

    private bool IsInPlay(SymbolId symbol) => Array.IndexOf(_symbols, symbol) >= 0;

    private static int CountSymbol(Tile tile, SymbolId symbol)
    {
        var count = 0;
        for (var y = 0; y < 2; y++)
        {
            for (var x = 0; x < 2; x++)
            {
                if (tile.Local(x, y).TryGetSymbol(out var found) && found.Equals(symbol))
                {
                    count++;
                }
            }
        }

        return count;
    }
}
