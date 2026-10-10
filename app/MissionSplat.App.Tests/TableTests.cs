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
    public void APowerUse_OnAHumanSeat_DoesNotConcealAgain()
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
        Assert.That(after.QuarterTurns, Is.EqualTo(0));
        Assert.That(after.View.RemainingUses.Single(charge => charge.Power.Equals(OrdinaryCatalog.Rotate)).Remaining, Is.EqualTo(0));
        Assert.That(after.LegalTargets.Single(targets => targets.Power.Equals(OrdinaryCatalog.Rotate)).Targets, Is.Empty);

        Assert.That(table.Submit(new Place(1, 0, 2)).IsAccepted, Is.True);
        Assert.That(table.Snapshot.ConcealVisible, Is.True, "the placement passed the turn");
        Assert.That(table.Snapshot.QuarterTurns, Is.EqualTo(0));
    }

    [Test]
    public void Place_UsesTheOrientationSetByQuarterTurns()
    {
        var table = new Table(
            new LocalSession(),
            Humans(2),
            Setup(2, Cards.BlankTile("start"), Cards.Tile("lid", Cell.Symbol(OrdinaryCatalog.Stack), Cards.Red, Cards.Red, Cards.Red)));
        table.Start();
        table.Confirm();
        table.SetQuarterTurns(2);
        Assert.That(table.Snapshot.QuarterTurns, Is.EqualTo(2));

        var placed = table.Place(1, 0);

        Assert.That(placed.IsAccepted, Is.True, placed.Rejection?.Message);
        Assert.That(placed.Events.OfType<TilePlaced>().Single().QuarterTurnsClockwise, Is.EqualTo(2));
    }

    [Test]
    public void AUnshuffledStart_NeedsNoSeed_AndAShuffledOneWithoutIsRefused()
    {
        Assert.That(() => new Table(new LocalSession(), TableStart.PassAndPlayDraft()), Throws.ArgumentException);
        Assert.That(() => new Table(new LocalSession(), TableStart.HumanThenAi()), Throws.Nothing);
    }

    [Test]
    public void AnUnresolvedRulingOnSubmit_StopsTheTable_WithTheRulesMessage()
    {
        var table = new Table(
            new RulingOnSubmitSession(new LocalSession()),
            Humans(2),
            Setup(2, Cards.BlankTile("start"), Cards.Solid("red", Cards.Red), Cards.Solid("blue", Cards.Blue)));
        table.Start();
        table.Confirm();
        Assert.That(table.Snapshot.LegalPlacements, Is.Not.Empty);

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
    public void AHumanTurnWithAMixedPowerTile_StopsTheTable_WithTheRulesMessage()
    {
        var table = new Table(new LocalSession(), Humans(2), Setup(2, Cards.BlankTile("start"), MixedTile(), Cards.BlankTile("pad")));
        table.Start();
        Assert.That(table.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.ToConfirm));

        table.Confirm();

        var snapshot = table.Snapshot;
        Assert.That(snapshot.Status.Kind, Is.EqualTo(TableStatusKind.Stopped));
        Assert.That(snapshot.Status.Message, Does.StartWith("The drawn tile shows more than one power."));
        Assert.That(snapshot.Status.Message, Does.Contain("open ruling"));
        Assert.That(snapshot.SecretsVisible, Is.False);
        Assert.That(snapshot.ConcealVisible, Is.False);
        Assert.That(snapshot.LegalPlacements, Is.Empty);
        Assert.That(snapshot.LegalTargets, Is.Empty);
        Assert.That(table.Submit(new Place(1, 0, 0)).IsAccepted, Is.False);
    }

    [Test]
    public void AnAiTurnWithAMixedPowerTile_StopsTheTable_WithTheRulesMessageNotTheAisOwn()
    {
        var table = new Table(
            new LocalSession(),
            TableStart.HumanThenAi(),
            Setup(2, Cards.BlankTile("start"), Cards.Solid("red", Cards.Red), MixedTile(), Cards.BlankTile("pad")));
        table.Start();
        table.Confirm();

        var placed = table.Submit(new Place(1, 0, 0));

        Assert.That(placed.IsAccepted, Is.True, placed.Rejection?.Message);
        Assert.That(table.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.Stopped));
        Assert.That(table.Snapshot.Status.Message, Does.StartWith("The drawn tile shows more than one power."));
        Assert.That(table.Snapshot.View.Board.Tiles, Has.Count.EqualTo(2), "the AI placed nothing");
    }

    [Test]
    public void AHumanTurnWithTheMatchDeckExhausted_StopsTheTable_WithTheRulesMessage()
    {
        var table = new Table(new LocalSession(), Humans(2), Setup(2, Cards.BlankTile("start")));
        table.Start();
        table.Confirm();

        var status = table.Snapshot.Status;
        Assert.That(status.Kind, Is.EqualTo(TableStatusKind.Stopped));
        Assert.That(status.Message, Does.StartWith("The match deck has no tile to place."));
        Assert.That(table.Snapshot.LegalPlacements, Is.Empty);
        Assert.That(table.Snapshot.ConcealVisible, Is.False);
    }

    [Test]
    public void AnAutomatedSeatWithTheMatchDeckExhausted_StopsTheTable_WithTheRulesMessage()
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
        Assert.That(table.Snapshot.Status.Message, Does.StartWith("The match deck has no tile to place."));
    }

    [Test]
    public void TheSnapshot_IsNotChangedByEditsToTheListsItWasBuiltFrom()
    {
        var session = new LocalSession();
        session.Start(Setup(2, Cards.BlankTile("start"), Cards.BlankTile("drawn")));
        var view = session.View(First);
        var targets = new List<BoardPosition> { new(0, 0) };
        var powers = new List<PowerTargets> { new(OrdinaryCatalog.Bounce, targets) };
        var snapshot = new TableSnapshot(view, false, false, false, [], powers, 0, TableStatus.ToAct(First), null);

        targets.Add(new BoardPosition(5, 5));
        powers.Add(new PowerTargets(OrdinaryCatalog.Rotate, []));

        Assert.That(snapshot.LegalTargets, Has.Count.EqualTo(1));
        Assert.That(snapshot.LegalTargets[0].Targets, Is.EqualTo(new[] { new BoardPosition(0, 0) }));
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

    [Test]
    public void SelectingBounce_ListsItsTargetsAndEmptiesPlacements_AndUseAtBouncesTheTarget()
    {
        var table = BounceTableAtTheSecondSeat();
        var before = table.Snapshot;
        Assert.That(before.SelectedPower, Is.Null);
        Assert.That(before.LegalPlacements, Is.Not.Empty);

        table.SelectPower(OrdinaryCatalog.Bounce);

        var selected = table.Snapshot;
        Assert.That(selected.SelectedPower, Is.EqualTo(OrdinaryCatalog.Bounce));
        Assert.That(selected.LegalPlacements, Is.Empty);
        var targets = selected.LegalTargets.Single(entry => entry.Power.Equals(OrdinaryCatalog.Bounce)).Targets;
        Assert.That(targets, Does.Contain(new BoardPosition(1, 0)));

        var bounced = table.UseAt(1, 0);

        Assert.That(bounced.IsAccepted, Is.True, bounced.Rejection?.Message);
        Assert.That(bounced.Events.OfType<TileBounced>().Single().Removed.Value, Is.EqualTo("side"));
        var after = table.Snapshot;
        Assert.That(after.View.Board.Tiles, Has.Count.EqualTo(before.View.Board.Tiles.Count - 1));
        Assert.That(after.View.CurrentSeat, Is.EqualTo(Second));
        Assert.That(after.ConcealVisible, Is.False);
        Assert.That(after.SelectedPower, Is.Null, "the last use is spent");
        Assert.That(after.LegalPlacements, Is.Not.Empty);
    }

    [Test]
    public void SelectingRotate_AndUseAt_TurnsTheTargetByTheQuarterTurnsChosen()
    {
        var table = RotateTable();
        var before = CellsOf(table.Snapshot.View);
        table.SelectPower(OrdinaryCatalog.Rotate);
        table.SetQuarterTurns(2);
        Assert.That(table.Snapshot.LegalPlacements, Is.Empty);
        Assert.That(
            table.Snapshot.LegalTargets.Single(entry => entry.Power.Equals(OrdinaryCatalog.Rotate)).Targets,
            Is.EqualTo(new[] { new BoardPosition(0, 0) }));

        var rotated = table.UseAt(0, 0);

        Assert.That(rotated.IsAccepted, Is.True, rotated.Rejection?.Message);
        Assert.That(rotated.Events, Is.EqualTo(new GameEvent[] { new TileRotated(First, 0, 0, 2) }));
        Assert.That(CellsOf(table.Snapshot.View), Is.Not.EqualTo(before));
        Assert.That(table.Snapshot.View.CurrentSeat, Is.EqualTo(First));
        Assert.That(table.Snapshot.SelectedPower, Is.Null);
        Assert.That(table.Snapshot.LegalPlacements, Is.Not.Empty);
    }

    [Test]
    public void AnAcceptedRotate_DoesNotLeakItsAmountIntoThePlacementOrientation()
    {
        var table = RotateTable();
        table.SelectPower(OrdinaryCatalog.Rotate);
        table.SetQuarterTurns(2);

        var rotated = table.UseAt(0, 0);

        Assert.That(rotated.IsAccepted, Is.True, rotated.Rejection?.Message);
        Assert.That(table.Snapshot.QuarterTurns, Is.EqualTo(0));
    }

    [Test]
    public void TheSnapshot_ReportsTheSelectedPowersTargets_AndWhetherTheSeatCanAct()
    {
        var table = BounceTableAtTheSecondSeat();
        Assert.That(table.Snapshot.CanAct, Is.True);
        Assert.That(table.Snapshot.SelectedTargets, Is.Empty);

        table.SelectPower(OrdinaryCatalog.Bounce);

        Assert.That(
            table.Snapshot.SelectedTargets,
            Is.EqualTo(table.Snapshot.LegalTargets.Single(entry => entry.Power.Equals(OrdinaryCatalog.Bounce)).Targets));
        Assert.That(table.Snapshot.SelectedTargets, Is.Not.Empty);
    }

    [Test]
    public void UseAtWithRotateAndNoQuarterTurns_IsRefusedByRules_AndLeavesTheGameAndTheSelection()
    {
        var table = RotateTable();
        var before = CellsOf(table.Snapshot.View);
        table.SelectPower(OrdinaryCatalog.Rotate);
        Assert.That(table.Snapshot.QuarterTurns, Is.EqualTo(0));

        var refused = table.UseAt(0, 0);

        Assert.That(refused.IsAccepted, Is.False);
        Assert.That(refused.Rejection?.RulesRejection?.Reason, Is.EqualTo(RejectionReason.InvalidRotateQuarterTurns));
        Assert.That(CellsOf(table.Snapshot.View), Is.EqualTo(before));
        Assert.That(table.Snapshot.View.RemainingUses.Single(charge => charge.Power.Equals(OrdinaryCatalog.Rotate)).Remaining, Is.EqualTo(1));
        Assert.That(table.Snapshot.SelectedPower, Is.EqualTo(OrdinaryCatalog.Rotate));
    }

    [Test]
    public void SelectPower_IsIgnoredForAPowerWithNoUsesRemaining_AndWhileConcealed()
    {
        var table = RotateTable();
        table.SelectPower(OrdinaryCatalog.Bounce);
        Assert.That(table.Snapshot.SelectedPower, Is.Null, "the drawn tile has no bounce cell");
        Assert.That(table.Snapshot.LegalPlacements, Is.Not.Empty);

        table.SelectPower(OrdinaryCatalog.Rotate);
        table.SelectPower(OrdinaryCatalog.Bounce);
        Assert.That(table.Snapshot.SelectedPower, Is.EqualTo(OrdinaryCatalog.Rotate), "an unusable choice leaves the selection as it was");
        table.SelectPower(null);
        Assert.That(table.Snapshot.SelectedPower, Is.Null);
        Assert.That(table.Snapshot.LegalPlacements, Is.Not.Empty);

        var concealed = new Table(new LocalSession(), Humans(2), SpinnerSetup());
        concealed.Start();
        concealed.SelectPower(OrdinaryCatalog.Rotate);
        concealed.Confirm();
        Assert.That(concealed.Snapshot.SelectedPower, Is.Null);
    }

    [Test]
    public void AnAcceptedPlace_ClearsTheSelection_EvenWhenTheSameHumanDrawsAnotherPowerTile()
    {
        var table = new Table(
            new LocalSession(),
            TableStart.HumanThenAi(),
            Setup(2, StartTile(), Spinner("first"), Cards.Solid("ai", Cards.Blue), Spinner("second"), Cards.Solid("spare", Cards.Green)));
        table.Start();
        table.Confirm();
        table.SelectPower(OrdinaryCatalog.Rotate);
        Assert.That(table.Snapshot.SelectedPower, Is.EqualTo(OrdinaryCatalog.Rotate));

        var placed = table.Place(1, 0);

        Assert.That(placed.IsAccepted, Is.True, placed.Rejection?.Message);
        Assert.That(table.Snapshot.View.CurrentSeat, Is.EqualTo(First));
        Assert.That(table.Snapshot.View.RemainingUses.Single(charge => charge.Power.Equals(OrdinaryCatalog.Rotate)).Remaining, Is.EqualTo(1));
        Assert.That(table.Snapshot.SelectedPower, Is.Null);
        Assert.That(table.Snapshot.LegalPlacements, Is.Not.Empty);
    }

    [Test]
    public void AStop_ClearsTheSelection()
    {
        var table = new Table(new RulingOnSubmitSession(new LocalSession()), Humans(2), SpinnerSetup());
        table.Start();
        table.Confirm();
        table.SelectPower(OrdinaryCatalog.Rotate);
        table.SetQuarterTurns(1);

        var result = table.UseAt(0, 0);

        Assert.That(result.IsAccepted, Is.False);
        Assert.That(table.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.Stopped));
        Assert.That(table.Snapshot.SelectedPower, Is.Null);
    }

    [Test]
    public void UseAtWithNothingSelected_IsRefusedLikeAnyTapWhileNobodyMayAct()
    {
        var table = RotateTable();

        var refused = table.UseAt(0, 0);

        Assert.That(refused.IsAccepted, Is.False);
        Assert.That(refused.Rejection?.Message, Is.EqualTo("The table is not taking an action."));
        Assert.That(refused.Rejection?.RulesRejection, Is.Null);
        Assert.That(table.Snapshot.View.RemainingUses.Single(charge => charge.Power.Equals(OrdinaryCatalog.Rotate)).Remaining, Is.EqualTo(1));
    }

    [Test]
    public void ATileWithTwoBounceCells_KeepsTheSelectionAndRefreshesTheTargets_UntilTheLastUse()
    {
        var table = BounceTableAtTheSecondSeat(TwoBounceTile());
        table.SelectPower(OrdinaryCatalog.Bounce);
        var targets = table.Snapshot.SelectedTargets;
        Assert.That(targets, Does.Contain(new BoardPosition(1, 0)));
        Assert.That(targets, Does.Contain(new BoardPosition(0, 0)));

        var first = table.UseAt(1, 0);

        Assert.That(first.IsAccepted, Is.True, first.Rejection?.Message);
        var between = table.Snapshot;
        Assert.That(between.SelectedPower, Is.EqualTo(OrdinaryCatalog.Bounce), "a use remains, so the selection stays");
        Assert.That(between.View.CurrentSeat, Is.EqualTo(Second));
        Assert.That(between.View.RemainingUses.Single(charge => charge.Power.Equals(OrdinaryCatalog.Bounce)).Remaining, Is.EqualTo(1));
        Assert.That(between.SelectedTargets, Does.Not.Contain(new BoardPosition(1, 0)));
        Assert.That(between.SelectedTargets, Is.EqualTo(new[] { new BoardPosition(0, 0) }));
        Assert.That(between.LegalPlacements, Is.Empty, "a selected power shows targets, not placements");

        var second = table.UseAt(0, 0);

        Assert.That(second.IsAccepted, Is.True, second.Rejection?.Message);
        var after = table.Snapshot;
        Assert.That(after.View.Board.Tiles, Is.Empty);
        Assert.That(after.SelectedPower, Is.Null, "the last use is spent");
        Assert.That(after.LegalPlacements, Is.Not.Empty);
    }

    [Test]
    public void ATileWithTwoRotateCells_KeepsTheSelection_AndEachUseTakesItsOwnQuarterTurns()
    {
        var table = new Table(new LocalSession(), Humans(2), Setup(2, StartTile(), TwoRotateTile(), Cards.Solid("blue", Cards.Blue)));
        table.Start();
        table.Confirm();
        var original = CellsOf(table.Snapshot.View);
        table.SelectPower(OrdinaryCatalog.Rotate);
        table.SetQuarterTurns(1);

        var first = table.UseAt(0, 0);

        Assert.That(first.IsAccepted, Is.True, first.Rejection?.Message);
        Assert.That(first.Events, Is.EqualTo(new GameEvent[] { new TileRotated(First, 0, 0, 1) }));
        var between = table.Snapshot;
        var turnedOnce = CellsOf(between.View);
        Assert.That(turnedOnce, Is.Not.EqualTo(original));
        Assert.That(between.SelectedPower, Is.EqualTo(OrdinaryCatalog.Rotate), "a use remains, so the selection stays");
        Assert.That(between.QuarterTurns, Is.EqualTo(0), "the amount a use spent is not carried to the next");
        Assert.That(between.View.RemainingUses.Single(charge => charge.Power.Equals(OrdinaryCatalog.Rotate)).Remaining, Is.EqualTo(1));
        Assert.That(between.LegalPlacements, Is.Empty);

        table.SetQuarterTurns(3);
        var second = table.UseAt(0, 0);

        Assert.That(second.IsAccepted, Is.True, second.Rejection?.Message);
        Assert.That(second.Events, Is.EqualTo(new GameEvent[] { new TileRotated(First, 0, 0, 3) }));
        var after = table.Snapshot;
        Assert.That(CellsOf(after.View), Is.EqualTo(original), "one and three quarter-turns make a full turn");
        Assert.That(after.SelectedPower, Is.Null, "the last use is spent");
        Assert.That(after.QuarterTurns, Is.EqualTo(0));
        Assert.That(after.LegalPlacements, Is.Not.Empty);
    }

    private static Tile StartTile() => Cards.Tile("start", Cards.Red, Cards.Blue, Cards.Green, Cards.Purple);

    private static Tile Spinner(string id) =>
        Cards.Tile(id, Cell.Symbol(OrdinaryCatalog.Rotate), Cards.Red, Cards.Red, Cards.Red);

    private static Tile TwoRotateTile() =>
        Cards.Tile("two-spinner", Cell.Symbol(OrdinaryCatalog.Rotate), Cell.Symbol(OrdinaryCatalog.Rotate), Cards.Red, Cards.Red);

    private static Tile TwoBounceTile() =>
        Cards.Tile("two-lifter", Cell.Symbol(OrdinaryCatalog.Bounce), Cell.Symbol(OrdinaryCatalog.Bounce), Cards.Red, Cards.Red);

    private static GameSetup SpinnerSetup() => Setup(2, StartTile(), Spinner("spinner"), Cards.Solid("blue", Cards.Blue));

    // The first seat holds a drawn tile with one rotate cell, over a start tile whose turning shows.
    private static Table RotateTable()
    {
        var table = new Table(new LocalSession(), Humans(2), SpinnerSetup());
        table.Start();
        table.Confirm();
        return table;
    }

    // The second seat holds a drawn tile with one bounce cell, beside a tile the first seat placed.
    private static Table BounceTableAtTheSecondSeat() =>
        BounceTableAtTheSecondSeat(Cards.Tile("lifter", Cell.Symbol(OrdinaryCatalog.Bounce), Cards.Red, Cards.Red, Cards.Red));

    private static Table BounceTableAtTheSecondSeat(Tile lifter)
    {
        var table = new Table(
            new LocalSession(),
            Humans(2),
            Setup(
                2,
                Cards.BlankTile("start"),
                Cards.Solid("side", Cards.Red),
                lifter,
                Cards.Solid("next", Cards.Blue)));
        table.Start();
        table.Confirm();
        Assert.That(table.Submit(new Place(1, 0, 0)).IsAccepted, Is.True);
        table.Confirm();
        return table;
    }

    private static string CellsOf(SeatView view) =>
        string.Join(";", view.Board.Cells.Select(cell => cell.CellX + "," + cell.CellY + ":" + cell.Value));

    private static Tile MixedTile() =>
        Cards.Tile("mixed", Cell.Symbol(OrdinaryCatalog.Rotate), Cell.Symbol(OrdinaryCatalog.Bounce), Cards.Blank, Cards.Blank);

    private static TableStart Humans(int seatCount) => new(seatCount, new bool[seatCount], 0, false);

    // Seats are named as TableStart names them; the missions never complete, so only the tiles drive a test.
    private static GameSetup Setup(int seatCount, params Tile[] tiles)
    {
        var seats = Enumerable.Range(1, seatCount).Select(number => number.ToString()).ToArray();
        var missions = seats.SelectMany(seat => new[] { Cards.Row(seat + "-a"), Cards.Row(seat + "-b"), Cards.Row(seat + "-replacement") }).ToList();
        missions.Add(Cards.Row("spare"));
        return RepresentativeDeck.Setup(seats, "1", missions.ToArray(), tiles);
    }

    // The session as LocalSession answers it, except that a command reaches an open ruling.
    private sealed class RulingOnSubmitSession(ISession inner) : ISession
    {
        public SeatId CurrentSeat => inner.CurrentSeat;

        public IReadOnlyList<SymbolId> PowersInPlay => inner.PowersInPlay;

        public SessionResult Start(GameSetup setup) => inner.Start(setup);

        public SeatView View(SeatId seat) => inner.View(seat);

        public SessionResult Submit(SeatId seat, GameAction action) =>
            throw new UnresolvedRulingException("An open ruling applies here, so this command was not applied.");

        public IReadOnlyList<LegalPlacement> LegalPlacements(SeatId seat, int quarterTurnsClockwise) =>
            inner.LegalPlacements(seat, quarterTurnsClockwise);

        public IReadOnlyList<BoardPosition> LegalTargets(SeatId seat, SymbolId power) => inner.LegalTargets(seat, power);

        public ActionPreview Preview(SeatId seat, GameAction action) => inner.Preview(seat, action);
    }
}
