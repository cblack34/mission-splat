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

    // The tile the next Place or Stack will consume. Null when the game has ended or none remains; either command still refuses that empty deck.
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

        // Covering a tile is Stack. Overlap stays illegal here even when the drawn tile shows stack.
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

        return ResolvePlacement(
            tile,
            tileX,
            tileY,
            quarterTurnsClockwise,
            _grid.Place(tileX, tileY, tile.Id, located),
            located);
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
            _grid.Cover(tileX, tileY, tile.Id, located),
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

    // Both placements consume the front match tile. An empty deck is the same open ruling for either.
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
        IReadOnlyList<(int X, int Y, Cell Value)> located)
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

        var seats = (SeatSnapshot[])_seats.Clone();
        seats[_currentIndex] = new SeatSnapshot(acting.Id, hand.ToArray(), claims.ToArray());
        var next = new Game(
            seats,
            (_currentIndex + 1) % seats.Length,
            missionDeck.ToArray(),
            _matchDeck[1..],
            nextGrid,
            _patterns,
            ClaimsRequiredToWin,
            won);

        return CommandResult.Accept(next, events);
    }

    private static bool ShowsStack(Tile tile)
    {
        for (var y = 0; y < 2; y++)
        {
            for (var x = 0; x < 2; x++)
            {
                if (tile.Local(x, y).TryGetSymbol(out var symbol) && symbol.Equals(OrdinaryCatalog.Stack))
                {
                    return true;
                }
            }
        }

        return false;
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
