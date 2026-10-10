namespace MissionSplat.App;

using MissionSplat.Rules;

// Everything a GUI draws for the seat holding the device. The lists are empty when nobody may act.
public sealed class TableSnapshot
{
    public TableSnapshot(
        SeatView view,
        bool secretsVisible,
        bool concealVisible,
        bool canAct,
        IReadOnlyList<LegalPlacement> legalPlacements,
        IReadOnlyList<PowerTargets> legalTargets,
        int quarterTurns,
        TableStatus status,
        SymbolId? selectedPower)
    {
        View = view ?? throw new ArgumentNullException(nameof(view));
        SecretsVisible = secretsVisible;
        ConcealVisible = concealVisible;
        CanAct = canAct;
        LegalPlacements = Copied.List(legalPlacements, nameof(legalPlacements));
        LegalTargets = CopiedTargets(legalTargets, nameof(legalTargets));
        QuarterTurns = quarterTurns;
        Status = status ?? throw new ArgumentNullException(nameof(status));
        SelectedPower = selectedPower;
        SelectedTargets = TargetsOf(LegalTargets, selectedPower);
    }

    public SeatView View { get; }

    public bool SecretsVisible { get; }

    public bool ConcealVisible { get; }

    // True while the seat holding the device may place or use a power; the controls are live only then.
    public bool CanAct { get; }

    // Beside and on-top positions for the chosen orientation; empty while a power is selected.
    public IReadOnlyList<LegalPlacement> LegalPlacements { get; }

    // Every in-play power's targets, so a panel can show them all; SelectedTargets is the selected one's.
    public IReadOnlyList<PowerTargets> LegalTargets { get; }

    public int QuarterTurns { get; }

    public TableStatus Status { get; }

    // The power whose targets to highlight instead of placements; null while the seat is placing.
    public SymbolId? SelectedPower { get; }

    // The positions to highlight for SelectedPower; empty while the seat is placing.
    public IReadOnlyList<BoardPosition> SelectedTargets { get; }

    private static IReadOnlyList<BoardPosition> TargetsOf(IReadOnlyList<PowerTargets> entries, SymbolId? power)
    {
        for (var i = 0; power is not null && i < entries.Count; i++)
        {
            if (entries[i].Power.Equals(power))
            {
                return entries[i].Targets;
            }
        }

        return [];
    }

    private static PowerTargets[] CopiedTargets(IReadOnlyList<PowerTargets>? targets, string paramName)
    {
        var copy = Copied.List(targets, paramName);
        for (var i = 0; i < copy.Length; i++)
        {
            copy[i] = new PowerTargets(copy[i].Power, Copied.List(copy[i].Targets, paramName));
        }

        return copy;
    }
}

public readonly record struct PowerTargets(SymbolId Power, IReadOnlyList<BoardPosition> Targets);
