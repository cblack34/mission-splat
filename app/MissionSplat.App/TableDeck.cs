namespace MissionSplat.App;

using MissionSplat.Rules;

public static class TableDeck
{
    public const string Name = "table";

    public static GameSetup Ordinary(IReadOnlyList<SeatId> seats, SeatId firstSeat, int? shuffleSeed)
    {
        if (seats is null)
        {
            throw new ArgumentNullException(nameof(seats));
        }

        var missions = Missions();
        var tiles = Tiles();
        if (shuffleSeed is int seed)
        {
            var rng = new Random(seed);
            Shuffle(missions, rng);
            Shuffle(tiles, rng);
        }

        return new GameSetup(
            seats,
            firstSeat,
            OrdinaryCatalog.Colors,
            OrdinaryCatalog.NonScoringSymbols,
            OrdinaryCatalog.Patterns,
            OrdinaryCatalog.ClaimsRequiredToWin,
            missions,
            tiles);
    }

    private static Mission[] Missions()
    {
        var patterns = new[] { MissionPattern.Square, MissionPattern.Row, MissionPattern.L };
        var names = new[] { "square", "row", "l" };
        var colors = OrdinaryCatalog.Colors;
        var missions = new Mission[patterns.Length * 3 * colors.Count];
        var index = 0;
        for (var pattern = 0; pattern < patterns.Length; pattern++)
        {
            for (var copy = 1; copy <= 3; copy++)
            {
                for (var color = 0; color < colors.Count; color++)
                {
                    var id = names[pattern] + "-" + colors[color].Value + "-" + copy;
                    missions[index++] = new Mission(new MissionId(id), patterns[pattern], colors[color]);
                }
            }
        }

        return missions;
    }

    private static Tile[] Tiles()
    {
        var red = Cell.Color(OrdinaryCatalog.Red);
        var blue = Cell.Color(OrdinaryCatalog.Blue);
        var green = Cell.Color(OrdinaryCatalog.Green);
        var purple = Cell.Color(OrdinaryCatalog.Purple);
        var blank = Cell.Symbol(OrdinaryCatalog.Blank);
        var wild = Cell.Wild();
        var rotate = Cell.Symbol(OrdinaryCatalog.Rotate);
        var stack = Cell.Symbol(OrdinaryCatalog.Stack);
        var bounce = Cell.Symbol(OrdinaryCatalog.Bounce);
        var colors = new[] { red, blue, green, purple };
        var tiles = new List<Tile>
        {
            new(new TileId("start"), blank, red, blue, wild),
            new(new TileId("solid-red"), red, red, red, red),
            new(new TileId("solid-blue"), blue, blue, blue, blue),
            new(new TileId("solid-green"), green, green, green, green),
            new(new TileId("solid-purple"), purple, purple, purple, purple),
        };

        var pair = 0;
        for (var a = 0; a < colors.Length; a++)
        {
            for (var b = 0; b < colors.Length; b++)
            {
                tiles.Add(new Tile(new TileId("pair-" + pair), colors[a], colors[b], colors[a], colors[b]));
                pair++;
            }
        }

        for (var i = 0; i < colors.Length; i++)
        {
            var next = colors[(i + 1) % colors.Length];
            var skip = colors[(i + 2) % colors.Length];
            var prev = colors[(i + 3) % colors.Length];
            tiles.Add(new Tile(new TileId("wild-" + i), colors[i], wild, next, blank));
            tiles.Add(new Tile(new TileId("blank-" + i), colors[i], blank, blank, skip));
            tiles.Add(new Tile(new TileId("rotate-" + i), colors[i], rotate, next, blank));
            tiles.Add(new Tile(new TileId("stack-" + i), colors[i], stack, blank, prev));
            tiles.Add(new Tile(new TileId("bounce-" + i), bounce, colors[i], skip, blank));
            tiles.Add(new Tile(new TileId("mix-" + i), colors[i], next, skip, prev));
        }

        tiles.Add(new Tile(new TileId("spare-a"), red, green, blue, purple));
        tiles.Add(new Tile(new TileId("spare-b"), purple, red, green, blue));
        tiles.Add(new Tile(new TileId("spare-c"), blue, purple, red, green));
        return tiles.ToArray();
    }

    private static void Shuffle<T>(IList<T> items, Random rng)
    {
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}
