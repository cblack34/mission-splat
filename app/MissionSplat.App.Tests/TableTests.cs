namespace MissionSplat.App.Tests;

using MissionSplat.App;
using MissionSplat.Rules;

public class TableTests
{
    private static readonly SeatId First = new("1");

    private static readonly SeatId Second = new("2");

    [Test]
    public void AScriptedSequence_WithOneAiSeat_AdvancesThroughTheDriverWithoutAGui()
    {
        Assert.That(RepresentativeDeck.Label, Is.EqualTo("representative"));
        var session = new LocalSession();
        var table = new Table(
            session,
            TableStart.HumanThenAi(),
            Setup(2, Cards.BlankTile("start"), Cards.Solid("red", Cards.Red), Cards.Solid("blue", Cards.Blue), Cards.Solid("green", Cards.Green)));

        table.Start();

        Assert.That(table.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.ToConfirm));
        Assert.That(table.Snapshot.Status.Seat, Is.EqualTo(First));
        var refused = table.Submit(new Place(1, 0, 0));
        Assert.That(refused.IsAccepted, Is.False, "a concealed hand takes no action");
        Assert.That(table.Snapshot.View.Board.Tiles, Has.Count.EqualTo(1));

        table.Confirm();
        Assert.That(table.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.ToAct));
        Assert.That(table.Snapshot.SecretsVisible, Is.True);
        var placed = table.Submit(new Place(1, 0, 0));

        Assert.That(placed.IsAccepted, Is.True, placed.Rejection?.Message);
        var view = table.Snapshot.View;
        Assert.That(view.CurrentSeat, Is.EqualTo(First), "the AI seat played and gave the turn back");
        Assert.That(view.Board.Tiles.Select(tile => tile.Id.Value), Is.EqualTo(new[] { "blue", "start", "red" }));
        Assert.That(view.PendingMatchTile?.Id.Value, Is.EqualTo("green"));
        Assert.That(table.Snapshot.ConcealVisible, Is.False, "the same human still holds the device after the AI turn");
        Assert.That(table.Snapshot.SecretsVisible, Is.True);
        Assert.That(table.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.ToAct));
        Assert.That(session.CurrentSeat, Is.EqualTo(First));
    }

    [Test]
    public void TwoHumanSeats_HideTheHandUntilTheIncomingSeatConfirms()
    {
        var table = new Table(
            new LocalSession(),
            Humans(2),
            Setup(2, Cards.BlankTile("start"), Cards.Solid("red", Cards.Red), Cards.Solid("blue", Cards.Blue)));

        table.Start();
        Assert.That(table.Snapshot.ConcealVisible, Is.True);
        Assert.That(table.Snapshot.SecretsVisible, Is.False);
        Assert.That(table.Snapshot.LegalPlacements, Is.Empty);
        Assert.That(table.Snapshot.LegalTargets, Is.Empty);

        table.Confirm();
        Assert.That(table.Snapshot.ConcealVisible, Is.False);
        Assert.That(table.Submit(new Place(1, 0, 0)).IsAccepted, Is.True);

        var hidden = table.Snapshot;
        Assert.That(hidden.Status.Kind, Is.EqualTo(TableStatusKind.ToConfirm));
        Assert.That(hidden.Status.Seat, Is.EqualTo(Second));
        Assert.That(hidden.ConcealVisible, Is.True);
        Assert.That(hidden.SecretsVisible, Is.False);
        Assert.That(hidden.LegalPlacements, Is.Empty);
        Assert.That(table.Submit(new Place(-1, 0, 0)).IsAccepted, Is.False);

        table.Confirm();
        Assert.That(table.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.ToAct));
        Assert.That(table.Snapshot.Status.Seat, Is.EqualTo(Second));
        Assert.That(table.Snapshot.SecretsVisible, Is.True);
        Assert.That(table.Snapshot.LegalPlacements, Is.Not.Empty);
    }

    [Test]
    public void AHumanSeatReturnedToByAnAiSeat_IsNotConcealedAgain()
    {
        var table = new Table(
            new LocalSession(),
            TableStart.HumanThenAi(),
            Setup(2, Cards.BlankTile("start"), Cards.Solid("red", Cards.Red), Cards.Solid("blue", Cards.Blue), Cards.Solid("green", Cards.Green)));
        table.Start();
        Assert.That(table.Snapshot.ConcealVisible, Is.True);
        table.Confirm();

        Assert.That(table.Submit(new Place(1, 0, 0)).IsAccepted, Is.True);

        var after = table.Snapshot;
        Assert.That(after.View.CurrentSeat, Is.EqualTo(First));
        Assert.That(after.ConcealVisible, Is.False);
        Assert.That(after.SecretsVisible, Is.True);
        Assert.That(after.Status.Kind, Is.EqualTo(TableStatusKind.ToAct));
        Assert.That(after.LegalPlacements, Is.Not.Empty);
    }

    [Test]
    public void HumanAiHuman_ConcealsOnlyWhenControlReachesADifferentHuman()
    {
        var third = new SeatId("3");
        var table = new Table(
            new LocalSession(),
            new TableStart(3, [false, true, false], 0, false),
            Setup(
                3,
                Cards.BlankTile("start"),
                Cards.Solid("red", Cards.Red),
                Cards.Solid("blue", Cards.Blue),
                Cards.Solid("green", Cards.Green),
                Cards.Solid("purple", Cards.Purple)));
        table.Start();
        table.Confirm();

        Assert.That(table.Submit(new Place(1, 0, 0)).IsAccepted, Is.True);

        Assert.That(table.Snapshot.View.CurrentSeat, Is.EqualTo(third), "the AI seat played between the two humans");
        Assert.That(table.Snapshot.ConcealVisible, Is.True, "a different human is now current");
        Assert.That(table.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.ToConfirm));
        Assert.That(table.Snapshot.SecretsVisible, Is.False);

        table.Confirm();
        Assert.That(table.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.ToAct));
        Assert.That(table.Snapshot.LegalPlacements, Is.Not.Empty);
        var spot = table.Snapshot.LegalPlacements[0];
        Assert.That(table.Submit(new Place(spot.TileX, spot.TileY, 0)).IsAccepted, Is.True);

        Assert.That(table.Snapshot.View.CurrentSeat, Is.EqualTo(First), "the AI seat played again and the first human is next");
        Assert.That(table.Snapshot.ConcealVisible, Is.True, "the device last belonged to the third seat");
        Assert.That(table.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.ToConfirm));
    }

    [Test]
    public void APowerUse_OnAHumanSeat_DoesNotConcealAgain_AndKeepsTheChosenOrientation()
    {
        var table = new Table(
            new LocalSession(),
            Humans(2),
            Setup(
                2,
                Cards.BlankTile("start"),
                Cards.Tile("spinner", Cell.Symbol(OrdinaryCatalog.Rotate), Cards.Red, Cards.Red, Cards.Red),
                Cards.Solid("blue", Cards.Blue)));
        table.Start();
        table.Confirm();
        table.SetQuarterTurns(2);
        var rotate = table.Snapshot.LegalTargets.Single(targets => targets.Power.Equals(OrdinaryCatalog.Rotate));
        Assert.That(rotate.Targets, Is.EqualTo(new[] { new BoardPosition(0, 0) }));

        var used = table.Submit(new UseRotate(0, 0, 1));

        Assert.That(used.IsAccepted, Is.True, used.Rejection?.Message);
        var after = table.Snapshot;
        Assert.That(after.ConcealVisible, Is.False);
        Assert.That(after.SecretsVisible, Is.True);
        Assert.That(after.View.CurrentSeat, Is.EqualTo(First));
        Assert.That(after.Status.Kind, Is.EqualTo(TableStatusKind.ToAct));
        Assert.That(after.QuarterTurns, Is.EqualTo(2));
        Assert.That(after.View.RemainingUses.Single(charge => charge.Power.Equals(OrdinaryCatalog.Rotate)).Remaining, Is.EqualTo(0));
        Assert.That(after.LegalTargets.Single(targets => targets.Power.Equals(OrdinaryCatalog.Rotate)).Targets, Is.Empty);

        Assert.That(table.Submit(new Place(1, 0, 2)).IsAccepted, Is.True);
        Assert.That(table.Snapshot.ConcealVisible, Is.True, "the placement passed the turn");
        Assert.That(table.Snapshot.QuarterTurns, Is.EqualTo(0));
    }

    [Test]
    public void AnUnresolvedRuling_StopsTheTable_WithTheRulesMessage()
    {
        var table = new Table(new LocalSession(), Humans(2), Setup(2, Cards.BlankTile("start")));
        table.Start();
        table.Confirm();
        Assert.That(table.Snapshot.LegalPlacements, Is.Empty, "no drawn tile means nothing to highlight");

        var result = table.Submit(new Place(1, 0, 0));

        Assert.That(result.IsAccepted, Is.False);
        var status = table.Snapshot.Status;
        Assert.That(status.Kind, Is.EqualTo(TableStatusKind.Stopped));
        Assert.That(status.Message, Does.Contain("open ruling"));
        Assert.That(result.Rejection?.Message, Is.EqualTo(status.Message));
        Assert.That(table.Snapshot.ConcealVisible, Is.False);
        Assert.That(table.Snapshot.LegalPlacements, Is.Empty);
        Assert.That(table.Submit(new Place(1, 0, 0)).IsAccepted, Is.False);
        table.SetQuarterTurns(3);
        Assert.That(table.Snapshot.QuarterTurns, Is.EqualTo(0));
    }

    [Test]
    public void AnAutomatedSeatWithNothingToPlace_StopsTheTable_WithItsMessage()
    {
        var table = new Table(
            new LocalSession(),
            TableStart.HumanThenAi(),
            Setup(2, Cards.BlankTile("start"), Cards.Solid("red", Cards.Red)));
        table.Start();
        table.Confirm();

        var placed = table.Submit(new Place(1, 0, 0));

        Assert.That(placed.IsAccepted, Is.True, placed.Rejection?.Message);
        Assert.That(table.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.Stopped));
        Assert.That(table.Snapshot.Status.Message, Is.EqualTo("No accepted placement was found."));
    }

    [Test]
    public void AutomatedSeatsThatNeverEndTheGame_StopAtTheGuard()
    {
        var tiles = new List<Tile> { Cards.BlankTile("start") };
        for (var i = 0; i < 100; i++)
        {
            tiles.Add(Cards.BlankTile("blank-" + i));
        }

        var table = new Table(
            new LocalSession(),
            new TableStart(2, [true, true], 0, false),
            Setup(2, tiles.ToArray()));

        table.Start();

        Assert.That(table.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.Stopped));
        Assert.That(table.Snapshot.Status.Message, Is.EqualTo("AI play did not return to a human seat."));
        Assert.That(table.Snapshot.View.Board.Tiles, Has.Count.EqualTo(65));
    }

    [Test]
    public void TheWinningPlacement_EndsTheTable_NamingTheWinner()
    {
        var setup = new GameSetup(
            [First, Second],
            First,
            OrdinaryCatalog.Colors,
            OrdinaryCatalog.NonScoringSymbols,
            OrdinaryCatalog.Patterns,
            1,
            [Cards.Mission("one-square", MissionPattern.Square, OrdinaryCatalog.Red), Cards.Row("one-row"), Cards.Row("two-1"), Cards.Row("two-2"), Cards.Row("spare")],
            [Cards.BlankTile("start"), Cards.Solid("red", Cards.Red), Cards.BlankTile("next")]);
        var table = new Table(new LocalSession(), Humans(2), setup);
        table.Start();
        table.Confirm();

        var won = table.Submit(new Place(1, 0, 0));

        Assert.That(won.Events.OfType<GameWon>().Single().Seat, Is.EqualTo(First));
        var snapshot = table.Snapshot;
        Assert.That(snapshot.Status.Kind, Is.EqualTo(TableStatusKind.Won));
        Assert.That(snapshot.Status.Seat, Is.EqualTo(First));
        Assert.That(snapshot.View.HasEnded, Is.True);
        Assert.That(snapshot.ConcealVisible, Is.False);
        Assert.That(snapshot.SecretsVisible, Is.False);
        Assert.That(snapshot.LegalPlacements, Is.Empty);
        Assert.That(table.Submit(new Place(-1, 0, 0)).IsAccepted, Is.False);
    }

    [Test]
    public void TheSnapshotsLegalPlacements_AreTheSessionsQuery_ForTheChosenOrientation()
    {
        var session = new LocalSession();
        var table = new Table(
            session,
            Humans(2),
            Setup(2, Cards.BlankTile("start"), Cards.Tile("lid", Cell.Symbol(OrdinaryCatalog.Stack), Cards.Red, Cards.Red, Cards.Red)));
        table.Start();
        table.Confirm();

        foreach (var quarterTurns in new[] { 0, 1, 2, 3 })
        {
            table.SetQuarterTurns(quarterTurns);

            Assert.That(table.Snapshot.QuarterTurns, Is.EqualTo(quarterTurns));
            Assert.That(table.Snapshot.LegalPlacements, Is.EqualTo(session.LegalPlacements(First, quarterTurns)));
        }

        Assert.That(table.Snapshot.LegalPlacements, Does.Contain(new LegalPlacement(0, 0, PlacementKind.OnTop)));
        Assert.That(() => table.SetQuarterTurns(4), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void TheOrdinaryDeck_StartsATable_AndTheSameSeedDealsTheSameFirstTiles()
    {
        var first = new Table(new LocalSession(), TableStart.PassAndPlayDraft(), 7);
        var again = new Table(new LocalSession(), TableStart.PassAndPlayDraft(), 7);

        first.Start();
        again.Start();

        Assert.That(first.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.ToConfirm));
        Assert.That(first.Snapshot.View.Board.Tiles[0].Id, Is.EqualTo(again.Snapshot.View.Board.Tiles[0].Id));
        Assert.That(first.Snapshot.View.PendingMatchTile?.Id, Is.EqualTo(again.Snapshot.View.PendingMatchTile?.Id));
        Assert.That(
            first.Snapshot.View.UnclaimedMissions.Select(mission => mission.Id),
            Is.EqualTo(again.Snapshot.View.UnclaimedMissions.Select(mission => mission.Id)));
    }

    [Test]
    public void ATableNotYetStarted_HasNoSnapshot_AndRefusesActions()
    {
        var table = new Table(new LocalSession(), Humans(2), Setup(2, Cards.BlankTile("start")));

        Assert.That(table.IsStarted, Is.False);
        Assert.That(() => table.Snapshot, Throws.InvalidOperationException);
        Assert.That(table.Submit(new Place(0, 0, 0)).IsAccepted, Is.False);
        table.Confirm();
        table.SetQuarterTurns(1);
        Assert.That(table.IsStarted, Is.False);
    }

    [Test]
    public void ASetupForOtherSeats_IsRefused()
    {
        var other = RepresentativeDeck.Setup(["x", "y"], "x", [Cards.Row("m1"), Cards.Row("m2"), Cards.Row("m3"), Cards.Row("m4")], [Cards.BlankTile("start")]);

        Assert.That(() => new Table(new LocalSession(), Humans(2), other), Throws.ArgumentException);
    }

    private static TableStart Humans(int seatCount) => new(seatCount, new bool[seatCount], 0, false);

    // Seats are named as TableStart names them; the missions never complete, so only the tiles drive a test.
    private static GameSetup Setup(int seatCount, params Tile[] tiles)
    {
        var seats = Enumerable.Range(1, seatCount).Select(number => number.ToString()).ToArray();
        var missions = seats.SelectMany(seat => new[] { Cards.Row(seat + "-a"), Cards.Row(seat + "-b"), Cards.Row(seat + "-replacement") }).ToList();
        missions.Add(Cards.Row("spare"));
        return RepresentativeDeck.Setup(seats, "1", missions.ToArray(), tiles);
    }
}
