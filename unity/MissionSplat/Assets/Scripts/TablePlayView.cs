namespace MissionSplat.Player
{

using System;
using MissionSplat.App;
using MissionSplat.Rules;
using UnityEngine;
using UnityEngine.UI;

internal sealed class TablePlayView
{
    private readonly Text _status;
    private readonly RectTransform _pending;
    private readonly RectTransform _secrets;
    private readonly RectTransform _board;
    private readonly RectTransform _claims;
    private readonly RectTransform _rotation;
    private readonly RectTransform _powers;
    private readonly RectTransform _conceal;
    private readonly Text _concealLabel;

    public event Action<int, int> Tapped;
    public event Action Confirmed;
    public event Action<int> QuarterTurnsSelected;
    public event Action<SymbolId?> PowerSelected;
    public event Action<int, int> TargetTapped;

    public TablePlayView(RectTransform play)
    {
        _status = Ui.Label("Status", play, 28, SplatPalette.Ink, TextAnchor.MiddleLeft);
        // Stop rulings are long sentences. Only this label wraps; captions and buttons stay on one line.
        _status.horizontalOverflow = HorizontalWrapMode.Wrap;
        Ui.Anchored(_status.rectTransform, new Vector2(0.02f, 0.92f), new Vector2(0.98f, 0.99f), Vector2.zero, Vector2.zero);

        _pending = Ui.Rect("Pending", play);
        Ui.Anchored(_pending, new Vector2(0.02f, 0.72f), new Vector2(0.18f, 0.91f), Vector2.zero, Vector2.zero);
        _secrets = Ui.Rect("Secrets", play);
        Ui.Anchored(_secrets, new Vector2(0.20f, 0.72f), new Vector2(0.72f, 0.91f), Vector2.zero, Vector2.zero);
        _rotation = Ui.Rect("Rotation", play);
        Ui.Anchored(_rotation, new Vector2(0.74f, 0.72f), new Vector2(0.98f, 0.91f), Vector2.zero, Vector2.zero);
        // The power band sits under the top band, so the quarter-turn row above it doubles as the rotate amount.
        _powers = Ui.Rect("Powers", play);
        Ui.Anchored(_powers, new Vector2(0.02f, 0.63f), new Vector2(0.98f, 0.71f), Vector2.zero, Vector2.zero);
        _board = Ui.Rect("Board", play);
        Ui.Anchored(_board, new Vector2(0.02f, 0.18f), new Vector2(0.98f, 0.62f), Vector2.zero, Vector2.zero);
        var boardFill = Ui.Image("BoardFill", _board, Color.white);
        Ui.Stretch(boardFill.rectTransform);
        boardFill.color = new Color(1f, 1f, 1f, 0.35f);
        _claims = Ui.Rect("Claims", play);
        Ui.Anchored(_claims, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.16f), Vector2.zero, Vector2.zero);

        _conceal = Ui.Rect("Conceal", play);
        Ui.Anchored(_conceal, new Vector2(0.20f, 0.72f), new Vector2(0.72f, 0.91f), Vector2.zero, Vector2.zero);
        var concealFill = Ui.Image("Fill", _conceal, SplatPalette.Overlay);
        Ui.Stretch(concealFill.rectTransform);
        concealFill.raycastTarget = true;
        _concealLabel = Ui.Label("Message", _conceal, 22, Color.white, TextAnchor.MiddleCenter);
        Ui.Anchored(_concealLabel.rectTransform, new Vector2(0.04f, 0.42f), new Vector2(0.96f, 0.95f), Vector2.zero, Vector2.zero);
        var confirm = Ui.Button("Confirm", _conceal, "Confirm", SplatPalette.Green);
        Ui.Anchored(confirm.GetComponent<RectTransform>(), new Vector2(0.30f, 0.08f), new Vector2(0.70f, 0.38f), Vector2.zero, Vector2.zero);
        confirm.onClick.AddListener(() => Confirmed?.Invoke());
        _conceal.gameObject.SetActive(false);
    }

    public void Render(TableSnapshot snapshot)
    {
        Canvas.ForceUpdateCanvases();
        _status.text = StatusText(snapshot);
        // The quarter-turn value is the rotate amount while a power is selected, so the pending tile does not preview it.
        PaintPending(snapshot.View.PendingMatchTile, snapshot.SelectedPower is null ? snapshot.QuarterTurns : 0);
        PaintSecrets(snapshot);
        PaintBoard(snapshot);
        PaintClaims(snapshot.View);
        PaintRotation(snapshot);
        PaintPowers(snapshot);
        _conceal.gameObject.SetActive(snapshot.ConcealVisible);
        if (snapshot.ConcealVisible)
        {
            _concealLabel.text = SeatLabel(snapshot.View.CurrentSeat) + ", confirm to see your missions.";
        }
    }

    private static string SeatLabel(SeatId seat) => "Seat " + seat.Value;

    private static string StatusText(TableSnapshot snapshot)
    {
        var status = snapshot.Status;
        return status.Kind switch
        {
            TableStatusKind.ToConfirm => SeatLabel(status.Seat.Value) + " to confirm.",
            TableStatusKind.ToAct => SeatLabel(status.Seat.Value) + (snapshot.SelectedPower is { } power ? ": pick a tile to " + power.Value + "." : " to place."),
            TableStatusKind.Won => SeatLabel(status.Seat.Value) + " wins.",
            TableStatusKind.Ended => "The game has ended.",
            _ => status.Message,
        };
    }

    private void PaintPending(Tile pending, int quarterTurns)
    {
        Ui.Clear(_pending);
        var title = Ui.Label("Title", _pending, 18, SplatPalette.Muted, TextAnchor.UpperLeft);
        Ui.Anchored(title.rectTransform, new Vector2(0f, 0.82f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        title.text = "Pending tile";
        if (pending is null)
        {
            return;
        }

        PaintTile(_pending, pending, new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.80f), quarterTurns);
    }

    private void PaintSecrets(TableSnapshot snapshot)
    {
        Ui.Clear(_secrets);
        var title = Ui.Label("Title", _secrets, 18, SplatPalette.Muted, TextAnchor.UpperLeft);
        Ui.Anchored(title.rectTransform, new Vector2(0f, 0.82f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        title.text = snapshot.SecretsVisible ? SeatLabel(snapshot.View.Seat) + " missions" : "Missions hidden";
        if (!snapshot.SecretsVisible)
        {
            return;
        }

        var missions = snapshot.View.UnclaimedMissions;
        for (var i = 0; i < missions.Count; i++)
        {
            var card = Ui.Image("Mission" + i, _secrets, Color.white);
            var min = new Vector2(0.02f + (i * 0.48f), 0.02f);
            var max = new Vector2(0.46f + (i * 0.48f), 0.80f);
            Ui.Anchored(card.rectTransform, min, max, Vector2.zero, Vector2.zero);
            CellPainter.PaintMission(card.rectTransform, missions[i]);
        }
    }

    private void PaintBoard(TableSnapshot snapshot)
    {
        // Child 0 is the permanent board fill.
        Ui.Clear(_board, 1);

        var view = snapshot.View;
        // An emptied board still offers the origin, so only return when nothing is drawn or tappable.
        if (view.Board.Cells.Count == 0 && snapshot.LegalPlacements.Count == 0)
        {
            return;
        }

        var minX = int.MaxValue;
        var maxX = int.MinValue;
        var minY = int.MaxValue;
        var maxY = int.MinValue;
        foreach (var occupiedCell in view.Board.Cells)
        {
            minX = Math.Min(minX, occupiedCell.CellX);
            maxX = Math.Max(maxX, occupiedCell.CellX);
            minY = Math.Min(minY, occupiedCell.CellY);
            maxY = Math.Max(maxY, occupiedCell.CellY);
        }

        foreach (var highlight in snapshot.LegalPlacements)
        {
            minX = Math.Min(minX, highlight.TileX * 2);
            maxX = Math.Max(maxX, (highlight.TileX * 2) + 1);
            minY = Math.Min(minY, highlight.TileY * 2);
            maxY = Math.Max(maxY, (highlight.TileY * 2) + 1);
        }

        var width = maxX - minX + 1;
        var height = maxY - minY + 1;
        var area = _board.rect;
        var cellSize = Math.Min(area.width / (width + 1), area.height / (height + 1));
        if (cellSize <= 1f)
        {
            cellSize = 36f;
        }

        var lattice = Ui.Rect("Lattice", _board);
        lattice.anchorMin = new Vector2(0.5f, 0.5f);
        lattice.anchorMax = new Vector2(0.5f, 0.5f);
        lattice.pivot = new Vector2(0.5f, 0.5f);
        lattice.sizeDelta = new Vector2(width * cellSize, height * cellSize);

        foreach (var tile in view.Board.Tiles)
        {
            var back = Ui.Image("Tile" + tile.Id.Value, lattice, SplatPalette.Tile);
            PlaceCell(back.rectTransform, tile.TileX * 2, tile.TileY * 2, 2, 2, minX, minY, cellSize, 0f, 0f);
        }

        foreach (var occupied in view.Board.Cells)
        {
            var cellRect = Ui.Rect("Cell" + occupied.CellX + "_" + occupied.CellY, lattice);
            PlaceCell(cellRect, occupied.CellX, occupied.CellY, 1, 1, minX, minY, cellSize, 0f, 0f);
            CellPainter.Paint(cellRect, occupied.Value);
        }

        if (snapshot.ConcealVisible)
        {
            return;
        }

        // While a power is selected the snapshot offers no placements, so only its targets are drawn.
        foreach (var target in snapshot.SelectedTargets)
        {
            AddTileButton(lattice, "Target" + target.TileX + "_" + target.TileY, target.TileX, target.TileY, SplatPalette.TargetHighlight, minX, minY, cellSize, () => TargetTapped?.Invoke(target.TileX, target.TileY));
        }

        foreach (var highlight in snapshot.LegalPlacements)
        {
            var tint = highlight.Kind == PlacementKind.OnTop ? SplatPalette.StackHighlight : SplatPalette.Highlight;
            AddTileButton(lattice, "Highlight" + highlight.TileX + "_" + highlight.TileY, highlight.TileX, highlight.TileY, tint, minX, minY, cellSize, () => Tapped?.Invoke(highlight.TileX, highlight.TileY));
        }
    }

    private static void AddTileButton(RectTransform lattice, string name, int tileX, int tileY, Color tint, int minX, int minY, float cellSize, Action onTap)
    {
        var button = Ui.Button(name, lattice, string.Empty, tint);
        PlaceCell(button.GetComponent<RectTransform>(), tileX * 2, tileY * 2, 2, 2, minX, minY, cellSize, 0f, 0f);
        button.onClick.AddListener(() => onTap());
    }

    private void PaintClaims(SeatView view)
    {
        Ui.Clear(_claims);
        var title = Ui.Label("Title", _claims, 18, SplatPalette.Muted, TextAnchor.UpperLeft);
        Ui.Anchored(title.rectTransform, new Vector2(0f, 0.78f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        title.text = "Claims";
        var seats = view.Claims.Count;
        for (var i = 0; i < seats; i++)
        {
            var row = view.Claims[i];
            var min = new Vector2(i / (float)seats, 0f);
            var max = new Vector2((i + 1) / (float)seats, 0.76f);
            var group = Ui.Rect("Seat" + row.Seat.Value, _claims);
            Ui.Anchored(group, min, max, new Vector2(6f, 0f), new Vector2(-6f, 0f));
            var label = Ui.Label("Label", group, 16, SplatPalette.Ink, TextAnchor.UpperLeft);
            Ui.Anchored(label.rectTransform, new Vector2(0f, 0.72f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            label.text = SeatLabel(row.Seat);
            for (var m = 0; m < row.Missions.Count; m++)
            {
                var card = Ui.Image("Claim" + m, group, Color.white);
                var (cardMin, cardMax) = ClaimCardAnchors(m, row.Missions.Count);
                Ui.Anchored(card.rectTransform, cardMin, cardMax, Vector2.zero, Vector2.zero);
                CellPainter.PaintMission(card.rectTransform, row.Missions[m]);
            }
        }
    }

    private void PaintRotation(TableSnapshot snapshot)
    {
        Ui.Clear(_rotation);
        var title = Ui.Label("Title", _rotation, 18, SplatPalette.Muted, TextAnchor.UpperLeft);
        Ui.Anchored(title.rectTransform, new Vector2(0f, 0.82f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        var rotating = snapshot.SelectedPower is { } power && power.Equals(OrdinaryCatalog.Rotate);
        title.text = rotating ? "Rotate by" : "Quarter-turns";
        for (var turn = 0; turn < 4; turn++)
        {
            var chosen = turn;
            // Rules refuses a rotate by zero, so that choice is greyed out instead of offering a dead tap.
            var offered = !(rotating && turn == 0);
            var fill = !offered ? SplatPalette.Gray : turn == snapshot.QuarterTurns ? SplatPalette.Green : SplatPalette.Muted;
            var button = Ui.Button("Turn" + turn, _rotation, turn.ToString(), fill);
            var x = turn / 4f;
            Ui.Anchored(button.GetComponent<RectTransform>(), new Vector2(x + 0.02f, 0.15f), new Vector2(x + 0.23f, 0.75f), Vector2.zero, Vector2.zero);
            button.interactable = offered && snapshot.CanAct;
            button.onClick.AddListener(() => QuarterTurnsSelected?.Invoke(chosen));
        }
    }

    private void PaintPowers(TableSnapshot snapshot)
    {
        Ui.Clear(_powers);
        var title = Ui.Label("Title", _powers, 18, SplatPalette.Muted, TextAnchor.UpperLeft);
        Ui.Anchored(title.rectTransform, new Vector2(0f, 0.78f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        title.text = "Powers";
        var charges = snapshot.View.RemainingUses;
        var slots = Math.Max(3, charges.Count + 1);
        for (var i = 0; i < charges.Count; i++)
        {
            var charge = charges[i];
            var selected = snapshot.SelectedPower is { } current && current.Equals(charge.Power);
            var usable = snapshot.CanAct && charge.Remaining > 0;
            var name = PowerName(charge.Power);
            var fill = selected ? SplatPalette.PowerSelected : usable ? SplatPalette.Muted : SplatPalette.Gray;
            var button = Ui.Button("Power" + name, _powers, name + " · " + charge.Remaining + " left", fill);
            AnchorSlot(button.GetComponent<RectTransform>(), i, slots);
            button.interactable = usable;
            var power = charge.Power;
            button.onClick.AddListener(() => PowerSelected?.Invoke(selected ? null : power));
        }

        if (snapshot.SelectedPower is not null)
        {
            var back = Ui.Button("PlaceInstead", _powers, "Place instead", SplatPalette.Green);
            AnchorSlot(back.GetComponent<RectTransform>(), charges.Count, slots);
            back.onClick.AddListener(() => PowerSelected?.Invoke(null));
        }
    }

    private static void AnchorSlot(RectTransform rect, int index, int slots)
    {
        var x = index / (float)slots;
        Ui.Anchored(rect, new Vector2(x + 0.01f, 0.05f), new Vector2(x + (1f / slots) - 0.01f, 0.76f), Vector2.zero, Vector2.zero);
    }

    private static string PowerName(SymbolId power) => char.ToUpperInvariant(power.Value[0]) + power.Value.Substring(1);

    // Four is the ordinary row. One placement can claim both held missions, so a row at three can finish at five and must stay inside its group.
    private static (Vector2 Min, Vector2 Max) ClaimCardAnchors(int index, int missionCount)
    {
        var slots = Math.Max(4, missionCount);
        const float margin = 0.02f;
        const float gap = 0.02f;
        var width = (1f - (2f * margin) - ((slots - 1) * gap)) / slots;
        var left = margin + (index * (width + gap));
        return (new Vector2(left, 0.02f), new Vector2(left + width, 0.70f));
    }

    private static void PaintTile(RectTransform parent, Tile tile, Vector2 min, Vector2 max, int quarterTurns)
    {
        var frame = Ui.Image("Tile", parent, SplatPalette.Tile);
        Ui.Anchored(frame.rectTransform, min, max, Vector2.zero, Vector2.zero);
        Canvas.ForceUpdateCanvases();
        var area = frame.rectTransform.rect;
        var cell = Math.Min(area.width, area.height) * 0.42f;
        if (cell <= 1f)
        {
            cell = 36f;
        }

        var lattice = Ui.Rect("Lattice", frame.transform);
        lattice.anchorMin = new Vector2(0.5f, 0.5f);
        lattice.anchorMax = new Vector2(0.5f, 0.5f);
        lattice.pivot = new Vector2(0.5f, 0.5f);
        lattice.sizeDelta = new Vector2(cell * 2f, cell * 2f);
        for (var y = 0; y < 2; y++)
        {
            for (var x = 0; x < 2; x++)
            {
                var cellRect = Ui.Rect("C" + x + y, lattice);
                // Same clockwise map as Rules.Tile: (x, y) goes to (y, 1 - x).
                var (turnedX, turnedY) = TurnClockwise(x, y, quarterTurns);
                PlaceCell(cellRect, turnedX, turnedY, 1, 1, 0, 0, cell, 0f, 0f);
                CellPainter.Paint(cellRect, tile.Local(x, y));
            }
        }
    }

    private static (int X, int Y) TurnClockwise(int x, int y, int quarterTurns) =>
        quarterTurns switch
        {
            0 => (x, y),
            1 => (y, 1 - x),
            2 => (1 - x, 1 - y),
            3 => (1 - y, x),
            _ => throw new ArgumentOutOfRangeException(nameof(quarterTurns), quarterTurns, "Quarter-turns are 0, 1, 2, or 3."),
        };

    private static void PlaceCell(
        RectTransform rect,
        int cellX,
        int cellY,
        int cellsWide,
        int cellsHigh,
        int minX,
        int minY,
        float cell,
        float originX,
        float originY)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.sizeDelta = new Vector2(cellsWide * cell, cellsHigh * cell);
        rect.anchoredPosition = new Vector2(originX + ((cellX - minX) * cell), originY + ((cellY - minY) * cell));
    }
}
}
