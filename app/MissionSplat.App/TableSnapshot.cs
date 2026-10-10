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
        TableStatus status)
    {
        View = view ?? throw new ArgumentNullException(nameof(view));
        SecretsVisible = secretsVisible;
        ConcealVisible = concealVisible;
        LegalPlacements = Copied.List(legalPlacements, nameof(legalPlacements));
        LegalTargets = CopiedTargets(legalTargets, nameof(legalTargets));
        QuarterTurns = quarterTurns;
        Status = status ?? throw new ArgumentNullException(nameof(status));
    }

    public SeatView View { get; }

    public bool SecretsVisible { get; }

    public bool ConcealVisible { get; }

    // Beside and on-top positions for the chosen orientation.
    public IReadOnlyList<LegalPlacement> LegalPlacements { get; }

    public IReadOnlyList<PowerTargets> LegalTargets { get; }

    public int QuarterTurns { get; }

    public TableStatus Status { get; }

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
