namespace MyVidious.Models.Admin;

public class CreateAccountRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
    public string? InviteCode { get; set; }
}