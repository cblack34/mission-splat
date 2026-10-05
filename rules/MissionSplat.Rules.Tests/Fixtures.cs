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

    public static CommandResult Play(Game game, int tileX, int tileY, int quarterTurns = 0)
    {
        var result = game.Place(tileX, tileY, quarterTurns);
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

    public static Cell Symbol(string name) => Cell.Symbol(new SymbolId(name));
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
}
