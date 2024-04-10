namespace MyVidious.Models.Admin;

public class InviteCode
{
    public required string Code { get; set; }
    public int RemainingUses { get; set; }
    public int UsageCount { get; set; }
}