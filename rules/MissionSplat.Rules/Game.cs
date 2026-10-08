namespace MissionSplat.Rules;

public sealed class Game
{
    private readonly SeatSnapshot[] _seats;
    private readonly int _currentIndex;
    private readonly Mission[] _missionDeck;
    private readonly Tile[] _matchDeck;
    private readonly Grid _grid;
    private readonly MissionPattern[] _patterns;
    private readonly bool _hasEnded;

    private Game(
        SeatSnapshot[] seats,
        int currentIndex,
        Mission[] missionDeck,
        Tile[] matchDeck,
        Grid grid,
        MissionPattern[] patterns,
        int claimsRequiredToWin,
        bool hasEnded)
    {
        _seats = seats;
        _currentIndex = currentIndex;
        _missionDeck = missionDeck;
        _matchDeck = matchDeck;
        _grid = grid;
        _patterns = patterns;
        ClaimsRequiredToWin = claimsRequiredToWin;
        _hasEnded = hasEnded;
    }

    public SeatId CurrentSeat => _seats[_currentIndex].Id;

    public bool HasEnded => _hasEnded;

    public int ClaimsRequiredToWin { get; }

    public int TileCount => _grid.TileCount;

    public int MissionDeckRemaining => _missionDeck.Length;

    public int MatchDeckRemaining => _matchDeck.Length;

    // The tile the next Place, PlaceWithRotates, Stack, or PlaceWithBounces will consume. Null when the game has
    // ended or none remains; any of those commands still refuses that empty deck.
    public Tile? PendingMatchTile => _hasEnded || _matchDeck.Length == 0 ? null : _matchDeck[0];

    public IReadOnlyList<SeatId> SeatsInTurnOrder
    {
        get
        {
            var seats = new SeatId[_seats.Length];
            for (var i = 0; i < _seats.Length; i++)
            {
                seats[i] = _seats[i].Id;
            }

            return seats;
        }
    }

    public static CommandResult Start(GameSetup setup)
    {
        if (setup is null)
        {
            throw new ArgumentNullException(nameof(setup));
        }

        var rejection = SetupCheck.Find(setup);
        if (rejection is not null)
        {
            return CommandResult.Reject(null, rejection);
        }

        return CommandResult.Accept(Deal(setup), []);
    }

    // The tile covers the 2×2 cells at (2*tileX + lx, 2*tileY + ly), y upward.
    // Quarter-turns are clockwise and only orient this placement. They are not the rotate power.
    public CommandResult Place(int tileX, int tileY, int quarterTurnsClockwise)
    {
        var unplayable = RejectBeforeConsumingMatchTile(quarterTurnsClockwise);
        if (unplayable is not null)
        {
            return unplayable;
        }

        var tile = _matchDeck[0];
        var located = tile.CellsAt(tileX, tileY, quarterTurnsClockwise);
        var blocked = RejectBlockedSide(tileX, tileY, located);
        if (blocked is not null)
        {
            return blocked;
        }

        return ResolvePlacement(
            tile,
            tileX,
            tileY,
            quarterTurnsClockwise,
            _grid.Place(tileX, tileY, tile, located),
            located);
    }

    // Place still passes the turn without turning a tile.
    // Drawn rotate cells are spent here, after the tile is down and before the claim.
    public CommandResult PlaceWithRotates(
        int tileX,
        int tileY,
        int quarterTurnsClockwise,
        IReadOnlyList<RotateUse> rotates)
    {
        if (rotates is null)
        {
            throw new ArgumentNullException(nameof(rotates));
        }

        var unplayable = RejectBeforeConsumingMatchTile(quarterTurnsClockwise);
        if (unplayable is not null)
        {
            return unplayable;
        }

        var tile = _matchDeck[0];
        var blockedUses = RejectRotateUses(tile, rotates);
        if (blockedUses is not null)
        {
            return blockedUses;
        }

        var located = tile.CellsAt(tileX, tileY, quarterTurnsClockwise);
        var blocked = RejectBlockedSide(tileX, tileY, located);
        if (blocked is not null)
        {
            return blocked;
        }

        var blockedTarget = RejectRotateTargets(tileX, tileY, rotates);
        if (blockedTarget is not null)
        {
            return blockedTarget;
        }

        var grid = _grid.Place(tileX, tileY, tile, located);
        foreach (var use in rotates)
        {
            grid = grid.TurnClockwise(use.TileX, use.TileY, use.QuarterTurnsClockwise);
        }

        return ResolvePlacement(tile, tileX, tileY, quarterTurnsClockwise, grid, located);
    }

    // The tile just placed is never a legal bounce target, even though this command never stacks.
    // Drawn bounce cells are spent here, after the tile is down and before the claim.
    public CommandResult PlaceWithBounces(
        int tileX,
        int tileY,
        int quarterTurnsClockwise,
        IReadOnlyList<BounceUse> bounces)
    {
        if (bounces is null)
        {
            throw new ArgumentNullException(nameof(bounces));
        }

        var unplayable = RejectBeforeConsumingMatchTile(quarterTurnsClockwise);
        if (unplayable is not null)
        {
            return unplayable;
        }

        var tile = _matchDeck[0];
        var blockedUses = RejectBounceUses(tile, bounces);
        if (blockedUses is not null)
        {
            return blockedUses;
        }

        var located = tile.CellsAt(tileX, tileY, quarterTurnsClockwise);
        var blocked = RejectBlockedSide(tileX, tileY, located);
        if (blocked is not null)
        {
            return blocked;
        }

        var blockedTarget = RejectBounceTargets(tileX, tileY, bounces);
        if (blockedTarget is not null)
        {
            return blockedTarget;
        }

        var grid = _grid.Place(tileX, tileY, tile, located);
        var returned = new Tile[bounces.Count];
        for (var i = 0; i < bounces.Count; i++)
        {
            var (nextGrid, removed) = grid.Bounce(bounces[i].TileX, bounces[i].TileY);
            grid = nextGrid;
            returned[i] = removed;
        }

        return ResolvePlacement(tile, tileX, tileY, quarterTurnsClockwise, grid, located, returned);
    }

    // One or more stack cells allow this once. Place still refuses the same occupied cell.
    public CommandResult Stack(int tileX, int tileY, int quarterTurnsClockwise)
    {
        var unplayable = RejectBeforeConsumingMatchTile(quarterTurnsClockwise);
        if (unplayable is not null)
        {
            return unplayable;
        }

        var tile = _matchDeck[0];
        if (!ShowsStack(tile))
        {
            return Reject(RejectionReason.NoStackCell, "The drawn tile has no stack cell.");
        }

        if (!_grid.HasTile(tileX, tileY))
        {
            return Reject(RejectionReason.NoTileToCover, "There is no tile at that position to cover.");
        }

        var located = tile.CellsAt(tileX, tileY, quarterTurnsClockwise);
        return ResolvePlacement(
            tile,
            tileX,
            tileY,
            quarterTurnsClockwise,
            _grid.Cover(tileX, tileY, tile, located),
            located);
    }

    public IReadOnlyList<Mission> Hand(SeatId seat) => Copy(Find(seat).Hand);

    public IReadOnlyList<Mission> Claims(SeatId seat) => Copy(Find(seat).Claims);

    public Cell? CellAt(int cellX, int cellY) => _grid.At(cellX, cellY);

    public bool HasTileAt(int tileX, int tileY) => _grid.HasTile(tileX, tileY);

    public IReadOnlyList<TileId> CoveredTileIds(int tileX, int tileY) => _grid.CoveredTileIds(tileX, tileY);

    private List<Mission> CompletedMissions(
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

    private bool PatternIsActive(MissionPattern pattern)
    {
        foreach (var active in _patterns)
        {
            if (active == pattern)
            {
                return true;
            }
        }

        return false;
    }

    // Place, PlaceWithRotates, Stack, and PlaceWithBounces consume the front match tile. An empty deck is the same open ruling for each.
    private CommandResult? RejectBeforeConsumingMatchTile(int quarterTurnsClockwise)
    {
        if (_hasEnded)
        {
            return Reject(RejectionReason.GameOver, "The game has already been won.");
        }

        if (quarterTurnsClockwise is < 0 or > 3)
        {
            return Reject(RejectionReason.InvalidQuarterTurns, "Quarter-turns are 0, 1, 2, or 3.");
        }

        if (_matchDeck.Length == 0)
        {
            throw new UnresolvedRulingException(
                "The match deck has no tile to place. Exhausting the match deck is an open ruling, so this command was not applied.");
        }

        return null;
    }

    private CommandResult ResolvePlacement(
        Tile tile,
        int tileX,
        int tileY,
        int quarterTurnsClockwise,
        Grid nextGrid,
        IReadOnlyList<(int X, int Y, Cell Value)> located,
        IReadOnlyList<Tile>? returnedToMatchDeck = null)
    {
        var written = new HashSet<CellCoord>(located.Count);
        foreach (var (x, y, _) in located)
        {
            written.Add(new CellCoord(x, y));
        }

        var acting = _seats[_currentIndex];
        var completed = CompletedMissions(acting.Hand, nextGrid.Cells, written);
        if (completed.Count > _missionDeck.Length)
        {
            throw new UnresolvedRulingException(
                "The mission deck cannot replace every mission this placement completed. Exhausting the mission deck is an open ruling, so this command was not applied.");
        }

        var hand = new List<Mission>(acting.Hand);
        var claims = new List<Mission>(acting.Claims);
        var missionDeck = new List<Mission>(_missionDeck);
        var events = new List<GameEvent>
        {
            new TilePlaced(acting.Id, tile.Id, tileX, tileY, quarterTurnsClockwise),
        };

        foreach (var mission in completed)
        {
            hand.Remove(mission);
            var replacement = missionDeck[0];
            missionDeck.RemoveAt(0);
            hand.Add(replacement);
            claims.Add(mission);
            events.Add(new MissionClaimed(acting.Id, mission.Id));
        }

        var won = claims.Count >= ClaimsRequiredToWin;
        if (won)
        {
            events.Add(new GameWon(acting.Id, claims.Count));
        }

        var matchDeck = _matchDeck[1..];
        if (returnedToMatchDeck is { Count: > 0 })
        {
            var extended = new Tile[matchDeck.Length + returnedToMatchDeck.Count];
            Array.Copy(matchDeck, extended, matchDeck.Length);
            for (var i = 0; i < returnedToMatchDeck.Count; i++)
            {
                extended[matchDeck.Length + i] = returnedToMatchDeck[i];
            }

            matchDeck = extended;
        }

        var seats = (SeatSnapshot[])_seats.Clone();
        seats[_currentIndex] = new SeatSnapshot(acting.Id, hand.ToArray(), claims.ToArray());
        var next = new Game(
            seats,
            (_currentIndex + 1) % seats.Length,
            missionDeck.ToArray(),
            matchDeck,
            nextGrid,
            _patterns,
            ClaimsRequiredToWin,
            won);

        return CommandResult.Accept(next, events);
    }

    // Covering a tile is Stack. Overlap stays illegal on a side, even when the drawn tile shows stack.
    private CommandResult? RejectBlockedSide(
        int tileX,
        int tileY,
        IReadOnlyList<(int X, int Y, Cell Value)> located)
    {
        if (_grid.Overlaps(located))
        {
            return Reject(RejectionReason.CellOccupied, "That cell is already occupied.");
        }

        if (!_grid.SharesFullSide(tileX, tileY))
        {
            return Reject(
                RejectionReason.DoesNotShareFullSide,
                "A tile has to share a full side with a tile already on the board.");
        }

        return null;
    }

    private CommandResult? RejectRotateUses(Tile tile, IReadOnlyList<RotateUse> rotates)
    {
        if (rotates.Count == 0)
        {
            return Reject(RejectionReason.NoRotateUse, "Rotate placement needs at least one use.");
        }

        var charges = CountSymbol(tile, OrdinaryCatalog.Rotate);
        if (charges == 0)
        {
            return Reject(RejectionReason.NoRotateCell, "The drawn tile has no rotate cell.");
        }

        if (rotates.Count > charges)
        {
            return Reject(
                RejectionReason.TooManyRotateUses,
                "The drawn tile does not have a rotate cell for every use.");
        }

        foreach (var use in rotates)
        {
            if (use.QuarterTurnsClockwise is < 1 or > 3)
            {
                return Reject(
                    RejectionReason.InvalidRotateQuarterTurns,
                    "A rotate use is 1, 2, or 3 quarter-turns.");
            }
        }

        return null;
    }

    private CommandResult? RejectRotateTargets(int placedX, int placedY, IReadOnlyList<RotateUse> rotates)
    {
        foreach (var use in rotates)
        {
            if (!OccupiesAfterPlacement(use.TileX, use.TileY, placedX, placedY))
            {
                return Reject(RejectionReason.NoTileToRotate, "There is no tile at that position to rotate.");
            }

            if (SurroundedAfterPlacement(use.TileX, use.TileY, placedX, placedY))
            {
                return Reject(RejectionReason.TileSurrounded, "That tile is completely surrounded.");
            }
        }

        return null;
    }

    private CommandResult? RejectBounceUses(Tile tile, IReadOnlyList<BounceUse> bounces)
    {
        if (bounces.Count == 0)
        {
            return Reject(RejectionReason.NoBounceUse, "Bounce placement needs at least one use.");
        }

        var charges = CountSymbol(tile, OrdinaryCatalog.Bounce);
        if (charges == 0)
        {
            return Reject(RejectionReason.NoBounceCell, "The drawn tile has no bounce cell.");
        }

        if (bounces.Count > charges)
        {
            return Reject(
                RejectionReason.TooManyBounceUses,
                "The drawn tile does not have a bounce cell for every use.");
        }

        return null;
    }

    // The tile just placed is checked first: it is never on the grid yet, so HasTile alone cannot tell that
    // case apart from a genuinely empty position. Remaining layers are tracked per target across the uses in
    // this command, so a second use on an already-emptied single-layer position is rejected the same way as
    // a position that never had a tile, rather than throwing once the grid actually mutates.
    private CommandResult? RejectBounceTargets(int placedX, int placedY, IReadOnlyList<BounceUse> bounces)
    {
        var remainingLayers = new Dictionary<TileCoord, int>();
        foreach (var use in bounces)
        {
            if (use.TileX == placedX && use.TileY == placedY)
            {
                return Reject(
                    RejectionReason.CannotBounceJustPlacedTile,
                    "The tile just placed cannot be bounced on the same turn.");
            }

            var coord = new TileCoord(use.TileX, use.TileY);
            if (!remainingLayers.TryGetValue(coord, out var layers))
            {
                layers = _grid.LayerCountAt(use.TileX, use.TileY);
            }

            if (layers == 0)
            {
                return Reject(RejectionReason.NoTileToBounce, "There is no tile at that position to bounce.");
            }

            remainingLayers[coord] = layers - 1;
        }

        return null;
    }

    // The tile being placed is not on the grid yet. It still counts as a neighbor and as a legal target.
    private bool OccupiesAfterPlacement(int tileX, int tileY, int placedX, int placedY) =>
        (tileX == placedX && tileY == placedY) || _grid.HasTile(tileX, tileY);

    private bool SurroundedAfterPlacement(int tileX, int tileY, int placedX, int placedY) =>
        OccupiesAfterPlacement(tileX + 1, tileY, placedX, placedY)
        && OccupiesAfterPlacement(tileX - 1, tileY, placedX, placedY)
        && OccupiesAfterPlacement(tileX, tileY + 1, placedX, placedY)
        && OccupiesAfterPlacement(tileX, tileY - 1, placedX, placedY);

    private static bool ShowsStack(Tile tile) => CountSymbol(tile, OrdinaryCatalog.Stack) > 0;

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

    private CommandResult Reject(RejectionReason reason, string message) =>
        CommandResult.Reject(this, new Rejection(reason, message));

    private static Mission[] Copy(Mission[] missions)
    {
        var copy = new Mission[missions.Length];
        Array.Copy(missions, copy, missions.Length);
        return copy;
    }

    private SeatSnapshot Find(SeatId seat)
    {
        foreach (var snapshot in _seats)
        {
            if (snapshot.Id.Equals(seat))
            {
                return snapshot;
            }
        }

        throw new ArgumentException($"Seat '{seat.Value}' is not at the table.", nameof(seat));
    }

    private static Game Deal(GameSetup setup)
    {
        var seats = new SeatSnapshot[setup.SeatsInTurnOrder.Count];
        var deckIndex = 0;
        for (var i = 0; i < seats.Length; i++)
        {
            var hand = new[]
            {
                setup.MissionDeck[deckIndex],
                setup.MissionDeck[deckIndex + 1],
            };
            deckIndex += 2;
            seats[i] = new SeatSnapshot(setup.SeatsInTurnOrder[i], hand, []);
        }

        var missionsLeft = setup.MissionDeck.Count - deckIndex;
        var missionDeck = new Mission[missionsLeft];
        for (var i = 0; i < missionsLeft; i++)
        {
            missionDeck[i] = setup.MissionDeck[deckIndex + i];
        }

        var matchLeft = setup.MatchDeck.Count - 1;
        var matchDeck = new Tile[matchLeft];
        for (var i = 0; i < matchLeft; i++)
        {
            matchDeck[i] = setup.MatchDeck[i + 1];
        }

        return new Game(
            seats,
            IndexOf(seats, setup.FirstSeat),
            missionDeck,
            matchDeck,
            Grid.FromStart(setup.MatchDeck[0]),
            setup.ActivePatterns.ToArray(),
            setup.ClaimsRequiredToWin,
            hasEnded: false);
    }

    private static int IndexOf(SeatSnapshot[] seats, SeatId seat)
    {
        for (var i = 0; i < seats.Length; i++)
        {
            if (seats[i].Id.Equals(seat))
            {
                return i;
            }
        }

        throw new InvalidOperationException("The first seat was validated and then not found.");
    }

    private sealed class SeatSnapshot
    {
        public SeatSnapshot(SeatId id, Mission[] hand, Mission[] claims)
        {
            Id = id;
            Hand = hand;
            Claims = claims;
        }

        public SeatId Id { get; }

        public Mission[] Hand { get; }

        public Mission[] Claims { get; }
    }
}
