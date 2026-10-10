namespace MissionSplat.Rules;

public sealed class Game
{
    private readonly SeatSnapshot[] _seats;
    private readonly int _currentIndex;
    private readonly Mission[] _missionDeck;
    private readonly Tile[] _matchDeck;
    private readonly Grid _grid;
    private readonly Ruleset _ruleset;
    private readonly SpentUses _spent;
    private readonly bool _hasEnded;

    private Game(
        SeatSnapshot[] seats,
        int currentIndex,
        Mission[] missionDeck,
        Tile[] matchDeck,
        Grid grid,
        Ruleset ruleset,
        int claimsRequiredToWin,
        SpentUses spent,
        bool hasEnded)
    {
        _seats = seats;
        _currentIndex = currentIndex;
        _missionDeck = missionDeck;
        _matchDeck = matchDeck;
        _grid = grid;
        _ruleset = ruleset;
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

        if (PendingMatchTile is { } drawn)
        {
            RejectOpenRulings(drawn);
        }

        return action switch
        {
            UseRotate rotate => ApplyRotate(rotate),
            UseBounce bounce => ApplyBounce(bounce),
            Place place => ApplyPlace(place),
            _ => throw new ArgumentException("That action is not part of the closed action set.", nameof(action)),
        };
    }

    // Read-only and total: no drawn tile, an ended game, quarter-turns outside 0..3, or a tile showing two different
    // powers (an open ruling) yield nothing, never an exception.
    public IReadOnlyList<LegalPlacement> LegalPlacements(int quarterTurnsClockwise)
    {
        if (QueryTile is not { } tile || !IsOrientation(quarterTurnsClockwise))
        {
            return [];
        }

        var legal = new List<LegalPlacement>();
        foreach (var position in _ruleset.PlacementCandidates(_grid))
        {
            if (_ruleset.PlacementRefusal(_grid, tile, position.TileX, position.TileY) is null)
            {
                legal.Add(new LegalPlacement(
                    position.TileX,
                    position.TileY,
                    _ruleset.KindAt(_grid, position.TileX, position.TileY)));
            }
        }

        return legal;
    }

    public IReadOnlyList<BoardPosition> LegalTargets(SymbolId power)
    {
        if (QueryTile is not { } tile || !OrdinaryCatalog.IsUsePower(power))
        {
            return [];
        }

        var targets = new List<BoardPosition>();
        foreach (var coord in _grid.TilePositions)
        {
            if (_ruleset.UseRefusal(_grid, tile, power, _spent.Of(power), coord.X, coord.Y) is null)
            {
                targets.Add(new BoardPosition(coord.X, coord.Y));
            }
        }

        return BoardOrder.Sorted(targets);
    }

    // Stack is not counted: it is no separate use, only the on-top option inside a placement.
    public int RemainingUses(SymbolId power) =>
        QueryTile is { } tile && OrdinaryCatalog.IsUsePower(power) ? _ruleset.Remaining(tile, power, _spent.Of(power)) : 0;

    // The top tile at each occupied position, ordered by X then Y; a covered tile is not listed.
    public IReadOnlyList<VisibleTile> Tiles
    {
        get
        {
            var positions = new List<BoardPosition>(_grid.TileCount);
            foreach (var coord in _grid.TilePositions)
            {
                positions.Add(new BoardPosition(coord.X, coord.Y));
            }

            var tiles = new List<VisibleTile>(positions.Count);
            foreach (var position in BoardOrder.Sorted(positions))
            {
                var tile = _grid.TileAt(position.TileX, position.TileY)
                    ?? throw new InvalidOperationException("A listed board position holds no tile.");
                tiles.Add(new VisibleTile(tile.Id, position.TileX, position.TileY));
            }

            return tiles;
        }
    }

    public IReadOnlyList<Mission> Hand(SeatId seat) => Copy(Find(seat).Hand);

    public IReadOnlyList<Mission> Claims(SeatId seat) => Copy(Find(seat).Claims);

    public Cell? CellAt(int cellX, int cellY) => _grid.At(cellX, cellY);

    public bool HasTileAt(int tileX, int tileY) => _grid.HasTile(tileX, tileY);

    public IReadOnlyList<TileId> CoveredTileIds(int tileX, int tileY) => _grid.CoveredTileIds(tileX, tileY);

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
        var completed = _ruleset.CompletedMissions(acting.Hand, nextGrid.Cells, written);
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
            _ruleset,
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

        var drawn = DrawnTile();
        var refusal = _ruleset.UseRefusal(
            _grid,
            drawn,
            OrdinaryCatalog.Rotate,
            _spent.Of(OrdinaryCatalog.Rotate),
            rotate.TileX,
            rotate.TileY);
        if (refusal is not null)
        {
            return CommandResult.Reject(this, refusal);
        }

        var grid = _grid.TurnClockwise(rotate.TileX, rotate.TileY, rotate.QuarterTurnsClockwise);
        var next = AfterPower(grid, _matchDeck, _spent.WithUse(OrdinaryCatalog.Rotate));
        return CommandResult.Accept(
            next,
            [new TileRotated(CurrentSeat, rotate.TileX, rotate.TileY, rotate.QuarterTurnsClockwise)]);
    }

    private CommandResult ApplyBounce(UseBounce bounce)
    {
        var drawn = DrawnTile();
        var refusal = _ruleset.UseRefusal(
            _grid,
            drawn,
            OrdinaryCatalog.Bounce,
            _spent.Of(OrdinaryCatalog.Bounce),
            bounce.TileX,
            bounce.TileY);
        if (refusal is not null)
        {
            return CommandResult.Reject(this, refusal);
        }

        var (grid, removed, revealed) = _grid.Bounce(bounce.TileX, bounce.TileY);

        var matchDeck = new Tile[_matchDeck.Length + 1];
        Array.Copy(_matchDeck, matchDeck, _matchDeck.Length);
        matchDeck[^1] = removed;

        var next = AfterPower(grid, matchDeck, _spent.WithUse(OrdinaryCatalog.Bounce));
        return CommandResult.Accept(
            next,
            [new TileBounced(CurrentSeat, bounce.TileX, bounce.TileY, removed.Id, revealed)]);
    }

    private CommandResult ApplyPlace(Place place)
    {
        if (!IsOrientation(place.QuarterTurnsClockwise))
        {
            return Reject(RejectionReason.InvalidQuarterTurns, "Quarter-turns are 0, 1, 2, or 3.");
        }

        var tile = DrawnTile();
        var refusal = _ruleset.PlacementRefusal(_grid, tile, place.TileX, place.TileY);
        if (refusal is not null)
        {
            return CommandResult.Reject(this, refusal);
        }

        var located = tile.CellsAt(place.TileX, place.TileY, place.QuarterTurnsClockwise);
        TileId? covered = null;
        Grid grid;
        if (_ruleset.KindAt(_grid, place.TileX, place.TileY) == PlacementKind.OnTop)
        {
            (grid, var buriedId) = _grid.Cover(place.TileX, place.TileY, tile, located);
            covered = buriedId;
        }
        else
        {
            grid = _grid.Place(place.TileX, place.TileY, tile, located);
        }

        return ResolvePlacement(tile, place, grid, located, covered);
    }

    private static bool IsOrientation(int quarterTurns) => quarterTurns is >= 0 and <= 3;

    // The drawn tile as the queries see it: none when a tile shows two different powers, an open ruling Apply refuses.
    private Tile? QueryTile => PendingMatchTile is { } tile && !_ruleset.ShowsMixedPowers(tile) ? tile : null;

    // docs/rules.md leaves a tile showing two different powers open, so no command is applied while one is drawn.
    private void RejectOpenRulings(Tile tile)
    {
        if (_ruleset.ShowsMixedPowers(tile))
        {
            throw new UnresolvedRulingException(
                "The drawn tile shows more than one power. A tile showing two different powers is an open ruling, so this command was not applied.");
        }
    }

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
    private Game AfterPower(Grid grid, Tile[] matchDeck, SpentUses spent) =>
        new(
            _seats,
            _currentIndex,
            _missionDeck,
            matchDeck,
            grid,
            _ruleset,
            ClaimsRequiredToWin,
            spent,
            _hasEnded);

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
            new Ruleset(setup.NonScoringSymbols, setup.ActivePatterns),
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
