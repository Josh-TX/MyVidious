namespace MyVidious.Models.Admin;

public class UserInfo
{
    public string? Username { get; set; }
    public bool IsAdmin { get; set; }
    public bool AnyUsers { get; set; }

    /// <summary>
    /// False = invite closed, null = by code only, true = no code needed
    /// </summary>
    public bool? OpenInvite { get; set; }
}