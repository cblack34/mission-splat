namespace MissionSplat.App.Tests;

using System.Collections;
using System.Reflection;
using MissionSplat.Rules;

internal static class MissionIds
{
    public static HashSet<string> In(object root)
    {
        var ids = new HashSet<string>();
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        Walk(root, ids, seen);
        return ids;
    }

    private static void Walk(object? value, ISet<string> ids, ISet<object> seen)
    {
        if (value is null)
        {
            return;
        }

        switch (value)
        {
            case MissionId mission:
                ids.Add(mission.Value);
                return;
            case Mission mission:
                ids.Add(mission.Id.Value);
                return;
            case string text:
                ids.Add(text);
                return;
            case Cell:
            case SeatId:
            case TileId:
            case ColorId:
            case SymbolId:
                return;
        }

        var type = value.GetType();
        if (type.IsPrimitive || type.IsEnum)
        {
            return;
        }

        if (value is IEnumerable enumerable)
        {
            if (!seen.Add(value))
            {
                return;
            }

            foreach (var item in enumerable)
            {
                Walk(item, ids, seen);
            }

            return;
        }

        if (type.IsClass && !seen.Add(value))
        {
            return;
        }

        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            Walk(field.GetValue(value), ids, seen);
        }
    }
}
