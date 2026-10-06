namespace MissionSplat.Rules;

public sealed class GameSetup
{
    public GameSetup(
        IReadOnlyList<SeatId> seatsInTurnOrder,
        SeatId firstSeat,
        IReadOnlyList<ColorId> colors,
        IReadOnlyList<SymbolId> nonScoringSymbols,
        IReadOnlyList<MissionPattern> activePatterns,
        int claimsRequiredToWin,
        IReadOnlyList<Mission> missionDeck,
        IReadOnlyList<Tile> matchDeck)
    {
        SeatsInTurnOrder = Copy(seatsInTurnOrder, nameof(seatsInTurnOrder));
        FirstSeat = firstSeat;
        Colors = Copy(colors, nameof(colors));
        NonScoringSymbols = Copy(nonScoringSymbols, nameof(nonScoringSymbols));
        ActivePatterns = Copy(activePatterns, nameof(activePatterns));
        ClaimsRequiredToWin = claimsRequiredToWin;
        MissionDeck = CopyItems(missionDeck, nameof(missionDeck));
        MatchDeck = CopyItems(matchDeck, nameof(matchDeck));
    }

    public IReadOnlyList<SeatId> SeatsInTurnOrder { get; }

    public SeatId FirstSeat { get; }

    public IReadOnlyList<ColorId> Colors { get; }

    public IReadOnlyList<SymbolId> NonScoringSymbols { get; }

    public IReadOnlyList<MissionPattern> ActivePatterns { get; }

    public int ClaimsRequiredToWin { get; }

    public IReadOnlyList<Mission> MissionDeck { get; }

    public IReadOnlyList<Tile> MatchDeck { get; }

    private static T[] Copy<T>(IReadOnlyList<T>? items, string name)
    {
        if (items is null)
        {
            throw new ArgumentNullException(name);
        }

        var copy = new T[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            copy[i] = items[i];
        }

        return copy;
    }

    private static T[] CopyItems<T>(IReadOnlyList<T>? items, string name)
        where T : class
    {
        var copy = Copy(items, name);
        for (var i = 0; i < copy.Length; i++)
        {
            if (copy[i] is null)
            {
                throw new ArgumentException("The list contains a null entry.", name);
            }
        }

        return copy;
    }
}
