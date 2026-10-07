namespace MissionSplat.App.Tests;

using MissionSplat.App;
using MissionSplat.Rules;

// Every fixture deals a representative deck: ordered for the test, not a census of a physical box.
internal static class RepresentativeDeck
{
    public const string Label = "representative";

    public static GameSetup Setup(string[] seatNames, string firstSeat, Mission[] missions, Tile[] tiles)
    {
        return new GameSetup(
            seatNames.Select(name => new SeatId(name)).ToArray(),
            new SeatId(firstSeat),
            OrdinaryCatalog.Colors,
            OrdinaryCatalog.NonScoringSymbols,
            OrdinaryCatalog.Patterns,
            OrdinaryCatalog.ClaimsRequiredToWin,
            missions,
            tiles);
    }
}

internal static class Cards
{
    public static readonly Cell Red = Cell.Color(OrdinaryCatalog.Red);

    public static readonly Cell Blue = Cell.Color(OrdinaryCatalog.Blue);

    public static readonly Cell Green = Cell.Color(OrdinaryCatalog.Green);

    public static readonly Cell Purple = Cell.Color(OrdinaryCatalog.Purple);

    public static readonly Cell Blank = Cell.Symbol(OrdinaryCatalog.Blank);

    public static Mission Mission(string id, MissionPattern pattern, ColorId color) =>
        new(new MissionId(id), pattern, color);

    public static Mission Row(string id) => Mission(id, MissionPattern.Row, OrdinaryCatalog.Purple);

    public static Tile Solid(string id, Cell cell) => new(new TileId(id), cell, cell, cell, cell);

    public static Tile BlankTile(string id) => Solid(id, Blank);
}

internal static class ClaimingTable
{
    private static readonly string[] Names = ["a", "b", "c", "d"];

    private static readonly Cell[] SeatColors = [Cards.Red, Cards.Blue, Cards.Green, Cards.Purple];

    private static readonly Placement[] AroundTheStart =
    [
        new(1, 0, 0),
        new(0, 1, 0),
        new(-1, 0, 0),
        new(0, -1, 0),
    ];

    public static Table Open(int seatCount)
    {
        var names = Names.Take(seatCount).ToArray();
        var missions = new List<Mission>();
        foreach (var name in names)
        {
            missions.Add(Cards.Mission(name + "-square", MissionPattern.Square, ColorOf(name)));
            missions.Add(Cards.Row(name + "-row"));
        }

        foreach (var name in names)
        {
            missions.Add(Cards.Row(name + "-replacement"));
        }

        missions.Add(Cards.Row("spare-mission"));

        var tiles = new List<Tile> { Cards.BlankTile("start") };
        for (var i = 0; i < names.Length; i++)
        {
            tiles.Add(Cards.Solid(names[i] + "-tile", SeatColors[i]));
        }

        tiles.Add(Cards.BlankTile("spare-tile"));

        var session = new LocalSession();
        var started = session.Start(RepresentativeDeck.Setup(names, names[0], missions.ToArray(), tiles.ToArray()));
        Assert.That(started.IsAccepted, Is.True, RepresentativeDeck.Label + ": " + started.Rejection?.Message);

        var players = new IPlayer[names.Length];
        for (var i = 0; i < names.Length; i++)
        {
            players[i] = new ScriptedPlayer(names[i], AroundTheStart[i]);
        }

        return new Table(session, players);
    }

    private static ColorId ColorOf(string name) => name switch
    {
        "a" => OrdinaryCatalog.Red,
        "b" => OrdinaryCatalog.Blue,
        "c" => OrdinaryCatalog.Green,
        "d" => OrdinaryCatalog.Purple,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "The representative table has seats a through d."),
    };

    internal sealed record Table(LocalSession Session, IPlayer[] Players);
}

internal sealed class ScriptedPlayer : IPlayer
{
    private readonly Queue<Placement> _placements;

    public ScriptedPlayer(string seat, params Placement[] placements)
    {
        Seat = new SeatId(seat);
        _placements = new Queue<Placement>(placements);
    }

    public SeatId Seat { get; }

    public Placement ChoosePlacement(SeatView view)
    {
        if (!view.Seat.Equals(Seat))
        {
            throw new ArgumentException("The view is for a different seat.", nameof(view));
        }

        if (_placements.Count == 0)
        {
            throw new InvalidOperationException("The script has no placement left.");
        }

        return _placements.Dequeue();
    }
}
