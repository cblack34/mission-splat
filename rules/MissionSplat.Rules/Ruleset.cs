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
        foreach (var symbol in _symbols)
        {
            if (OrdinaryCatalog.IsPower(symbol) && CountSymbol(tile, symbol) > 0)
            {
                distinct++;
            }
        }

        return distinct > 1;
    }

    // A power symbol the setup does not list is a blank: it grants no use and no on-top placement.
    public int Remaining(Tile drawn, SymbolId power, SpentUses spent) =>
        IsInPlay(power) ? CountSymbol(drawn, power) - spent.Of(power) : 0;

    // A mission completes only when a cell this placement wrote is part of its pattern.
    public List<Mission> CompletedMissions(
        Grid grid,
        IReadOnlyList<(int X, int Y, Cell Value)> located,
        IReadOnlyList<Mission> hand)
    {
        var written = new HashSet<CellCoord>(located.Count);
        foreach (var (x, y, _) in located)
        {
            written.Add(new CellCoord(x, y));
        }

        var completed = new List<Mission>();
        foreach (var mission in hand)
        {
            if (!PatternIsActive(mission.Pattern))
            {
                continue;
            }

            if (PatternSearch.WasCompletedBy(mission.Pattern, mission.Color, grid.Cells, written))
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

    // The board after a placement the refusal already passed; stacking on an occupied position buries its top tile.
    public (Grid Grid, IReadOnlyList<(int X, int Y, Cell Value)> Located, TileId? Covered) Place(
        Grid grid,
        Tile drawn,
        Place action)
    {
        var located = drawn.CellsAt(action.TileX, action.TileY, action.QuarterTurnsClockwise);
        if (KindAt(grid, action.TileX, action.TileY) == PlacementKind.OnTop)
        {
            var (stacked, buried) = grid.Cover(action.TileX, action.TileY, drawn, located);
            return (stacked, located, buried);
        }

        return (grid.Place(action.TileX, action.TileY, drawn, located), located, null);
    }

    public PlacementKind KindAt(Grid grid, int tileX, int tileY) =>
        grid.HasTile(tileX, tileY) ? PlacementKind.OnTop : PlacementKind.Beside;

    // Every position the drawn tile may take, X then Y, each tagged as beside or on top.
    public IReadOnlyList<LegalPlacement> LegalPlacements(Grid grid, Tile drawn)
    {
        var legal = new List<LegalPlacement>();
        foreach (var position in PlacementCandidates(grid))
        {
            if (PlacementRefusal(grid, drawn, position.TileX, position.TileY) is null)
            {
                legal.Add(new LegalPlacement(position.TileX, position.TileY, KindAt(grid, position.TileX, position.TileY)));
            }
        }

        return legal;
    }

    // Every board tile the power may target, X then Y.
    public IReadOnlyList<BoardPosition> LegalTargets(Grid grid, Tile drawn, SymbolId power, SpentUses spent)
    {
        var targets = new List<BoardPosition>();
        foreach (var position in BoardOrder.SortedPositions(grid.TilePositions))
        {
            if (UseRefusal(grid, drawn, power, spent, position.TileX, position.TileY) is null)
            {
                targets.Add(position);
            }
        }

        return targets;
    }

    // Every position the drawn tile could be offered: each occupied one and its four neighbors, or the origin alone.
    private static List<BoardPosition> PlacementCandidates(Grid grid)
    {
        var candidates = new HashSet<BoardPosition>();
        if (grid.TileCount == 0)
        {
            candidates.Add(new BoardPosition(0, 0));
        }

        foreach (var position in BoardOrder.SortedPositions(grid.TilePositions))
        {
            candidates.Add(position);
            candidates.Add(new BoardPosition(position.TileX + 1, position.TileY));
            candidates.Add(new BoardPosition(position.TileX - 1, position.TileY));
            candidates.Add(new BoardPosition(position.TileX, position.TileY + 1));
            candidates.Add(new BoardPosition(position.TileX, position.TileY - 1));
        }

        return BoardOrder.Sorted(candidates);
    }

    // The single decision for a power use, shared by Apply and LegalTargets: a charge must remain, then the target
    // must be legal. Each use power answers for itself; only rotate and bounce are uses.
    public Rejection? UseRefusal(Grid grid, Tile drawn, SymbolId power, SpentUses spent, int tileX, int tileY)
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

    private Rejection? RotateRefusal(Grid grid, Tile drawn, SpentUses spent, int tileX, int tileY)
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

    // The board after a rotate the refusal already passed.
    public Grid Rotate(Grid grid, UseRotate use) =>
        grid.TurnClockwise(use.TileX, use.TileY, use.QuarterTurnsClockwise);

    private Rejection? BounceRefusal(Grid grid, Tile drawn, SpentUses spent, int tileX, int tileY)
    {
        if (Remaining(drawn, OrdinaryCatalog.Bounce, spent) == 0)
        {
            return new Rejection(RejectionReason.NoUseRemaining, "The drawn tile has no unused bounce cell.");
        }

        return grid.HasTile(tileX, tileY)
            ? null
            : new Rejection(RejectionReason.NoTileToBounce, "There is no tile at that position to bounce.");
    }

    // The board after a bounce the refusal already passed, with the tile it removed and any tile it revealed.
    public (Grid Grid, Tile Removed, TileId? Revealed) Bounce(Grid grid, UseBounce use) =>
        grid.Bounce(use.TileX, use.TileY);

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
