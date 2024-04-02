

public static class DeclusterContainer
{
    public static IEnumerable<T> Decluster<T, U>(this IEnumerable<T> source, Func<T, U> keySelector)
    {
        var result = SplitDecluster(source.ToList(), keySelector).ToList();
        SinglePassDecluster(result, keySelector);
        return result;
    }

    private static void SinglePassDecluster<T, U>(List<T> items, Func<T, U> keySelector)
    {
        var keys = items.Select(keySelector).ToList();
        for (var i = 0; i < items.Count - 1; i++)
        {
            //this loop works by considering i-1, i, i+1, and i+2, and calculates if swapping i and i+1 would reduce clustering
            var oldCount = 0;
            var newCount = 0;
            if (i > 0)
            {
                oldCount += keys[i - 1]!.Equals(keys[i]) ? 1 : 0;
                newCount += keys[i - 1]!.Equals(keys[i + 1]) ? 1 : 0;
            }
            if (i < items.Count - 2)
            {
                oldCount += keys[i + 2]!.Equals(keys[i + 1]) ? 1 : 0;
                newCount += keys[i + 2]!.Equals(keys[i]) ? 1 : 0;
            }
            if (newCount < oldCount)
            {
                var temp = items[i];
                items[i] = items[i + 1];
                items[i + 1] = temp;
            }
        }
    }

    public static IEnumerable<T> SplitDecluster<T, U>(List<T> source, Func<T, U> keySelector)
    {
        if (source.Count <= 2)
        {
            return source;
        }
        var groups = source.GroupBy(keySelector);
        var left = new List<T>();
        var right = new List<T>();
        foreach (var group in groups)
        {
            var items = group.ToList();
            int midIndex = MyVidious.Utilities.Helpers.RandomRound(items.Count / 2.0);
            left.AddRange(items.GetRange(0, midIndex));
            right.AddRange(items.GetRange(midIndex, items.Count - midIndex));
        }
        return SplitDecluster(left, keySelector).Concat(SplitDecluster(right, keySelector));
    }
}