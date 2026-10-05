namespace MissionSplat.Rules;

public readonly record struct SeatId
{
    public SeatId(string value)
    {
        Value = IdText.Required(value, nameof(value));
    }

    public string Value { get; }
}

public readonly record struct ColorId
{
    public ColorId(string value)
    {
        Value = IdText.Required(value, nameof(value));
    }

    public string Value { get; }
}

public readonly record struct SymbolId
{
    public SymbolId(string value)
    {
        Value = IdText.Required(value, nameof(value));
    }

    public string Value { get; }
}

public readonly record struct MissionId
{
    public MissionId(string value)
    {
        Value = IdText.Required(value, nameof(value));
    }

    public string Value { get; }
}

public readonly record struct TileId
{
    public TileId(string value)
    {
        Value = IdText.Required(value, nameof(value));
    }

    public string Value { get; }
}

internal static class IdText
{
    public static string Required(string value, string name)
    {
        if (IsMissing(value))
        {
            throw new ArgumentException("An id is required.", name);
        }

        return value;
    }

    // A default struct skips Required, so its text is null.
    public static bool IsMissing(string? value) => string.IsNullOrWhiteSpace(value);
}
