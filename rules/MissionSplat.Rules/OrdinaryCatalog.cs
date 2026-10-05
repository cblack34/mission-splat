namespace MissionSplat.Rules;

public static class OrdinaryCatalog
{
    public static ColorId Red { get; } = new("red");

    public static ColorId Blue { get; } = new("blue");

    public static ColorId Green { get; } = new("green");

    public static ColorId Purple { get; } = new("purple");

    // A wildcard is Cell.Wild(), not one of these symbols. It stays wild and counts as the mission color being checked.
    public static SymbolId Blank { get; } = new("blank");

    public static SymbolId Rotate { get; } = new("rotate");

    public static SymbolId Stack { get; } = new("stack");

    public static SymbolId Bounce { get; } = new("bounce");

    public static IReadOnlyList<ColorId> Colors { get; } = [Red, Blue, Green, Purple];

    public static IReadOnlyList<SymbolId> NonScoringSymbols { get; } = [Blank, Rotate, Stack, Bounce];

    public static IReadOnlyList<MissionPattern> Patterns { get; } =
        [MissionPattern.Row, MissionPattern.Square, MissionPattern.L];

    public const int ClaimsRequiredToWin = 4;
}
