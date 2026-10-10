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
            // A rejected setup is not installed. Clearing an accepted session would make this rejection destructive, unlike Place.
            return SessionResult.Reject(Required(result.Rejection));
        }

        _game = RequiredGame(result);
        return SessionResult.Accept(result.Events);
    }

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
            game.PendingMatchTile);
    }

    public SessionResult Place(SeatId seat, Placement placement)
    {
        if (RejectionFor(seat) is SessionRejection blocked)
        {
            return SessionResult.Reject(blocked);
        }

        var result = StartedGame().Apply(
            seat,
            new Place(placement.TileX, placement.TileY, placement.QuarterTurnsClockwise));
        if (!result.IsAccepted)
        {
            return SessionResult.Reject(Required(result.Rejection));
        }

        _game = RequiredGame(result);
        return SessionResult.Accept(result.Events);
    }

    public PlacementPreview Preview(SeatId seat, Placement placement)
    {
        if (RejectionFor(seat) is SessionRejection blocked)
        {
            return PlacementPreview.Reject(blocked);
        }

        var result = StartedGame().Apply(
            seat,
            new Place(placement.TileX, placement.TileY, placement.QuarterTurnsClockwise));
        if (!result.IsAccepted)
        {
            return PlacementPreview.Reject(Required(result.Rejection));
        }

        var claimed = new List<MissionId>();
        foreach (var claim in result.Events.OfType<MissionClaimed>())
        {
            claimed.Add(claim.Mission);
        }

        return PlacementPreview.Accept(claimed);
    }

    private SessionRejection? RejectionFor(SeatId seat)
    {
        if (_game is null)
        {
            return new SessionRejection("The session has not started.", null);
        }

        if (!seat.Equals(_game.CurrentSeat))
        {
            return new SessionRejection("It is not that seat's turn.", null);
        }

        return null;
    }

    private Game StartedGame()
    {
        if (_game is null)
        {
            throw new InvalidOperationException("The session has not started.");
        }

        return _game;
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
