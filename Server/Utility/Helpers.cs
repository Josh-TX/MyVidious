

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

    public static string FormatViews(long number)
    {
        const long billion = 1000000000;
        const long million = 1000000;
        const long thousand = 1000;

        if (number >= billion)
            return $"{number / billion}.{(number % billion) / (billion / 10)}B";
        if (number >= million)
            return $"{number / million}.{(number % million) / (million / 10)}M";
        if (number >= thousand)
            return $"{number / thousand}.{(number % thousand) / (thousand / 10)}K";
        return number.ToString() + " views";
    }

    public static string GetPublishedText(long published)
    {
        DateTimeOffset inputDateTime = DateTimeOffset.FromUnixTimeSeconds(published);
        DateTimeOffset currentDateTime = DateTimeOffset.UtcNow;

        int years = currentDateTime.Year - inputDateTime.Year;
        int months = currentDateTime.Month - inputDateTime.Month;
        int days = currentDateTime.Day - inputDateTime.Day;
        int hours = currentDateTime.Hour - inputDateTime.Hour;
        int minutes = currentDateTime.Minute - inputDateTime.Minute;
        if (years > 0)
        {
            return $"{years} {(years == 1 ? "year" : "years")} ago";
        }
        else if (months > 0)
        {
            return $"{months} {(months == 1 ? "month" : "months")} ago";
        }
        else if (days > 0)
        {
            return $"{days} {(days == 1 ? "day" : "days")} ago";
        }
        else if (hours > 0)
        {
            return $"{hours} {(hours == 1 ? "hour" : "hours")} ago";
        }
        else if (minutes > 0)
        {
            return $"{minutes} {(minutes == 1 ? "minutes" : "minutes")} ago";
        }
        else
        {
            return "just now";
        }
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