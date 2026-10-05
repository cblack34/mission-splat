namespace MissionSplat.Rules.Tests;

public class SetupTests
{
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void Setup_DealsTwoDistinctMissions_AndDoesNotClaim(int seatCount)
    {
        Assert.That(RepresentativeDeck.Label, Is.EqualTo("representative"));

        var names = new[] { "a", "b", "c", "d" }.Take(seatCount).ToArray();
        var first = names[^1];
        var missions = new Mission[seatCount * 2 + 1];
        missions[0] = Cards.Mission("square-red", MissionPattern.Square, OrdinaryCatalog.Red);
        for (var i = 1; i < missions.Length; i++)
        {
            missions[i] = Cards.Purple("m" + i);
        }

        var result = RepresentativeDeck.Open(
            names,
            first,
            missions,
            [Cards.Solid("start", Cards.Red), Cards.BlankTile("later")]);

        Assert.That(result.IsAccepted, Is.True, "representative deck: " + result.Rejection?.Message);
        Assert.That(result.Events, Is.Empty);
        var game = See.Game(result);

        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat(first)));
        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.HasTileAt(0, 0), Is.True);
        Assert.That(game.CellAt(0, 0), Is.EqualTo(Cards.Red));
        Assert.That(game.CellAt(1, 1), Is.EqualTo(Cards.Red));
        Assert.That(game.ClaimsRequiredToWin, Is.EqualTo(OrdinaryCatalog.ClaimsRequiredToWin));
        Assert.That(game.ClaimsRequiredToWin, Is.EqualTo(4));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(1));
        Assert.That(game.MissionDeckRemaining, Is.EqualTo(1));

        var dealt = new List<string>();
        for (var i = 0; i < seatCount; i++)
        {
            var seat = Cards.Seat(names[i]);
            var hand = game.Hand(seat);
            Assert.That(hand, Has.Count.EqualTo(2));
            Assert.That(See.Ids(hand), Is.EqualTo(new[] { missions[i * 2].Id.Value, missions[(i * 2) + 1].Id.Value }));
            Assert.That(game.Claims(seat), Is.Empty);
            dealt.AddRange(See.Ids(hand));
        }

        Assert.That(dealt, Is.Unique);
        Assert.That(dealt, Does.Contain("square-red"));
    }

    [Test]
    public void StartingWildcard_StaysWild_AndSetupDoesNotClaim()
    {
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "b",
            [Cards.Purple("a1"), Cards.Purple("a2"), Cards.Purple("b1"), Cards.Purple("b2")],
            [Cards.Tile("start", Cards.Wild, Cards.Blank, Cards.Blank, Cards.Blank), Cards.BlankTile("later")]);

        var wild = game.CellAt(0, 0);
        Assert.That(wild?.IsWild, Is.True);
        Assert.That(wild?.TryGetColor(out _), Is.False);
        Assert.That(wild?.CountsAs(OrdinaryCatalog.Red), Is.True);
        Assert.That(wild?.CountsAs(OrdinaryCatalog.Blue), Is.True);
        Assert.That(game.Claims(Cards.Seat("a")), Is.Empty);
        Assert.That(game.Claims(Cards.Seat("b")), Is.Empty);
        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
    }

    [Test]
    public void DefaultColor_DoesNotMatchAColor_AndSetupRejectsIt()
    {
        var cell = Cell.Color(default);
        Assert.That(cell.CountsAs(default), Is.False);
        Assert.That(cell.CountsAs(OrdinaryCatalog.Red), Is.False);
        Assert.That(cell.TryGetColor(out _), Is.False);

        var result = Open(tiles: [new Tile(new TileId("t"), cell, Cards.Blank, Cards.Blank, Cards.Blank)]);

        Rejects(result, "Tile 't' has a cell with no id.");
    }

    [Test]
    public void DefaultSymbolCell_IsRejected()
    {
        var result = Open(tiles: [new Tile(
            new TileId("t"),
            Cell.Symbol(default),
            Cards.Blank,
            Cards.Blank,
            Cards.Blank)]);

        Rejects(result, "Tile 't' has a cell with no id.");
    }

    [Test]
    public void EmptyCell_IsStillAnEmptyCell()
    {
        var result = Open(tiles: [new Tile(new TileId("t"), default, Cards.Blank, Cards.Blank, Cards.Blank)]);

        Rejects(result, "Tile 't' has an empty cell.");
    }

    [Test]
    public void DefaultCatalogIds_AreRejected()
    {
        Rejects(
            Open(colors: [default, OrdinaryCatalog.Purple]),
            "A color id is required.");
        Rejects(
            Open(symbols: [default]),
            "A symbol id is required.");
        Rejects(
            Open(missions:
            [
                new Mission(default, MissionPattern.Row, OrdinaryCatalog.Purple),
                Cards.Purple("a2"),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
            ]),
            "A mission id is required.");
        Rejects(
            Open(missions:
            [
                new Mission(new MissionId("a1"), MissionPattern.Row, default),
                Cards.Purple("a2"),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
            ]),
            "A color id is required.");
        Rejects(
            Open(tiles: [new Tile(default, Cards.Blank, Cards.Blank, Cards.Blank, Cards.Blank)]),
            "A tile id is required.");
    }

    [Test]
    public void UnknownMissionPattern_IsRejected()
    {
        var result = Open(missions:
        [
            new Mission(new MissionId("a1"), (MissionPattern)99, OrdinaryCatalog.Purple),
            Cards.Purple("a2"),
            Cards.Purple("b1"),
            Cards.Purple("b2"),
        ]);

        Rejects(result, "Mission 'a1' is not a row, square, or L.");
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void ClaimsRequiredBelowOne_IsRejected(int claimsRequiredToWin)
    {
        Rejects(
            Open(claimsRequiredToWin: claimsRequiredToWin),
            "Claims required to win must be at least 1.");
    }

    private static CommandResult Open(
        Mission[]? missions = null,
        Tile[]? tiles = null,
        IReadOnlyList<ColorId>? colors = null,
        IReadOnlyList<SymbolId>? symbols = null,
        int claimsRequiredToWin = OrdinaryCatalog.ClaimsRequiredToWin)
    {
        return RepresentativeDeck.Open(
            ["a", "b"],
            "a",
            missions ?? [Cards.Purple("a1"), Cards.Purple("a2"), Cards.Purple("b1"), Cards.Purple("b2")],
            tiles ?? [Cards.BlankTile("only")],
            claimsRequiredToWin,
            colors,
            symbols);
    }

    private static void Rejects(CommandResult result, string message)
    {
        Assert.That(result.IsAccepted, Is.False);
        Assert.That(result.Game, Is.Null);
        Assert.That(result.Events, Is.Empty);
        Assert.That(result.Rejection?.Reason, Is.EqualTo(RejectionReason.InvalidSetup));
        Assert.That(result.Rejection?.Message, Is.EqualTo(message));
    }
}
