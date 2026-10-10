namespace MissionSplat.Player.Tests
{

using System.Linq;
using MissionSplat.App;
using MissionSplat.Player;
using MissionSplat.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class TableSessionPlayModeTests
{
    private GameObject _root;

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
        {
            Object.DestroyImmediate(_root);
        }
    }

    [Test]
    public void ScriptedHumanPlacement_ThenOneAiTurn_PlacesThroughLocalSession()
    {
        Assert.That(OrdinaryCatalog.ClaimsRequiredToWin, Is.EqualTo(4));

        _root = new GameObject("Table");
        var table = _root.AddComponent<TableSession>();
        table.Begin(TableStart.HumanThenAi());

        Assert.That(table.IsStarted, Is.True);
        Assert.That(table.Snapshot.View.CurrentSeat.Value, Is.EqualTo("1"));
        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(1));
        Assert.That(table.Snapshot.ConcealVisible, Is.True);
        Assert.That(table.Snapshot.SecretsVisible, Is.False);
        Assert.That(table.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.ToConfirm));
        Assert.That(table.Snapshot.LegalPlacements, Is.Empty);

        table.ConfirmIncomingSeat();

        var placements = table.Snapshot.LegalPlacements;
        Assert.That(table.Snapshot.ConcealVisible, Is.False);
        Assert.That(table.Snapshot.SecretsVisible, Is.True);
        Assert.That(table.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.ToAct));
        Assert.That(table.Snapshot.View.UnclaimedMissions.Count, Is.EqualTo(2));
        Assert.That(placements, Is.Not.Empty);
        Assert.That(placements.Any(slot => slot.TileX == 1 && slot.TileY == 1), Is.False);
        Assert.That(placements.Any(slot => slot.TileX == 1 && slot.TileY == 0), Is.True);

        table.Tap(1, 0);

        var view = table.Snapshot.View;
        Assert.That(view.Board.Tiles.Count, Is.EqualTo(3));
        Assert.That(view.CurrentSeat.Value, Is.EqualTo("1"));
        Assert.That(view.HasEnded, Is.False);
        Assert.That(view.Claims.Single(row => row.Seat.Value == "1").Missions.Count, Is.EqualTo(1));
        Assert.That(view.UnclaimedMissions.Count, Is.EqualTo(2));
        // The same human holds the device after an AI turn, so the hand stays visible.
        Assert.That(table.Snapshot.ConcealVisible, Is.False);
        Assert.That(table.Snapshot.SecretsVisible, Is.True);
        Assert.That(table.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.ToAct));
    }

    [Test]
    public void StackTile_OffersAnOnTopPlacement_AndTappingItCoversWithoutAddingATile()
    {
        _root = new GameObject("Table");
        var table = _root.AddComponent<TableSession>();
        table.Begin(TableStart.PassAndPlayDraft(), StackSetup());
        table.ConfirmIncomingSeat();

        var before = table.Snapshot.View.Board;
        var onTop = table.Snapshot.LegalPlacements.Where(slot => slot.Kind == PlacementKind.OnTop).ToArray();
        Assert.That(onTop, Is.EqualTo(new[] { new LegalPlacement(0, 0, PlacementKind.OnTop) }));
        Assert.That(table.Snapshot.LegalPlacements.Any(slot => slot.Kind == PlacementKind.Beside), Is.True);
        Assert.That(before.Tiles.Count, Is.EqualTo(1));
        Assert.That(before.Cells.All(cell => cell.Value.Equals(Cell.Symbol(OrdinaryCatalog.Blank))), Is.True);

        FindButton("Highlight0_0").onClick.Invoke();

        var after = table.Snapshot.View.Board;
        Assert.That(after.Tiles.Count, Is.EqualTo(1));
        Assert.That(after.Tiles[0].Id.Value, Is.EqualTo("lid"));
        Assert.That(after.Cells.Any(cell => cell.Value.Equals(Cell.Color(OrdinaryCatalog.Red))), Is.True);
        Assert.That(table.Snapshot.View.CurrentSeat.Value, Is.EqualTo("2"));
        Assert.That(table.Snapshot.ConcealVisible, Is.True);
    }

    [Test]
    public void BouncingTheLastTile_StillRendersTheOriginPlacement()
    {
        _root = new GameObject("Table");
        var table = _root.AddComponent<TableSession>();
        table.Begin(TableStart.PassAndPlayDraft(), BounceSetup());
        table.ConfirmIncomingSeat();

        table.Submit(new UseBounce(0, 0));

        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(0));
        Assert.That(table.Snapshot.LegalPlacements, Is.EqualTo(new[] { new LegalPlacement(0, 0, PlacementKind.Beside) }));
        var origin = FindButton("Highlight0_0");
        Assert.That(origin, Is.Not.Null);
        Assert.That(origin.transform.parent.name, Is.EqualTo("Lattice"));

        origin.onClick.Invoke();

        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(1));
    }

    [Test]
    public void InteractiveLayoutsSitInsideTheReportedSafeArea()
    {
        _root = new GameObject("Table");
        _root.AddComponent<TableSession>();

        var canvas = FindRect("Canvas");
        var safe = FindRect("SafeArea");
        var play = FindRect("Play");
        var setup = FindRect("Setup");
        var background = FindRect("Background");
        Assert.That(canvas, Is.Not.Null);
        Assert.That(safe, Is.Not.Null);
        Assert.That(play, Is.Not.Null);
        Assert.That(setup, Is.Not.Null);
        Assert.That(background, Is.Not.Null);
        Assert.That(play.transform.parent, Is.SameAs(safe.transform));
        Assert.That(setup.transform.parent, Is.SameAs(safe.transform));
        Assert.That(background.transform.parent, Is.SameAs(canvas.transform));

        var width = Screen.width;
        var height = Screen.height;
        var area = Screen.safeArea;
        if (width <= 0 || height <= 0 || area.width <= 0f || area.height <= 0f)
        {
            Assert.That(safe.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(safe.anchorMax, Is.EqualTo(Vector2.one));
            return;
        }

        var min = new Vector2(Mathf.Clamp01(area.xMin / width), Mathf.Clamp01(area.yMin / height));
        var max = new Vector2(Mathf.Clamp01(area.xMax / width), Mathf.Clamp01(area.yMax / height));
        if (max.x <= min.x || max.y <= min.y)
        {
            min = Vector2.zero;
            max = Vector2.one;
        }

        Assert.That(safe.anchorMin.x, Is.EqualTo(min.x).Within(0.0001f));
        Assert.That(safe.anchorMin.y, Is.EqualTo(min.y).Within(0.0001f));
        Assert.That(safe.anchorMax.x, Is.EqualTo(max.x).Within(0.0001f));
        Assert.That(safe.anchorMax.y, Is.EqualTo(max.y).Within(0.0001f));
        Assert.That(safe.offsetMin, Is.EqualTo(Vector2.zero));
        Assert.That(safe.offsetMax, Is.EqualTo(Vector2.zero));
    }

    [Test]
    public void StatusStopTextWrapsInsideItsRect()
    {
        _root = new GameObject("Table");
        _root.AddComponent<TableSession>();

        var status = FindText("Status");
        var confirm = FindRect("Confirm");
        Assert.That(status, Is.Not.Null);
        Assert.That(confirm, Is.Not.Null);
        Assert.That(status.horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Wrap));
        Assert.That(confirm.GetComponentInChildren<Text>(true).horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Overflow));

        status.transform.parent.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        Assert.That(status.rectTransform.rect.height, Is.GreaterThan(status.fontSize));
    }

    [Test]
    public void RuntimeInputModule_BindsPointClickAndSubmit()
    {
        _root = new GameObject("Table");
        _root.AddComponent<TableSession>();

        var module = _root.GetComponentInChildren<InputSystemUIInputModule>(true);
        Assert.That(module, Is.Not.Null);
        Assert.That(module.point?.action, Is.Not.Null);
        Assert.That(module.leftClick?.action, Is.Not.Null);
        Assert.That(module.submit?.action, Is.Not.Null);
        Assert.That(module.point.action.bindings.Any(binding => binding.path.Contains("Touchscreen")), Is.True);
        Assert.That(module.leftClick.action.bindings.Any(binding => binding.path.Contains("Touchscreen")), Is.True);
    }

    [Test]
    public void AHumanTurnWithAMixedPowerTile_ShowsTheRulesMessage_AndOffersNothingToTap()
    {
        _root = new GameObject("Table");
        var table = _root.AddComponent<TableSession>();
        table.Begin(TableStart.PassAndPlayDraft(), MixedSetup());
        table.ConfirmIncomingSeat();

        var snapshot = table.Snapshot;
        Assert.That(snapshot.Status.Kind, Is.EqualTo(TableStatusKind.Stopped));
        Assert.That(snapshot.Status.Message, Does.Contain("open ruling"));
        Assert.That(FindText("Status").text, Is.EqualTo(snapshot.Status.Message));
        var lattice = FindRect("Lattice");
        Assert.That(lattice, Is.Not.Null);
        Assert.That(lattice.GetComponentsInChildren<Button>(true).Any(button => button.name.StartsWith("Highlight")), Is.False);
        foreach (var turn in new[] { "Turn0", "Turn1", "Turn2", "Turn3" })
        {
            var button = FindButton(turn);
            Assert.That(button, Is.Not.Null, turn);
            Assert.That(button.interactable, Is.False, turn);
        }
    }

    [Test]
    public void SelectingRotate_TurnsTheTappedTile_AndKeepsTheDrawnTileToPlace()
    {
        var table = BeginHumanTable(RotateSetup());
        var before = CellsOf(table.Snapshot.View);
        Assert.That(LiveButtons("Highlight"), Is.Not.Empty);

        LiveButton("PowerRotate").onClick.Invoke();

        Assert.That(table.Snapshot.SelectedPower, Is.EqualTo(OrdinaryCatalog.Rotate));
        Assert.That(LiveButtons("Highlight"), Is.Empty);
        Assert.That(LiveButton("Target0_0"), Is.Not.Null);
        Assert.That(LiveButton("Target0_0").interactable, Is.False, "a rotate target waits for a 1–3 amount");
        Assert.That(LiveButton("Turn0").interactable, Is.False);
        Assert.That(LiveButton("Turn2").interactable, Is.True);
        Assert.That(FindText("Status").text, Does.Contain("rotate"));

        LiveButton("Turn2").onClick.Invoke();
        Assert.That(LiveButton("Target0_0").interactable, Is.True);
        LiveButton("Target0_0").onClick.Invoke();

        var snapshot = table.Snapshot;
        Assert.That(CellsOf(snapshot.View), Is.Not.EqualTo(before));
        Assert.That(snapshot.View.PendingMatchTile, Is.Not.Null);
        Assert.That(snapshot.View.CurrentSeat.Value, Is.EqualTo("1"));
        Assert.That(snapshot.SelectedPower, Is.Null);
        Assert.That(ButtonCaption("PowerRotate"), Does.Contain("0 left"));
        Assert.That(LiveButton("PowerRotate").interactable, Is.False);
        Assert.That(LiveButtons("Target"), Is.Empty);
        Assert.That(LiveButtons("Highlight"), Is.Not.Empty);
    }

    [Test]
    public void SelectingBounce_RemovesTheTappedTile_ThenAHighlightPlacesTheDrawnTile()
    {
        var table = BeginHumanTable(BounceBoardSetup());
        LiveButton("Highlight1_0").onClick.Invoke();
        table.ConfirmIncomingSeat();
        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(2));
        Assert.That(table.Snapshot.View.CurrentSeat.Value, Is.EqualTo("2"));

        LiveButton("PowerBounce").onClick.Invoke();

        Assert.That(table.Snapshot.SelectedPower, Is.EqualTo(OrdinaryCatalog.Bounce));
        Assert.That(LiveButton("Turn0").interactable, Is.True);
        Assert.That(FindText("Status").text, Does.Contain("bounce"));

        // The lifter's bounce cell is local (0, 0); a quarter-turn moves it up the pending tile's lattice, so bounce still previews the turn.
        Assert.That(PendingCellPosition("C00"), Is.EqualTo(Vector2.zero));
        LiveButton("Turn1").onClick.Invoke();
        Assert.That(PendingCellPosition("C00").x, Is.EqualTo(0f));
        Assert.That(PendingCellPosition("C00").y, Is.GreaterThan(0f));
        LiveButton("Turn0").onClick.Invoke();
        Assert.That(PendingCellPosition("C00"), Is.EqualTo(Vector2.zero));

        LiveButton("Target1_0").onClick.Invoke();

        var snapshot = table.Snapshot;
        Assert.That(snapshot.View.Board.Tiles.Count, Is.EqualTo(1));
        Assert.That(snapshot.View.PendingMatchTile, Is.Not.Null);
        Assert.That(snapshot.View.CurrentSeat.Value, Is.EqualTo("2"));
        Assert.That(snapshot.SelectedPower, Is.Null);
        Assert.That(ButtonCaption("PowerBounce"), Does.Contain("0 left"));

        LiveButtons("Highlight").First().onClick.Invoke();

        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(2));
        Assert.That(table.Snapshot.View.CurrentSeat.Value, Is.EqualTo("1"));
    }

    [Test]
    public void PlaceInstead_AfterSelectingAPower_ReturnsThePlacementHighlights()
    {
        var table = BeginHumanTable(RotateSetup());
        Assert.That(LiveButton("PlaceInstead"), Is.Null);

        LiveButton("PowerRotate").onClick.Invoke();
        Assert.That(LiveButtons("Highlight"), Is.Empty);

        LiveButton("PlaceInstead").onClick.Invoke();

        Assert.That(table.Snapshot.SelectedPower, Is.Null);
        Assert.That(LiveButtons("Highlight"), Is.Not.Empty);
        Assert.That(LiveButtons("Target"), Is.Empty);
        Assert.That(LiveButton("PlaceInstead"), Is.Null);
        Assert.That(ButtonCaption("PowerRotate"), Does.Contain("1 left"));
    }

    [Test]
    public void TappingTheSelectedPowerAgain_Deselects()
    {
        var table = BeginHumanTable(RotateSetup());

        LiveButton("PowerRotate").onClick.Invoke();
        LiveButton("PowerRotate").onClick.Invoke();

        Assert.That(table.Snapshot.SelectedPower, Is.Null);
        Assert.That(LiveButtons("Highlight"), Is.Not.Empty);
    }

    private TableSession BeginHumanTable(GameSetup setup)
    {
        _root = new GameObject("Table");
        var table = _root.AddComponent<TableSession>();
        table.Begin(TableStart.PassAndPlayDraft(), setup);
        table.ConfirmIncomingSeat();
        return table;
    }

    // An asymmetric start tile under a drawn tile with one rotate cell, so a turn shows in the cells.
    private static GameSetup RotateSetup() => SetupFrom(
        new Tile(new TileId("start"), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Blue), Cell.Color(OrdinaryCatalog.Green), Cell.Color(OrdinaryCatalog.Purple)),
        new Tile(new TileId("spinner"), Cell.Symbol(OrdinaryCatalog.Rotate), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Red)),
        BlankTile("pad"));

    // The first seat places a plain tile beside the start; the second seat then draws the tile with a bounce cell.
    private static GameSetup BounceBoardSetup() => SetupFrom(
        BlankTile("start"),
        new Tile(new TileId("side"), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Red)),
        new Tile(new TileId("lifter"), Cell.Symbol(OrdinaryCatalog.Bounce), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Red)),
        BlankTile("pad"));

    private static Tile BlankTile(string id)
    {
        var blank = Cell.Symbol(OrdinaryCatalog.Blank);
        return new Tile(new TileId(id), blank, blank, blank, blank);
    }

    private static string CellsOf(SeatView view) =>
        string.Join(";", view.Board.Cells.Select(cell => cell.CellX + "," + cell.CellY + ":" + cell.Value));

    private static GameSetup StackSetup() => SetupWith(
        new Tile(new TileId("lid"), Cell.Symbol(OrdinaryCatalog.Stack), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Blue), Cell.Symbol(OrdinaryCatalog.Blank)));

    private static GameSetup BounceSetup() => SetupWith(
        new Tile(new TileId("lid"), Cell.Symbol(OrdinaryCatalog.Bounce), Cell.Symbol(OrdinaryCatalog.Blank), Cell.Symbol(OrdinaryCatalog.Blank), Cell.Symbol(OrdinaryCatalog.Blank)));

    private static GameSetup MixedSetup() => SetupWith(
        new Tile(new TileId("mixed"), Cell.Symbol(OrdinaryCatalog.Rotate), Cell.Symbol(OrdinaryCatalog.Bounce), Cell.Symbol(OrdinaryCatalog.Blank), Cell.Symbol(OrdinaryCatalog.Blank)));

    private static GameSetup SetupWith(Tile drawn) => SetupFrom(BlankTile("start"), drawn, BlankTile("pad"));

    private static GameSetup SetupFrom(params Tile[] tiles)
    {
        var missions = new[] { "a1", "a2", "b1", "b2", "spare" }
            .Select(id => new Mission(new MissionId(id), MissionPattern.Row, OrdinaryCatalog.Purple))
            .ToArray();
        var seats = TableStart.PassAndPlayDraft().SeatsInTurnOrder();
        return new GameSetup(
            seats,
            seats[0],
            OrdinaryCatalog.Colors,
            OrdinaryCatalog.NonScoringSymbols,
            OrdinaryCatalog.Patterns,
            OrdinaryCatalog.ClaimsRequiredToWin,
            missions,
            tiles);
    }

    // The live (not yet destroyed) cell of the pending tile's lattice.
    private Vector2 PendingCellPosition(string cell) =>
        _root.GetComponentsInChildren<RectTransform>(false)
            .First(rect => rect.name == cell && IsUnder(rect, "Pending"))
            .anchoredPosition;

    private static bool IsUnder(Transform node, string ancestor)
    {
        for (var parent = node.parent; parent != null; parent = parent.parent)
        {
            if (parent.name == ancestor)
            {
                return true;
            }
        }

        return false;
    }

    private RectTransform FindRect(string name)
    {
        foreach (var rect in _root.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect.name == name)
            {
                return rect;
            }
        }

        return null;
    }

    // A re-render leaves the replaced buttons inactive until the frame ends, so only live ones count.
    private Button LiveButton(string name) =>
        _root.GetComponentsInChildren<Button>(false).FirstOrDefault(button => button.name == name);

    private Button[] LiveButtons(string prefix) =>
        _root.GetComponentsInChildren<Button>(false).Where(button => button.name.StartsWith(prefix)).ToArray();

    private string ButtonCaption(string name) => LiveButton(name).GetComponentInChildren<Text>().text;

    private Button FindButton(string name)
    {
        foreach (var button in _root.GetComponentsInChildren<Button>(true))
        {
            if (button.name == name)
            {
                return button;
            }
        }

        return null;
    }

    private Text FindText(string name)
    {
        foreach (var text in _root.GetComponentsInChildren<Text>(true))
        {
            if (text.name == name)
            {
                return text;
            }
        }

        return null;
    }
}
}
