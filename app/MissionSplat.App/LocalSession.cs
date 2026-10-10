namespace MissionSplat.App;

using MissionSplat.Rules;

public sealed class LocalSession : ISession
{
    private Game? _game;

    public SessionResult Start(GameSetup setup)
    {
        var result = Game.Start(setup);
        if (!result.IsAccepted)
        {
            // A rejected setup is not installed. Clearing an accepted session would make this rejection destructive, unlike Submit.
            return SessionResult.Reject(Required(result.Rejection));
        }

        _game = RequiredGame(result);
        return SessionResult.Accept(result.Events);
    }

    public SeatId CurrentSeat => StartedGame().CurrentSeat;

    public IReadOnlyList<SymbolId> PowersInPlay => StartedGame().PowersInPlay;

    public SeatView View(SeatId seat)
    {
        var game = StartedGame();
        var hand = game.Hand(seat);
        var seats = game.SeatsInTurnOrder;
        var claims = new SeatClaims[seats.Count];
        for (var i = 0; i < seats.Count; i++)
        {
            claims[i] = new SeatClaims(seats[i], game.Claims(seats[i]));
        }

        return new SeatView(
            seat,
            hand,
            claims,
            Board(game),
            game.CurrentSeat,
            game.HasEnded,
            game.PendingMatchTile,
            RemainingUses(game));
    }

    public SessionResult Submit(SeatId seat, GameAction action)
    {
        if (_game is null)
        {
            return SessionResult.Reject(NotStarted());
        }

        var result = _game.Apply(seat, action);
        if (!result.IsAccepted)
        {
            return SessionResult.Reject(Required(result.Rejection));
        }

        _game = RequiredGame(result);
        return SessionResult.Accept(result.Events);
    }

    public IReadOnlyList<LegalPlacement> LegalPlacements(SeatId seat, int quarterTurnsClockwise) =>
        _game is { } game && seat.Equals(game.CurrentSeat) ? game.LegalPlacements(quarterTurnsClockwise) : [];

    public IReadOnlyList<BoardPosition> LegalTargets(SeatId seat, SymbolId power) =>
        _game is { } game && seat.Equals(game.CurrentSeat) ? game.LegalTargets(power) : [];

    public ActionPreview Preview(SeatId seat, GameAction action)
    {
        if (_game is null)
        {
            return ActionPreview.Reject(NotStarted());
        }

        var result = _game.Apply(seat, action);
        if (!result.IsAccepted)
        {
            return ActionPreview.Reject(Required(result.Rejection));
        }

        var claimed = new List<MissionId>();
        foreach (var claim in result.Events.OfType<MissionClaimed>())
        {
            claimed.Add(claim.Mission);
        }

        return ActionPreview.Accept(claimed);
    }

    private static SessionRejection NotStarted() => new("The session has not started.", null);

    private Game StartedGame()
    {
        return _game ?? throw new InvalidOperationException(NotStarted().Message);
    }

    // Charges are public table state: the drawn tile is shown to every seat. Stack is no use, so it never appears.
    private static PowerCharge[] RemainingUses(Game game)
    {
        var charges = new List<PowerCharge>();
        foreach (var power in game.PowersInPlay)
        {
            if (power.Equals(OrdinaryCatalog.Rotate) || power.Equals(OrdinaryCatalog.Bounce))
            {
                charges.Add(new PowerCharge(power, game.RemainingUses(power)));
            }
        }

        return charges.ToArray();
    }

    private BoardView Board(Game game)
    {
        var visible = game.Tiles;
        var tiles = new OccupiedTile[visible.Count];
        var cells = new OccupiedCell[visible.Count * 4];
        var cellIndex = 0;
        for (var i = 0; i < visible.Count; i++)
        {
            var placed = visible[i];
            tiles[i] = new OccupiedTile(placed.Id, placed.TileX, placed.TileY);
            for (var y = 0; y < 2; y++)
            {
                for (var x = 0; x < 2; x++)
                {
                    var cellX = (placed.TileX * 2) + x;
                    var cellY = (placed.TileY * 2) + y;
                    cells[cellIndex++] = new OccupiedCell(cellX, cellY, RequiredCell(game, cellX, cellY));
                }
            }
        }

        return new BoardView(tiles, cells);
    }

    private static Cell RequiredCell(Game game, int cellX, int cellY)
    {
        return game.CellAt(cellX, cellY)
            ?? throw new InvalidOperationException("An accepted placement left no cell.");
    }

    private static Rejection Required(Rejection? rejection)
    {
        if (rejection is null)
        {
            throw new InvalidOperationException("A rejected command had no rejection.");
        }

        return rejection;
    }

    private static Game RequiredGame(CommandResult result)
    {
        if (result.Game is not Game game)
        {
            throw new InvalidOperationException("An accepted command did not return a game.");
        }

        return game;
    }
}
