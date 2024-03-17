

namespace MyVidious.Utilities;

public static class Helpers
{
    /// <summary>
    /// Mutates the input list and returns it
    /// </summary>
    public static List<T> RandomizeList<T>(List<T> videos)
    {
        var random = new Random();
        int n = videos.Count();
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            T value = videos[k];
            videos[k] = videos[n];
            videos[n] = value;
        }
        return videos;
    }

    /// <summary>
    /// rounds to one of the 2 nearest ints, but with higher probability towards the closer int. 
    /// </summary>
    public static int RandomRound(double number)
    {
        return new Random().NextDouble() < number % 1 ? (int)Math.Ceiling(number) : (int)Math.Floor(number);
    }

    public static IEnumerable<T> GetInfiniteDistinctLoop<T>(IList<T> items, int startOffset)
    {
        HashSet<T> distinctItems = new HashSet<T>();
        startOffset = startOffset % items.Count;
        int currentIndex = startOffset;

        while (true)
        {
            T currentItem = items[currentIndex];
            if (!distinctItems.Contains(currentItem))
            {
                distinctItems.Add(currentItem);
                yield return currentItem;
            }

            currentIndex = (currentIndex + 1) % items.Count;
            if (currentIndex == startOffset)
            {
                yield break;
            }
        }
    }

    public static IEnumerable<T> GetBackwardsInfiniteDistinctLoop<T>(IList<T> items, int startOffset)
    {
        HashSet<T> distinctItems = new HashSet<T>();
        startOffset = startOffset % items.Count;
        int currentIndex = startOffset;

        while (true)
        {
            T currentItem = items[items.Count - 1 - currentIndex]; //this is the only difference with GetInfiniteDistinctLoop()
            if (!distinctItems.Contains(currentItem))
            {
                distinctItems.Add(currentItem);
                yield return currentItem;
            }

            currentIndex = (currentIndex + 1) % items.Count;
            if (currentIndex == startOffset)
            {
                yield break;
            }
        }
    }
}