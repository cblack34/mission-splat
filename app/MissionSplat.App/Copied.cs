namespace MissionSplat.App;

// Defensive copies of the lists a constructor or factory takes, so a caller cannot change them afterwards.
internal static class Copied
{
    public static T[] List<T>(IReadOnlyList<T>? items, string paramName)
    {
        if (items is null)
        {
            throw new ArgumentNullException(paramName);
        }

        var copy = new T[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            copy[i] = items[i];
        }

        return copy;
    }

    public static T[] Items<T>(IReadOnlyList<T>? items, string paramName)
        where T : class
    {
        var copy = List(items, paramName);
        for (var i = 0; i < copy.Length; i++)
        {
            if (copy[i] is null)
            {
                throw new ArgumentException("The list contains a null entry.", paramName);
            }
        }

        return copy;
    }
}
