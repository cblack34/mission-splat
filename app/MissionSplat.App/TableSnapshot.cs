namespace MissionSplat.App;

using MissionSplat.Rules;

// Everything a GUI draws for the seat holding the device. The lists are empty when nobody may act.
public sealed class TableSnapshot
{
    public TableSnapshot(
        SeatView view,
        bool secretsVisible,
        bool concealVisible,
        IReadOnlyList<LegalPlacement> legalPlacements,
        IReadOnlyList<PowerTargets> legalTargets,
        int quarterTurns,
        TableStatus status,
        SymbolId? selectedPower = null)
    {
        View = view ?? throw new ArgumentNullException(nameof(view));
        SecretsVisible = secretsVisible;
        ConcealVisible = concealVisible;
        LegalPlacements = Copied.List(legalPlacements, nameof(legalPlacements));
        LegalTargets = CopiedTargets(legalTargets, nameof(legalTargets));
        QuarterTurns = quarterTurns;
        Status = status ?? throw new ArgumentNullException(nameof(status));
        SelectedPower = selectedPower;
    }

    public SeatView View { get; }

    public bool SecretsVisible { get; }

    public bool ConcealVisible { get; }

    // Beside and on-top positions for the chosen orientation; empty while a power is selected.
    public IReadOnlyList<LegalPlacement> LegalPlacements { get; }

    // Every in-play power's targets, so a panel can show them all; the selected power's entry is the one to highlight.
    public IReadOnlyList<PowerTargets> LegalTargets { get; }

    public int QuarterTurns { get; }

    public TableStatus Status { get; }

    // The power whose targets to highlight instead of placements; null while the seat is placing.
    public SymbolId? SelectedPower { get; }

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
