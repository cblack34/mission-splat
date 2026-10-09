namespace MissionSplat.Rules;

public sealed class Game
{
    private readonly SeatSnapshot[] _seats;
    private readonly int _currentIndex;
    private readonly Mission[] _missionDeck;
    private readonly Tile[] _matchDeck;
    private readonly Grid _grid;
    private readonly MissionPattern[] _patterns;
    private readonly SymbolId[] _symbols;
    private readonly PowerUses _spent;
    private readonly bool _hasEnded;

    private Game(
        SeatSnapshot[] seats,
        int currentIndex,
        Mission[] missionDeck,
        Tile[] matchDeck,
        Grid grid,
        MissionPattern[] patterns,
        SymbolId[] symbols,
        int claimsRequiredToWin,
        PowerUses spent,
        bool hasEnded)
    {
        _seats = seats;
        _currentIndex = currentIndex;
        _missionDeck = missionDeck;
        _matchDeck = matchDeck;
        _grid = grid;
        _patterns = patterns;
        _symbols = symbols;
        ClaimsRequiredToWin = claimsRequiredToWin;
        _spent = spent;
        _hasEnded = hasEnded;
    }

    public SeatId CurrentSeat => _seats[_currentIndex].Id;

    public bool HasEnded => _hasEnded;

    public int ClaimsRequiredToWin { get; }

    public int TileCount => _grid.TileCount;

    public int MissionDeckRemaining => _missionDeck.Length;

    public int MatchDeckRemaining => _matchDeck.Length;

    // The drawn tile: the one Apply spends powers from and the next Place consumes. Null when the game has
    // ended or none remains; Apply still refuses that empty deck.
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

    public CommandResult Apply(SeatId seat, GameAction action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (_hasEnded)
        {
            return Reject(RejectionReason.GameOver, "The game has already been won.");
        }

        if (!seat.Equals(CurrentSeat))
        {
            return Reject(RejectionReason.NotYourTurn, "It is not that seat's turn.");
        }

        return action switch
        {
            UseRotate rotate => ApplyRotate(rotate),
            UseBounce bounce => ApplyBounce(bounce),
            Place place => ApplyPlace(place),
            _ => throw new ArgumentException("That action is not part of the closed action set.", nameof(action)),
        };
    }

    // Read-only and total: no drawn tile, an ended game, or quarter-turns outside 0..3 yield nothing, never an exception.
    public IReadOnlyList<LegalPlacement> LegalPlacements(int quarterTurnsClockwise)
    {
        if (PendingMatchTile is not { } tile || quarterTurnsClockwise is < 0 or > 3)
        {
            return [];
        }

        var legal = new List<LegalPlacement>();
        foreach (var position in PlacementCandidates())
        {
            if (PlacementRefusal(tile, position.TileX, position.TileY) is null)
            {
                legal.Add(new LegalPlacement(position.TileX, position.TileY, KindAt(position.TileX, position.TileY)));
            }
        }

        return legal;
    }

    public IReadOnlyList<BoardPosition> LegalTargets(SymbolId power)
    {
        if (PendingMatchTile is not { } tile || !OrdinaryCatalog.IsUsePower(power) || Remaining(tile, power, Spent(power)) == 0)
        {
            return [];
        }

        var targets = new List<BoardPosition>();
        foreach (var coord in _grid.TilePositions)
        {
            if (TargetRefusal(power, coord.X, coord.Y) is null)
            {
                targets.Add(new BoardPosition(coord.X, coord.Y));
            }
        }

        return Sorted(targets);
    }

    // Stack is not counted: it is no separate use, only the on-top option inside a placement.
    public int RemainingUses(SymbolId power) =>
        PendingMatchTile is { } tile && OrdinaryCatalog.IsUsePower(power) ? Remaining(tile, power, Spent(power)) : 0;

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

    private CommandResult ResolvePlacement(
        Tile tile,
        Place place,
        Grid nextGrid,
        IReadOnlyList<(int X, int Y, Cell Value)> located,
        TileId? covered)
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
            new TilePlaced(
                acting.Id,
                tile.Id,
                place.TileX,
                place.TileY,
                place.QuarterTurnsClockwise,
                covered),
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
            _symbols,
            ClaimsRequiredToWin,
            default,
            won);

        return CommandResult.Accept(next, events);
    }

    private CommandResult ApplyRotate(UseRotate rotate)
    {
        if (rotate.QuarterTurnsClockwise is < 1 or > 3)
        {
            return Reject(RejectionReason.InvalidRotateQuarterTurns, "A rotate use is 1, 2, or 3 quarter-turns.");
        }

        var tile = DrawnTile();
        if (Remaining(tile, OrdinaryCatalog.Rotate, _spent.Rotates) == 0)
        {
            return Reject(RejectionReason.NoUseRemaining, "The drawn tile has no unused rotate cell.");
        }

        var refusal = TargetRefusal(OrdinaryCatalog.Rotate, rotate.TileX, rotate.TileY);
        if (refusal is not null)
        {
            return CommandResult.Reject(this, refusal);
        }

        var grid = _grid.TurnClockwise(rotate.TileX, rotate.TileY, rotate.QuarterTurnsClockwise);
        var next = AfterPower(grid, _matchDeck, _spent with { Rotates = _spent.Rotates + 1 });
        return CommandResult.Accept(
            next,
            [new TileRotated(CurrentSeat, rotate.TileX, rotate.TileY, rotate.QuarterTurnsClockwise)]);
    }

    private CommandResult ApplyBounce(UseBounce bounce)
    {
        var tile = DrawnTile();
        if (Remaining(tile, OrdinaryCatalog.Bounce, _spent.Bounces) == 0)
        {
            return Reject(RejectionReason.NoUseRemaining, "The drawn tile has no unused bounce cell.");
        }

        var refusal = TargetRefusal(OrdinaryCatalog.Bounce, bounce.TileX, bounce.TileY);
        if (refusal is not null)
        {
            return CommandResult.Reject(this, refusal);
        }

        var buried = _grid.CoveredTileIds(bounce.TileX, bounce.TileY);
        TileId? revealed = buried.Count == 0 ? null : buried[buried.Count - 1];
        var (grid, removed) = _grid.Bounce(bounce.TileX, bounce.TileY);

        var matchDeck = new Tile[_matchDeck.Length + 1];
        Array.Copy(_matchDeck, matchDeck, _matchDeck.Length);
        matchDeck[^1] = removed;

        var next = AfterPower(grid, matchDeck, _spent with { Bounces = _spent.Bounces + 1 });
        return CommandResult.Accept(
            next,
            [new TileBounced(CurrentSeat, bounce.TileX, bounce.TileY, removed.Id, revealed)]);
    }

    private CommandResult ApplyPlace(Place place)
    {
        if (place.QuarterTurnsClockwise is < 0 or > 3)
        {
            return Reject(RejectionReason.InvalidQuarterTurns, "Quarter-turns are 0, 1, 2, or 3.");
        }

        var tile = DrawnTile();
        var refusal = PlacementRefusal(tile, place.TileX, place.TileY);
        if (refusal is not null)
        {
            return CommandResult.Reject(this, refusal);
        }

        var located = tile.CellsAt(place.TileX, place.TileY, place.QuarterTurnsClockwise);
        TileId? covered = null;
        Grid grid;
        if (KindAt(place.TileX, place.TileY) == PlacementKind.OnTop)
        {
            grid = _grid.Cover(place.TileX, place.TileY, tile, located);
            var buried = grid.CoveredTileIds(place.TileX, place.TileY);
            covered = buried[buried.Count - 1];
        }
        else
        {
            grid = _grid.Place(place.TileX, place.TileY, tile, located);
        }

        return ResolvePlacement(tile, place, grid, located, covered);
    }

    // The single placement decision, shared by Apply and LegalPlacements so they cannot disagree. Orientation only
    // changes which cells are written, never where the tile may go.
    private Rejection? PlacementRefusal(Tile tile, int tileX, int tileY)
    {
        if (_grid.TileCount == 0)
        {
            return tileX == 0 && tileY == 0
                ? null
                : new Rejection(RejectionReason.NotAtOrigin, "An empty board takes the drawn tile only at the origin.");
        }

        if (_grid.HasTile(tileX, tileY))
        {
            return ShowsStack(tile)
                ? null
                : new Rejection(RejectionReason.CellOccupied, "That position is already occupied.");
        }

        return _grid.SharesFullSide(tileX, tileY)
            ? null
            : new Rejection(
                RejectionReason.DoesNotShareFullSide,
                "A tile has to share a full side with a tile already on the board.");
    }

    // The single target decision for a use power, shared by Apply and LegalTargets so they cannot disagree.
    private Rejection? TargetRefusal(SymbolId power, int tileX, int tileY)
    {
        var isRotate = power.Equals(OrdinaryCatalog.Rotate);
        if (!_grid.HasTile(tileX, tileY))
        {
            return isRotate
                ? new Rejection(RejectionReason.NoTileToRotate, "There is no tile at that position to rotate.")
                : new Rejection(RejectionReason.NoTileToBounce, "There is no tile at that position to bounce.");
        }

        return isRotate && IsSurrounded(tileX, tileY)
            ? new Rejection(RejectionReason.TileSurrounded, "That tile is completely surrounded.")
            : null;
    }

    private PlacementKind KindAt(int tileX, int tileY) =>
        _grid.HasTile(tileX, tileY) ? PlacementKind.OnTop : PlacementKind.Beside;

    // Every position the drawn tile could be offered: each occupied one and its four neighbors, or the origin alone.
    private List<BoardPosition> PlacementCandidates()
    {
        var candidates = new HashSet<BoardPosition>();
        if (_grid.TileCount == 0)
        {
            candidates.Add(new BoardPosition(0, 0));
        }

        foreach (var coord in _grid.TilePositions)
        {
            candidates.Add(new BoardPosition(coord.X, coord.Y));
            candidates.Add(new BoardPosition(coord.X + 1, coord.Y));
            candidates.Add(new BoardPosition(coord.X - 1, coord.Y));
            candidates.Add(new BoardPosition(coord.X, coord.Y + 1));
            candidates.Add(new BoardPosition(coord.X, coord.Y - 1));
        }

        return Sorted(candidates);
    }

    private static List<BoardPosition> Sorted(IEnumerable<BoardPosition> positions)
    {
        var sorted = new List<BoardPosition>(positions);
        sorted.Sort((a, b) => a.TileX != b.TileX ? a.TileX.CompareTo(b.TileX) : a.TileY.CompareTo(b.TileY));
        return sorted;
    }

    private int Spent(SymbolId power) => power.Equals(OrdinaryCatalog.Rotate) ? _spent.Rotates : _spent.Bounces;

    // Every Apply that reaches a tile consumes the front match tile. An empty deck is the same open ruling for each.
    private Tile DrawnTile()
    {
        if (_matchDeck.Length == 0)
        {
            throw new UnresolvedRulingException(
                "The match deck has no tile to place. Exhausting the match deck is an open ruling, so this command was not applied.");
        }

        return _matchDeck[0];
    }

    // A power use keeps the same seat and the same drawn tile; only the board, the charge, and a bounced tile move.
    private Game AfterPower(Grid grid, Tile[] matchDeck, PowerUses spent) =>
        new(
            _seats,
            _currentIndex,
            _missionDeck,
            matchDeck,
            grid,
            _patterns,
            _symbols,
            ClaimsRequiredToWin,
            spent,
            _hasEnded);

    // Completely surrounded counts only tiles on the board; a stacked position is one occupied neighbor.
    private bool IsSurrounded(int tileX, int tileY) =>
        _grid.HasTile(tileX + 1, tileY)
        && _grid.HasTile(tileX - 1, tileY)
        && _grid.HasTile(tileX, tileY + 1)
        && _grid.HasTile(tileX, tileY - 1);

    // A power symbol the setup does not list is a blank: it grants no use and no on-top placement.
    private int Remaining(Tile tile, SymbolId power, int spent) =>
        IsInPlay(power) ? CountSymbol(tile, power) - spent : 0;

    private bool ShowsStack(Tile tile) => IsInPlay(OrdinaryCatalog.Stack) && CountSymbol(tile, OrdinaryCatalog.Stack) > 0;

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
            setup.NonScoringSymbols.ToArray(),
            setup.ClaimsRequiredToWin,
            default,
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

    // Uses already spent on the drawn tile this turn, one count per power. An accepted placement starts the next turn at zero.
    private readonly record struct PowerUses(int Rotates, int Bounces);

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
