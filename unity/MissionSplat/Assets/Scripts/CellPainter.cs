namespace MissionSplat.Player
{

using MissionSplat.Rules;
using UnityEngine;
using UnityEngine.UI;

internal static class CellPainter
{
    public static void Paint(RectTransform parent, Cell cell)
    {
        if (cell.IsWild)
        {
            Quarter(parent, SplatPalette.Red, new Vector2(0f, 0.5f), new Vector2(0.5f, 1f));
            Quarter(parent, SplatPalette.Blue, new Vector2(0.5f, 0.5f), new Vector2(1f, 1f));
            Quarter(parent, SplatPalette.Green, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f));
            Quarter(parent, SplatPalette.Purple, new Vector2(0.5f, 0f), new Vector2(1f, 0.5f));
            return;
        }

        if (cell.TryGetColor(out var color))
        {
            Dot(parent, SplatPalette.Of(color));
            return;
        }

        if (cell.TryGetSymbol(out var symbol))
        {
            PaintSymbol(parent, symbol);
        }
    }

    public static void PaintMission(RectTransform parent, Mission mission)
    {
        var fill = Ui.Image("Fill", parent, Color.white);
        Ui.Stretch(fill.rectTransform);
        var color = SplatPalette.Of(mission.Color);
        var lattice = Ui.Rect("Lattice", parent);
        lattice.anchorMin = new Vector2(0.5f, 0.58f);
        lattice.anchorMax = new Vector2(0.5f, 0.58f);
        lattice.pivot = new Vector2(0.5f, 0.5f);
        var side = Mathf.Min(parent.rect.width, parent.rect.height * 0.72f);
        if (side <= 1f)
        {
            side = 88f;
        }

        lattice.sizeDelta = new Vector2(side, side);
        switch (mission.Pattern)
        {
            case MissionPattern.Row:
                SquareDots(lattice, color, new[] { (0, 1), (1, 1), (2, 1), (3, 1) }, 4, 3);
                break;
            case MissionPattern.Square:
                SquareDots(lattice, color, new[] { (0, 1), (1, 1), (0, 0), (1, 0) }, 2, 2);
                break;
            default:
                SquareDots(lattice, color, new[] { (0, 2), (0, 1), (0, 0), (1, 0) }, 2, 3);
                break;
        }

        var caption = Ui.Label("Caption", parent, 16, SplatPalette.Ink, TextAnchor.LowerCenter);
        Ui.Anchored(caption.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.22f), Vector2.zero, Vector2.zero);
        caption.text = PatternName(mission.Pattern);
    }

    private static void PaintSymbol(RectTransform parent, SymbolId symbol)
    {
        var fill = symbol.Value == OrdinaryCatalog.Blank.Value ? SplatPalette.BlankFill : SplatPalette.Gray;
        Dot(parent, fill);
        if (symbol.Value == OrdinaryCatalog.Blank.Value)
        {
            return;
        }

        if (symbol.Value == OrdinaryCatalog.Rotate.Value)
        {
            // power-ups.svg draws an open clockwise arrow. The builtin font has no such glyph.
            var arrow = Ui.Image("Rotate", parent, Color.white, RotateArrow);
            arrow.preserveAspect = true;
            Ui.Anchored(arrow.rectTransform, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f), Vector2.zero, Vector2.zero);
            return;
        }

        var mark = Ui.Label("Mark", parent, 22, Color.white, TextAnchor.MiddleCenter);
        Ui.Stretch(mark.rectTransform);
        if (symbol.Value == OrdinaryCatalog.Stack.Value)
        {
            mark.text = "+";
        }
        else if (symbol.Value == OrdinaryCatalog.Bounce.Value)
        {
            mark.text = "-";
        }
        else
        {
            mark.text = symbol.Value.Substring(0, 1);
        }
    }

    private const float RotateRadius = 17.5f;

    private const float RotateHalfStroke = 2.8f;

    private const float RotateSweep = 220f * Mathf.Deg2Rad;

    private static readonly Sprite RotateArrow = BuildRotateArrow();

    private static Sprite BuildRotateArrow()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var center = (size - 1) / 2f;
        var (tip, baseLeft, baseRight) = RotateHead(center);
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var ink = OnRotateStroke(x, y, center) || InTriangle(x, y, tip, baseLeft, baseRight);
                texture.SetPixel(x, y, ink ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static bool OnRotateStroke(int x, int y, float center)
    {
        var dx = x - center;
        var dy = y - center;
        var distance = Mathf.Sqrt((dx * dx) + (dy * dy));
        if (Mathf.Abs(distance - RotateRadius) > RotateHalfStroke)
        {
            return false;
        }

        return ClockwiseFromTop(Mathf.Atan2(dy, dx)) <= RotateSweep;
    }

    private static float ClockwiseFromTop(float angle)
    {
        var span = (Mathf.PI / 2f) - angle;
        if (span < 0f)
        {
            span += Mathf.PI * 2f;
        }

        return span;
    }

    private static (Vector2 Tip, Vector2 BaseLeft, Vector2 BaseRight) RotateHead(float center)
    {
        var theta = (Mathf.PI / 2f) - RotateSweep;
        var forward = new Vector2(Mathf.Sin(theta), -Mathf.Cos(theta));
        var radial = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
        var origin = new Vector2(center, center) + (radial * RotateRadius);
        var back = origin - (forward * 6f);
        var side = new Vector2(-forward.y, forward.x) * 7.5f;
        return (origin + (forward * 10f), back + side, back - side);
    }

    private static bool InTriangle(int x, int y, Vector2 tip, Vector2 baseLeft, Vector2 baseRight)
    {
        var point = new Vector2(x, y);
        var ab = Cross(baseLeft - tip, point - tip);
        var bc = Cross(baseRight - baseLeft, point - baseLeft);
        var ca = Cross(tip - baseRight, point - baseRight);
        var positive = ab >= 0f && bc >= 0f && ca >= 0f;
        var negative = ab <= 0f && bc <= 0f && ca <= 0f;
        return positive || negative;
    }

    private static float Cross(Vector2 u, Vector2 v) => (u.x * v.y) - (u.y * v.x);

    private static void Dot(RectTransform parent, Color color)
    {
        var image = Ui.Image("Dot", parent, color, Ui.Circle);
        image.preserveAspect = true;
        Ui.Anchored(image.rectTransform, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f), Vector2.zero, Vector2.zero);
    }

    private static void Quarter(RectTransform parent, Color color, Vector2 min, Vector2 max)
    {
        var image = Ui.Image("Quarter", parent, color);
        Ui.Anchored(image.rectTransform, min, max, Vector2.zero, Vector2.zero);
    }

    private static void SquareDots(RectTransform lattice, Color color, (int x, int y)[] cells, int gridW, int gridH)
    {
        var span = Mathf.Max(gridW, gridH);
        var cell = lattice.sizeDelta.x / span;
        var originX = (lattice.sizeDelta.x - (gridW * cell)) * 0.5f;
        var originY = (lattice.sizeDelta.y - (gridH * cell)) * 0.5f;
        var pad = cell * 0.12f;
        for (var i = 0; i < cells.Length; i++)
        {
            var image = Ui.Image("Dot" + i, lattice, color, Ui.Circle);
            image.preserveAspect = true;
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(cell - pad, cell - pad);
            rect.anchoredPosition = new Vector2(
                originX + (cells[i].x * cell) + (pad * 0.5f),
                originY + (cells[i].y * cell) + (pad * 0.5f));
        }
    }

    private static string PatternName(MissionPattern pattern) =>
        pattern switch
        {
            MissionPattern.Row => "row",
            MissionPattern.Square => "square",
            _ => "L",
        };
}
}
