namespace MissionSplat.Rules;

internal static class SetupCheck
{
    public static Rejection? Find(GameSetup setup)
    {
        var seats = setup.SeatsInTurnOrder;
        if (seats.Count is < 2 or > 4)
        {
            return Invalid("A game has 2, 3, or 4 seats.");
        }

        for (var i = 0; i < seats.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(seats[i].Value))
            {
                return Invalid("A seat id is required.");
            }

            for (var j = i + 1; j < seats.Count; j++)
            {
                if (seats[i].Equals(seats[j]))
                {
                    return Invalid($"Seat '{seats[i].Value}' is listed twice.");
                }
            }
        }

        if (string.IsNullOrWhiteSpace(setup.FirstSeat.Value) || !ContainsSeat(seats, setup.FirstSeat))
        {
            return Invalid("The first seat has to be one of the seats at the table.");
        }

        if (setup.ClaimsRequiredToWin < 1)
        {
            return Invalid("Claims required to win must be at least 1.");
        }

        var colors = Distinct(setup.Colors, out var duplicateColor);
        if (duplicateColor)
        {
            return Invalid("The color catalog lists a color twice.");
        }

        var symbols = Distinct(setup.NonScoringSymbols, out var duplicateSymbol);
        if (duplicateSymbol)
        {
            return Invalid("The non-scoring symbols list a symbol twice.");
        }

        if (HasDuplicatePattern(setup.ActivePatterns, out var unknownPattern))
        {
            return unknownPattern
                ? Invalid("A pattern in play is not a row, square, or L.")
                : Invalid("The patterns in play list a pattern twice.");
        }

        if (setup.MissionDeck.Count < seats.Count * 2)
        {
            return Invalid("The mission deck does not have two cards for every seat.");
        }

        var missionIds = new HashSet<MissionId>();
        foreach (var mission in setup.MissionDeck)
        {
            if (!missionIds.Add(mission.Id))
            {
                return Invalid($"Mission '{mission.Id.Value}' is in the deck twice.");
            }

            if (!colors.Contains(mission.Color))
            {
                return Invalid(
                    $"Mission '{mission.Id.Value}' uses color '{mission.Color.Value}', which is not in the catalog.");
            }
        }

        if (setup.MatchDeck.Count < 1)
        {
            return Invalid("The match deck needs a starting tile.");
        }

        var tileIds = new HashSet<TileId>();
        foreach (var tile in setup.MatchDeck)
        {
            if (!tileIds.Add(tile.Id))
            {
                return Invalid($"Tile '{tile.Id.Value}' is in the deck twice.");
            }

            for (var y = 0; y < 2; y++)
            {
                for (var x = 0; x < 2; x++)
                {
                    var cellProblem = CellProblem(tile.Local(x, y), colors, symbols, tile.Id);
                    if (cellProblem is not null)
                    {
                        return cellProblem;
                    }
                }
            }
        }

        return null;
    }

    private static Rejection? CellProblem(
        Cell cell,
        HashSet<ColorId> colors,
        HashSet<SymbolId> symbols,
        TileId tile)
    {
        if (!cell.IsDefined)
        {
            return Invalid($"Tile '{tile.Value}' has an empty cell.");
        }

        if (cell.TryGetColor(out var color) && !colors.Contains(color))
        {
            return Invalid($"Tile '{tile.Value}' uses color '{color.Value}', which is not in the catalog.");
        }

        if (cell.TryGetSymbol(out var symbol) && !symbols.Contains(symbol))
        {
            return Invalid($"Tile '{tile.Value}' uses symbol '{symbol.Value}', which is not a non-scoring symbol.");
        }

        return null;
    }

    private static bool ContainsSeat(IReadOnlyList<SeatId> seats, SeatId seat)
    {
        foreach (var candidate in seats)
        {
            if (candidate.Equals(seat))
            {
                return true;
            }
        }

        return false;
    }

    private static HashSet<T> Distinct<T>(IReadOnlyList<T> items, out bool duplicate)
    {
        var set = new HashSet<T>();
        duplicate = false;
        foreach (var item in items)
        {
            if (!set.Add(item))
            {
                duplicate = true;
            }
        }

        return set;
    }

    private static bool HasDuplicatePattern(IReadOnlyList<MissionPattern> patterns, out bool unknown)
    {
        unknown = false;
        var seen = new HashSet<MissionPattern>();
        foreach (var pattern in patterns)
        {
            if (!Enum.IsDefined(typeof(MissionPattern), pattern))
            {
                unknown = true;
                return true;
            }

            if (!seen.Add(pattern))
            {
                return true;
            }
        }

        return false;
    }

    private static Rejection Invalid(string message) => new(RejectionReason.InvalidSetup, message);
}
