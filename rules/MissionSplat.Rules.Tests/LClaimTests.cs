namespace MissionSplat.Rules.Tests;

public class LClaimTests
{
    // Eight rotations and reflections of a length-3 arm with a foot on one end.
    private static readonly (int X, int Y)[][] Shapes =
    [
        [(0, 0), (0, 1), (0, 2), (1, 0)],
        [(0, 0), (0, 1), (1, 1), (2, 1)],
        [(0, 2), (1, 0), (1, 1), (1, 2)],
        [(0, 0), (1, 0), (2, 0), (2, 1)],
        [(0, 0), (1, 0), (1, 1), (1, 2)],
        [(0, 0), (0, 1), (1, 0), (2, 0)],
        [(0, 0), (0, 1), (0, 2), (1, 2)],
        [(0, 1), (1, 1), (2, 0), (2, 1)],
    ];

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    public void L_EachOrientation_ClaimsTheL_AndNotTheStraightRow(int index)
    {
        var finished = Finish(
            index,
            Cards.Mission("l-red", MissionPattern.L, OrdinaryCatalog.Red),
            Cards.Mission("row-red", MissionPattern.Row, OrdinaryCatalog.Red));

        Assert.That(See.Ids(finished.Claims(Cards.Seat("a"))), Is.EqualTo(new[] { "l-red" }));
        Assert.That(See.Ids(finished.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "row-red", "repl" }));
        Assert.That(finished.Claims(Cards.Seat("b")), Is.Empty);
        Assert.That(finished.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
        Assert.That(finished.HasTileAt(0, 0), Is.True);
        foreach (var (x, y) in Shapes[index])
        {
            Assert.That(finished.CellAt(x, y), Is.EqualTo(Cards.Red));
        }
    }

    [Test]
    public void StraightRow_DoesNotSatisfyAnLMission()
    {
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Cards.Mission("l-red", MissionPattern.L, OrdinaryCatalog.Red),
                Cards.Purple("a2"),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Cards.Purple("spare"),
            ],
            [
                Cards.Tile("start", Cards.Red, Cards.Red, Cards.Blank, Cards.Blank),
                Cards.Tile("finish", Cards.Red, Cards.Red, Cards.Blank, Cards.Blank),
            ]);

        var next = See.Game(RepresentativeDeck.Play(game, 1, 0));
        Assert.That(next.Claims(Cards.Seat("a")), Is.Empty);
        Assert.That(See.Ids(next.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "l-red", "a2" }));
    }

    [Test]
    public void SquareTile_DoesNotSatisfyAnLMission()
    {
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Cards.Mission("l-red", MissionPattern.L, OrdinaryCatalog.Red),
                Cards.Purple("a2"),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Cards.Purple("spare"),
            ],
            [Cards.BlankTile("start"), Cards.Solid("square", Cards.Red)]);

        var next = See.Game(RepresentativeDeck.Play(game, 1, 0));
        Assert.That(next.Claims(Cards.Seat("a")), Is.Empty);
    }

    [Test]
    public void LShape_DoesNotSatisfyASquareMission()
    {
        var finished = Finish(
            0,
            Cards.Mission("square-red", MissionPattern.Square, OrdinaryCatalog.Red),
            Cards.Purple("a2"));

        Assert.That(finished.Claims(Cards.Seat("a")), Is.Empty);
        Assert.That(See.Ids(finished.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "square-red", "a2" }));
    }

    [Test]
    public void Tee_DoesNotSatisfyAnLMission()
    {
        // Vertical arm of three with the foot on the middle, not an end.
        var game = StartOpposite(
            Cards.Tile("start", Cards.Red, Cards.Blank, Cards.Red, Cards.Red),
            Cards.Tile("finish", Cards.Red, Cards.Blank, Cards.Blank, Cards.Blank));

        var next = See.Game(RepresentativeDeck.Play(game, 0, 1));
        Assert.That(next.CellAt(0, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(0, 1), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(0, 2), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(1, 1), Is.EqualTo(Cards.Red));
        Assert.That(next.Claims(Cards.Seat("a")), Is.Empty);
    }

    [Test]
    public void MissingArmMiddle_DoesNotSatisfyAnLMission()
    {
        // First L template without (0,1): (0,0), (0,2), (1,0). (0,2) is on tile (0,1).
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Cards.Mission("l-red", MissionPattern.L, OrdinaryCatalog.Red),
                Cards.Purple("a2"),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Cards.Purple("spare"),
            ],
            [
                Cards.Tile("start", Cards.Red, Cards.Red, Cards.Blank, Cards.Blank),
                Cards.Tile("gap", Cards.Red, Cards.Blank, Cards.Blank, Cards.Blank),
            ]);

        var next = See.Game(RepresentativeDeck.Play(game, 0, 1));

        Assert.That(next.CellAt(0, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(1, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(0, 1), Is.EqualTo(Cards.Blank));
        Assert.That(next.CellAt(0, 2), Is.EqualTo(Cards.Red));
        Assert.That(next.Claims(Cards.Seat("a")), Is.Empty);
        Assert.That(See.Ids(next.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "l-red", "a2" }));
    }

    [Test]
    public void Skew_DoesNotSatisfyAnLMission()
    {
        var game = StartOpposite(
            Cards.Tile("start", Cards.Blank, Cards.Red, Cards.Red, Cards.Red),
            Cards.Tile("finish", Cards.Red, Cards.Blank, Cards.Blank, Cards.Blank));

        var next = See.Game(RepresentativeDeck.Play(game, 1, 0));
        Assert.That(next.CellAt(1, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(0, 1), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(1, 1), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(2, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.Claims(Cards.Seat("a")), Is.Empty);
    }

    private static Game Finish(int index, Mission first, Mission second)
    {
        var shape = Shapes[index];
        var tiles = shape.Select(cell => (TileX: cell.X / 2, TileY: cell.Y / 2)).Distinct().ToArray();
        Assert.That(tiles, Has.Length.EqualTo(2), $"orientation {index} should cross one tile boundary");
        var other = tiles.Single(tile => tile != (0, 0));
        Assert.That(Math.Abs(other.TileX) + Math.Abs(other.TileY), Is.EqualTo(1), $"orientation {index}");

        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                first,
                second,
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Cards.Purple("repl"),
                Cards.Purple("spare"),
            ],
            [Paint("start", 0, 0, shape), Paint("finish", other.TileX, other.TileY, shape)]);

        Assert.That(game.Claims(Cards.Seat("a")), Is.Empty);
        return See.Game(RepresentativeDeck.Play(game, other.TileX, other.TileY));
    }

    private static Game StartOpposite(Tile start, Tile finish) =>
        RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Cards.Mission("l-red", MissionPattern.L, OrdinaryCatalog.Red),
                Cards.Purple("a2"),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Cards.Purple("spare"),
            ],
            [start, finish]);

    private static Tile Paint(string id, int tileX, int tileY, IReadOnlyCollection<(int X, int Y)> reds)
    {
        Cell At(int localX, int localY)
        {
            var world = (tileX * 2 + localX, tileY * 2 + localY);
            return reds.Contains(world) ? Cards.Red : Cards.Blank;
        }

        return Cards.Tile(id, At(0, 0), At(1, 0), At(0, 1), At(1, 1));
    }
}
