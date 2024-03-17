using System;
using System.IO;
using System.Net;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

public static class TimeHelper
{
    public static string GetPublishedText(long published)
    {
        DateTimeOffset inputDateTime = DateTimeOffset.FromUnixTimeSeconds(published);
        DateTimeOffset currentDateTime = DateTimeOffset.UtcNow;

        TimeSpan difference = currentDateTime - inputDateTime;
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
            return $"{minutes} {(minutes == 1 ? "minutes" : "minutess")} ago";
        }
        else
        {
            return "just now";
        }
    }
}