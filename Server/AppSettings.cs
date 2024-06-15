
using MyVidious.Access;

public class AppSettings
{
    public string? InternalInvidiousUrl { get; set; }
    public string? ExternalInvidiousUrl { get; set; }
    public string? MeilisearchUrl { get; set; }
    public string? MeilisearchKey { get; set; }
    public string? ConnectionString { get; set; }

    public static void Validate(AppSettings appSettings)
    {
        if (string.IsNullOrEmpty(appSettings.ConnectionString))
        {
            var msg = "ERROR: No Postgres ConnectionString specified. If running with docker, make sure a CONNECTIONSTRING environmental variable is specified.";
            ErrorMessage(msg);
        }
        if (string.IsNullOrEmpty(appSettings.MeilisearchUrl))
        {
            var msg = "ERROR: No MeilisearchUrl specified. If running with docker, make sure a MEILISEARCHURL environmental variable is specified.";
            ErrorMessage(msg);
        }
        if (!IsValidHttpUrl(appSettings.MeilisearchUrl!, true))
        {
            var msg = $"ERROR: invalid MeilisearchUrl '${appSettings.MeilisearchUrl}'. Must be a valid URL";
            ErrorMessage(msg);
        }
        if (string.IsNullOrEmpty(appSettings.InternalInvidiousUrl))
        {
            var msg = "ERROR: No InternalInvidiousUrl specified. If running with docker, make sure an INTERNALINVIDIOUSURL environmental variable is specified.";
            var msg2 = "InternalInvidiousUrl can be set to a URL, or you can set it to 'pool', which will use a pool of public Invidious instances loaded from " + InvidiousUrlsAccess.INSTANCES_URL;
            ErrorMessage(msg, msg2);
        }
        if (!IsValidHttpUrl(appSettings.InternalInvidiousUrl!, true))
        {
            var msg = $"ERROR: invalid InternalInvidiousUrl '${appSettings.InternalInvidiousUrl}'. Must be either a URL or you can set it to 'pool'.";
            ErrorMessage(msg);
        }
        if (string.IsNullOrEmpty(appSettings.MeilisearchKey))
        {
            var msg = "WARNING: No MeilisearchKey specified. This might cause authentication issues if the meilisearch instance specifies a MEILI_MASTER_KEY";
            WarningMessage(msg);
        }
        if (string.IsNullOrEmpty(appSettings.ExternalInvidiousUrl))
        {
            var msg = "WARNING: No ExternalInvidiousUrl specified. This means that all image urls will be proxied through MyVidious, causing slower load times";
            var msg2 = "ExternalInvidiousUrl can be set to a URL, or you can set it to 'pool', which will use a pool of public Invidious instances loaded from " + InvidiousUrlsAccess.INSTANCES_URL;
            WarningMessage(msg, msg2);
        }
        if (!IsValidHttpUrl(appSettings.ExternalInvidiousUrl!, true))
        {
            var msg = $"ERROR: invalid ExternalInvidiousUrl '${appSettings.ExternalInvidiousUrl}'. Must be either a URL or you can set it to 'pool'.";
            ErrorMessage(msg);
        }
    }

    public static bool IsValidHttpUrl(string url, bool allowPool)
    {
        if (allowPool && url.ToLower() == "pool")
        {
            return true;
        }
        if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult))
        {
            return uriResult != null && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
        return false;
    }
    private static void WarningMessage(string msg, string? msg2 = null)
    {
        Console.WriteLine(msg);
        System.Diagnostics.Debug.WriteLine(msg);
        if (msg2 != null)
        {
            Console.WriteLine(msg2);
            System.Diagnostics.Debug.WriteLine(msg2);
        }
    }
    private static void ErrorMessage(string msg, string? msg2 = null)
    {
        Console.WriteLine(msg);
        System.Diagnostics.Debug.WriteLine(msg);
        if (msg2 != null)
        {
            Console.WriteLine(msg2);
            System.Diagnostics.Debug.WriteLine(msg2);
        }
        Environment.Exit(1);
    }
}