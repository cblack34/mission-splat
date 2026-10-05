namespace MissionSplat.Rules;

public readonly record struct Cell
{
    private enum Kind : byte
    {
        None = 0,
        Color = 1,
        Wild = 2,
        Symbol = 3,
    }

    private readonly Kind _kind;
    private readonly string? _id;

    private Cell(Kind kind, string? id)
    {
        _kind = kind;
        _id = id;
    }

    public static Cell Color(ColorId color) => new(Kind.Color, color.Value);

    public static Cell Wild() => new(Kind.Wild, null);

    public static Cell Symbol(SymbolId symbol) => new(Kind.Symbol, symbol.Value);

    public bool IsWild => _kind == Kind.Wild;

    public bool IsDefined => _kind != Kind.None;

    public bool CountsAs(ColorId color) =>
        _kind == Kind.Wild || (_kind == Kind.Color && _id == color.Value);

    public bool TryGetColor(out ColorId color)
    {
        if (_kind == Kind.Color && _id is not null)
        {
            color = new ColorId(_id);
            return true;
        }

        color = default;
        return false;
    }

    public bool TryGetSymbol(out SymbolId symbol)
    {
        if (_kind == Kind.Symbol && _id is not null)
        {
            symbol = new SymbolId(_id);
            return true;
        }

        symbol = default;
        return false;
    }

    public override string ToString() => _kind switch
    {
        Kind.Color => "color:" + _id,
        Kind.Wild => "wild",
        Kind.Symbol => "symbol:" + _id,
        _ => "undefined",
    };
}
