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
    private readonly RectTransform _conceal;
    private readonly Text _concealLabel;

    public event Action<int, int> Tapped;
    public event Action Confirmed;
    public event Action<int> QuarterTurnsSelected;

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
        _board = Ui.Rect("Board", play);
        Ui.Anchored(_board, new Vector2(0.02f, 0.18f), new Vector2(0.98f, 0.70f), Vector2.zero, Vector2.zero);
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
        _status.text = StatusText(snapshot.Status);
        PaintPending(snapshot.View.PendingMatchTile, snapshot.QuarterTurns);
        PaintSecrets(snapshot);
        PaintBoard(snapshot);
        PaintClaims(snapshot.View);
        PaintRotation(snapshot);
        _conceal.gameObject.SetActive(snapshot.ConcealVisible);
        if (snapshot.ConcealVisible)
        {
            _concealLabel.text = SeatLabel(snapshot.View.CurrentSeat) + ", confirm to see your missions.";
        }
    }

    private static string SeatLabel(SeatId seat) => "Seat " + seat.Value;

    private static string StatusText(TableStatus status) =>
        status.Kind switch
        {
            TableStatusKind.ToConfirm => SeatLabel(status.Seat.Value) + " to confirm.",
            TableStatusKind.ToAct => SeatLabel(status.Seat.Value) + " to place.",
            TableStatusKind.Won => SeatLabel(status.Seat.Value) + " wins.",
            TableStatusKind.Ended => "The game has ended.",
            _ => status.Message,
        };

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
        if (view.Board.Cells.Count == 0)
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

        foreach (var highlight in snapshot.LegalPlacements)
        {
            var tint = highlight.Kind == PlacementKind.OnTop ? SplatPalette.StackHighlight : SplatPalette.Highlight;
            var button = Ui.Button("Highlight" + highlight.TileX + "_" + highlight.TileY, lattice, string.Empty, tint);
            PlaceCell(button.GetComponent<RectTransform>(), highlight.TileX * 2, highlight.TileY * 2, 2, 2, minX, minY, cellSize, 0f, 0f);
            var chosen = highlight;
            button.onClick.AddListener(() => Tapped?.Invoke(chosen.TileX, chosen.TileY));
        }
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
        title.text = "Quarter-turns";
        for (var turn = 0; turn < 4; turn++)
        {
            var chosen = turn;
            var fill = turn == snapshot.QuarterTurns ? SplatPalette.Green : SplatPalette.Muted;
            var button = Ui.Button("Turn" + turn, _rotation, turn.ToString(), fill);
            var x = turn / 4f;
            Ui.Anchored(button.GetComponent<RectTransform>(), new Vector2(x + 0.02f, 0.15f), new Vector2(x + 0.23f, 0.75f), Vector2.zero, Vector2.zero);
            button.interactable = !snapshot.ConcealVisible && snapshot.Status.Kind == TableStatusKind.ToAct;
            button.onClick.AddListener(() => QuarterTurnsSelected?.Invoke(chosen));
        }
    }

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
