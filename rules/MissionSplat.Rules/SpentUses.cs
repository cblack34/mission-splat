namespace MissionSplat.Rules;

// Uses already spent on the drawn tile this turn, one count per power. The default value is a fresh turn, and an
// accepted placement starts the next turn at that default.
internal readonly struct SpentUses
{
    private readonly (SymbolId Power, int Count)[]? _counts;

    private SpentUses((SymbolId Power, int Count)[] counts)
    {
        _counts = counts;
    }

    public int Of(SymbolId power)
    {
        foreach (var (spentPower, count) in _counts ?? [])
        {
            if (spentPower.Equals(power))
            {
                return count;
            }
        }

        return 0;
    }

    public SpentUses WithUse(SymbolId power)
    {
        var counts = new List<(SymbolId Power, int Count)>(_counts ?? []);
        var index = counts.FindIndex(entry => entry.Power.Equals(power));
        if (index < 0)
        {
            counts.Add((power, 1));
        }
        else
        {
            counts[index] = (power, counts[index].Count + 1);
        }

        return new SpentUses(counts.ToArray());
    }
}
