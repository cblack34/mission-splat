namespace MissionSplat.Rules.Tests;

// Every fixture deals a representative deck: ordered for the test, not a census of a physical box.
internal static class RepresentativeDeck
{
    public const string Label = "representative";

    public static CommandResult Open(
        string[] seatNames,
        string firstSeat,
        Mission[] missions,
        Tile[] tiles,
        int claimsRequiredToWin = OrdinaryCatalog.ClaimsRequiredToWin,
        IReadOnlyList<ColorId>? colors = null,
        IReadOnlyList<SymbolId>? symbols = null,
        IReadOnlyList<MissionPattern>? patterns = null)
    {
        return Game.Start(new GameSetup(
            seatNames.Select(name => new SeatId(name)).ToArray(),
            new SeatId(firstSeat),
            colors ?? OrdinaryCatalog.Colors,
            symbols ?? OrdinaryCatalog.NonScoringSymbols,
            patterns ?? OrdinaryCatalog.Patterns,
            claimsRequiredToWin,
            missions,
            tiles));
    }

    public static Game Start(
        string[] seatNames,
        string firstSeat,
        Mission[] missions,
        Tile[] tiles,
        int claimsRequiredToWin = OrdinaryCatalog.ClaimsRequiredToWin,
        IReadOnlyList<ColorId>? colors = null,
        IReadOnlyList<SymbolId>? symbols = null,
        IReadOnlyList<MissionPattern>? patterns = null)
    {
        var result = Open(
            seatNames,
            firstSeat,
            missions,
            tiles,
            claimsRequiredToWin,
            colors,
            symbols,
            patterns);
        return See.Game(result);
    }

    // The acting seat is whoever is current, so a fixture names only the action.
    public static CommandResult Try(Game game, GameAction action) => game.Apply(game.CurrentSeat, action);

    public static CommandResult Play(Game game, int tileX, int tileY, int quarterTurns = 0) =>
        Accepted(Try(game, new Place(tileX, tileY, quarterTurns)));

    public static CommandResult Rotate(Game game, int tileX, int tileY, int quarterTurns = 1) =>
        Accepted(Try(game, new UseRotate(tileX, tileY, quarterTurns)));

    public static CommandResult Bounce(Game game, int tileX, int tileY) =>
        Accepted(Try(game, new UseBounce(tileX, tileY)));

    public static Game TwoSeats(params Tile[] tiles) =>
        Start(
            ["a", "b"],
            "a",
            [Cards.Purple("a1"), Cards.Purple("a2"), Cards.Purple("b1"), Cards.Purple("b2"), Cards.Purple("spare")],
            tiles);

    private static CommandResult Accepted(CommandResult result)
    {
        Assert.That(result.IsAccepted, Is.True, result.Rejection?.Message);
        return result;
    }
}

internal static class Cards
{
    public static readonly Cell Red = Cell.Color(OrdinaryCatalog.Red);

    public static readonly Cell Blue = Cell.Color(OrdinaryCatalog.Blue);

    public static readonly Cell Blank = Cell.Symbol(OrdinaryCatalog.Blank);

    public static readonly Cell Wild = Cell.Wild();

    public static SeatId Seat(string name) => new(name);

    public static Mission Mission(string id, MissionPattern pattern, ColorId color) =>
        new(new MissionId(id), pattern, color);

    public static Mission Purple(string id) => Mission(id, MissionPattern.Row, OrdinaryCatalog.Purple);

    public static Tile Tile(string id, Cell x0y0, Cell x1y0, Cell x0y1, Cell x1y1) =>
        new(new TileId(id), x0y0, x1y0, x0y1, x1y1);

    public static Tile Solid(string id, Cell cell) => Tile(id, cell, cell, cell, cell);

    public static Tile BlankTile(string id) => Solid(id, Blank);

    // One given power cell, the rest blank.
    public static Tile Showing(string id, Cell power) => Tile(id, power, Blank, Blank, Blank);

    public static Cell Symbol(string name) => Cell.Symbol(new SymbolId(name));

    // Four distinct colors, so every orientation of it reads differently.
    public static Tile Cross(string id) =>
        Tile(id, Red, Blue, Cell.Color(OrdinaryCatalog.Green), Cell.Color(OrdinaryCatalog.Purple));
}

internal static class Oriented
{
    // Cells run x0y0, x1y0, x0y1, x1y1 with y upward, so one clockwise turn moves bottom-left to top-left.
    public static Cell[] Clockwise(Cell[] upright, int quarterTurns) => quarterTurns switch
    {
        0 => [upright[0], upright[1], upright[2], upright[3]],
        1 => [upright[1], upright[3], upright[0], upright[2]],
        2 => [upright[3], upright[2], upright[1], upright[0]],
        3 => [upright[2], upright[0], upright[3], upright[1]],
        _ => throw new ArgumentOutOfRangeException(nameof(quarterTurns)),
    };

    public static Cell[] Cross(int quarterTurns) =>
        Clockwise(
            [Cards.Red, Cards.Blue, Cell.Color(OrdinaryCatalog.Green), Cell.Color(OrdinaryCatalog.Purple)],
            quarterTurns);
}

internal static class Expect
{
    // The four cells a tile shows at a board position, in the order x0y0, x1y0, x0y1, x1y1.
    public static void Tile(Game game, int tileX, int tileY, Cell[] cells)
    {
        Assert.That(game.CellAt(tileX * 2, tileY * 2), Is.EqualTo(cells[0]));
        Assert.That(game.CellAt((tileX * 2) + 1, tileY * 2), Is.EqualTo(cells[1]));
        Assert.That(game.CellAt(tileX * 2, (tileY * 2) + 1), Is.EqualTo(cells[2]));
        Assert.That(game.CellAt((tileX * 2) + 1, (tileY * 2) + 1), Is.EqualTo(cells[3]));
    }

    public static void Rejected(Game game, CommandResult result, RejectionReason reason)
    {
        Assert.That(result.IsAccepted, Is.False);
        Assert.That(result.Rejection?.Reason, Is.EqualTo(reason));
        Assert.That(result.Events, Is.Empty);
        Assert.That(result.Game, Is.SameAs(game));
    }
}

internal static class See
{
    public static Game Game(CommandResult result)
    {
        if (!result.IsAccepted || result.Game is not Game game)
        {
            Assert.Fail(result.Rejection?.Message ?? "Accepted command did not return a game.");
            throw new InvalidOperationException("Accepted command did not return a game.");
        }

        return game;
    }

    public static string[] Ids(IReadOnlyList<Mission> missions) =>
        missions.Select(mission => mission.Id.Value).ToArray();

    public static string[] Ids(IReadOnlyList<TileId> tiles) =>
        tiles.Select(tile => tile.Value).ToArray();
}

// Checks a query against Apply over a window wider than any fixture board: every listed answer is accepted and
// every other position in the window is refused. It returns the listed answer for the test to pin down.
internal static class Agreement
{
    private const int Reach = 3;

    public static IReadOnlyList<LegalPlacement> Placements(Game game, int quarterTurns)
    {
        var listed = game.LegalPlacements(quarterTurns);
        foreach (var (x, y) in Window())
        {
            var found = listed.Where(p => p.TileX == x && p.TileY == y).ToArray();
            var accepted = RepresentativeDeck.Try(game, new Place(x, y, quarterTurns)).IsAccepted;
            Assert.That(found.Length > 0, Is.EqualTo(accepted), $"placement ({x}, {y}) at {quarterTurns} quarter-turns");
            if (found.Length > 0)
            {
                var kind = game.HasTileAt(x, y) ? PlacementKind.OnTop : PlacementKind.Beside;
                Assert.That(found, Is.EqualTo(new[] { new LegalPlacement(x, y, kind) }));
            }
        }

        return listed;
    }

    public static IReadOnlyList<BoardPosition> Targets(Game game, SymbolId power)
    {
        var listed = game.LegalTargets(power);
        foreach (var (x, y) in Window())
        {
            GameAction? use = power.Equals(OrdinaryCatalog.Rotate) ? new UseRotate(x, y, 1)
                : power.Equals(OrdinaryCatalog.Bounce) ? new UseBounce(x, y)
                : null;
            var accepted = use is not null && RepresentativeDeck.Try(game, use).IsAccepted;
            Assert.That(listed.Contains(new BoardPosition(x, y)), Is.EqualTo(accepted), $"{power.Value} target ({x}, {y})");
        }

        return listed;
    }

    public static BoardPosition[] At(params (int X, int Y)[] positions) =>
        positions.Select(p => new BoardPosition(p.X, p.Y)).ToArray();

    private static IEnumerable<(int X, int Y)> Window()
    {
        for (var x = -Reach; x <= Reach; x++)
        {
            for (var y = -Reach; y <= Reach; y++)
            {
                yield return (x, y);
            }
        }
    }
}
