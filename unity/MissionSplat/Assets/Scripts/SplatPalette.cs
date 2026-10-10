namespace MissionSplat.Player
{

using System;
using MissionSplat.Rules;
using UnityEngine;

public static class SplatPalette
{
    public static readonly Color Cream = Hex("f7f1e4");
    public static readonly Color Ink = Hex("1c3348");
    public static readonly Color Muted = Hex("4a5b6b");
    public static readonly Color Tile = Hex("f3d19c");
    public static readonly Color TileEdge = Hex("8d6e43");
    public static readonly Color Red = Hex("e03131");
    public static readonly Color Blue = Hex("1c7ed6");
    public static readonly Color Green = Hex("2f9e44");
    public static readonly Color Purple = Hex("9c36b5");
    public static readonly Color Gray = Hex("adb5bd");
    public static readonly Color BlankFill = Hex("f8f9fa");
    public static readonly Color Highlight = new Color(0.184f, 0.620f, 0.267f, 0.42f);
    public static readonly Color StackHighlight = new Color(0.612f, 0.212f, 0.710f, 0.45f);
    public static readonly Color TargetHighlight = new Color(0.910f, 0.349f, 0.047f, 0.5f);
    public static readonly Color PowerSelected = Hex("e8590c");
    public static readonly Color Overlay = new Color(0.11f, 0.20f, 0.28f, 0.82f);

    public static Color Of(ColorId color)
    {
        switch (color.Value)
        {
            case "red":
                return Red;
            case "blue":
                return Blue;
            case "green":
                return Green;
            case "purple":
                return Purple;
            default:
                return HashColor(color.Value);
        }
    }

    public static Color Hex(string rgb)
    {
        var value = Convert.ToInt32(rgb, 16);
        return new Color(
            ((value >> 16) & 255) / 255f,
            ((value >> 8) & 255) / 255f,
            (value & 255) / 255f,
            1f);
    }

    private static Color HashColor(string id)
    {
        unchecked
        {
            var hash = 23;
            for (var i = 0; i < id.Length; i++)
            {
                hash = (hash * 31) + id[i];
            }

            return Color.HSVToRGB((hash & 255) / 255f, 0.55f, 0.8f);
        }
    }
}
}
